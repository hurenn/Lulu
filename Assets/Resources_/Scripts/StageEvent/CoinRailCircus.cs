using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// コインレール上を1回のコインワープで渡り切る演出(板野サーカス風)。
/// レールのコインにコインワープした時点でWarpControlから呼び出され、
/// 登録順にコインを高速でワープしながら、画面外から絶え間なく撃ち込まれる弾に狙われ続ける。
/// </summary>
public class CoinRailCircus : MonoBehaviour {
    [Header("レール")]
    [SerializeField] private Transform[] _rails;            // 渡る順に並べたコイン列(子のコインを子の順番に渡る)
    [SerializeField] private float _stepInterval = 0.03f;   // コイン1枚ごとのワープ間隔
    [SerializeField, Range(0f, 1f)] private float _coinSeVolume = 0.5f; // レールのコインの取得音の音量の倍率(連続で鳴るため下げる)

    [SerializeField] private GameObject[] _hideOnStart;     // 演出開始時に非表示にするもの(追ってきていたゼルクス等)

    [Header("ルルの見た目")]
    [SerializeField] private GameObject _fallbackLightDomePrefab; // 光の能力が未所持の時に、代わりにルルに付けるLightDome

    [Header("弾(ルルの進行方向の反対側の画面外から絶え間なく撃ち込まれる。大半は弧を描いてルルの脇をかすめる)")]
    [SerializeField] private CircusMissile _missilePrefab;
    [SerializeField] private float _fireInterval = 0.05f;    // 撃ち込まれる間隔
    [SerializeField] private float _spawnMargin = 1.5f;      // 画面の外側へどれだけ離れた位置から撃つか
    [SerializeField] private int _travelLookAheadCoins = 8;   // ルルの大まかな進行方向を、何枚先のコインまでの向きで判断するか
    [SerializeField] private int _fireEndMargin = 12;        // レールの残りコインがこの枚数を切ったら撃ち込みを止める(ボス戦の手前)
    [SerializeField, Range(0f, 1f)] private float _directShotRate = 0.05f; // ルルを一直線に狙う(弱いホーミング付きの)弾の割合。残りは弧を描いてかすめる弾
    [SerializeField] private Vector2 _arcTurnRateRange = new Vector2(60f, 120f); // 弧を描く弾の曲がる速さの範囲(度/秒。向きは左右ランダム)
    [SerializeField] private Vector2 _missDistanceRange = new Vector2(2.5f, 6.0f); // 弧を描く弾が、ルルの進む先からどれだけ横に外れた所を通るか
    [SerializeField] private int _leadJitterCoins = 4;       // 狙う先をコイン何枚分前後にずらすか(ばらけさせる)
    [SerializeField, Range(0f, 1f)] private float _burstRate = 0.8f; // ルルの周りで弾けて消える弾の割合(残りは弾けずに画面外へ飛び去る)
    [SerializeField] private Vector2 _burstTimeRateRange = new Vector2(0.85f, 1.25f); // 弾ける時間を、狙う点に届くまでの時間の何倍にするかの範囲

    [Header("見せ場(指定レールの先頭コインに着いた瞬間)")]
    [SerializeField] private Transform[] _highlightRails;
    [SerializeField] private int _highlightMissiles = 14;   // 一斉に撃ち込まれる弾数(ルルの進む先へ集中する)

    [Header("ニアミス(弾がプレイヤーの間近で爆発した瞬間)")]
    [SerializeField] private Color _nearMissFlashColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private float _nearMissFlashTime = 0.15f;
    [SerializeField] private float _nearMissShake = 0.3f;
    [SerializeField] private float _nearMissCooldown = 0.4f;     // 連続で起きた時に演出を出さない間隔(実時間)

    [Header("カメラ")]
    [SerializeField] private CinemachineCamera _circusCamera; // 演出中に優先させるカメラ
    [SerializeField] private float _fieldOfView = 52.0f;    // 演出開始時の画角(引きの絵にする)
    [SerializeField] private float _straightMinFov = 40.0f; // 直線が続いた時に寄りきる画角
    [SerializeField] private float _zoomInSpeed = 5.0f;     // 直線で寄っていく速さ(度/秒)
    [SerializeField] private float _turnFovKick = 6.0f;     // 方向転換の時に引く量(度)
    [SerializeField] private float _turnMaxFov = 56.0f;     // 方向転換で引く時の上限の画角
    [SerializeField] private float _fovSmoothTime = 0.2f;   // 画角の変化のなめらかさ
    [SerializeField] private float _dutchAmplitude = 7.0f;  // カメラの傾きの振れ幅(度)
    [SerializeField] private float _dutchFrequency = 0.8f;

    [Header("カメラの追従(ばねのようにルルを追いかける)")]
    // 直線の初動では引き離されてから徐々に追いつき、急な方向転換では勢いが残って行き過ぎてから戻る
    [SerializeField] private float _turnAngleThreshold = 40.0f; // レールの向きがこの角度以上変わるコインを方向転換とみなす(画角を少し引く)
    [SerializeField] private float _followFrequency = 5.0f;  // 追従の速さ(大きいほどすぐ追いつく)
    [SerializeField, Range(0.1f, 1f)] private float _followDamping = 0.55f; // 揺れの収まりやすさ(1で行き過ぎない。小さいほど方向転換で大きく行き過ぎる)
    [SerializeField, Range(0f, 1f)] private float _lagMaxViewRate = 0.75f; // 追従が遅れても、ルルが画面中心から端までのこの割合の内側に収まるようにする

    private static readonly Dictionary<Transform, CoinRailCircus> _coinToCircus = new Dictionary<Transform, CoinRailCircus>();
    private readonly List<Transform> _coins = new List<Transform>();
    private readonly HashSet<Transform> _highlightCoins = new HashSet<Transform>();
    private bool _isPlaying;
    private int _currentIndex;         // プレイヤーが今いるコインの番号
    private Transform _target;         // 狙われるプレイヤー
    private Coroutine _fireRoutine;
    private readonly List<CircusMissile> _activeMissiles = new List<CircusMissile>(); // 飛んでいる弾(演出の終了時に全て消す)
    private float _lastNearMissTime = -999f;
    private float _stepPeriod = 0.05f;  // ルルがコイン1枚を進むのにかかる実測の時間(偏差撃ちの予測に使う)
    private float _lastStepTime = -1f;

    // カメラが追う目印。ばねのようにルルを追いかける
    private Transform _cameraAnchor;
    private Vector2 _anchorVelocity;   // 目印の速度(ばねの追従用)
    private float _targetFov;          // 目指す画角(直線で徐々に狭め、方向転換で広げる)
    private float _fovVelocity;        // SmoothDamp用
    private float _cameraTime;         // カメラの傾きの揺れ用の経過時間

    private void Awake() {
        foreach (var rail in _rails) {
            if (rail == null) continue;
            for (int i = 0; i < rail.childCount; i++) {
                var coin = rail.GetChild(i);
                _coins.Add(coin);
                var coin_object = coin.GetComponent<Coin_Object>();
                if (coin_object != null) coin_object.SetCollectSeVolume(_coinSeVolume);
                _coinToCircus[coin] = this;
            }
        }
        foreach (var rail in _highlightRails) {
            if (rail != null && rail.childCount > 0) _highlightCoins.Add(rail.GetChild(0));
        }
    }

    private void OnDestroy() {
        foreach (var coin in _coins) {
            if (coin != null && _coinToCircus.TryGetValue(coin, out var circus) && circus == this) {
                _coinToCircus.Remove(coin);
            }
        }
    }

    /// <summary>
    /// 指定コインがサーカス演出のレールに含まれていれば、その演出を返す
    /// </summary>
    public static CoinRailCircus FindByCoin(Transform coin) {
        if (coin != null && _coinToCircus.TryGetValue(coin, out var circus) && circus != null && !circus._isPlaying) {
            return circus;
        }
        return null;
    }

    /// <summary>
    /// 指定コインからレールの最後までワープで渡る
    /// </summary>
    public IEnumerator Play(WarpControl warp, Transform start_coin) {
        int start_index = _coins.IndexOf(start_coin);
        if (start_index < 0) yield break;

        _isPlaying = true;
        _lastStepTime = -1f;
        _target = warp.transform;
        _currentIndex = start_index;
        var player = warp.GetComponentInParent<Player_Character>();
        bool prev_invincible = player != null && player.IsEventInvincible;
        GameObject fallback_dome = null;
        if (player != null) {
            player.IsEventInvincible = true;
            player.SetRailTravel(true);
            // 移動中はルルの姿を消し、LightDomeの光だけを見せる(光の能力が無ければ代わりのLightDomeを付ける)
            bool has_light = player.SetLightOnlyAppearance(true);
            if (!has_light && _fallbackLightDomePrefab != null) {
                fallback_dome = _CreateFallbackLightDome(player.transform);
            }
        }

        int prev_priority = 0;
        float prev_fov = 0f;
        Vector3 prev_damping = Vector3.zero;
        if (_circusCamera != null) {
            prev_priority = _circusCamera.Priority.Value;
            prev_fov = _circusCamera.Lens.FieldOfView;
            if (_cameraAnchor == null) _cameraAnchor = new GameObject("CircusCameraAnchor").transform;
            _cameraAnchor.position = warp.transform.position;
            _circusCamera.Follow = _cameraAnchor;
            _circusCamera.PreviousStateIsValid = false;
            _circusCamera.Priority = 30;
            prev_damping = _SetCameraDamping(Vector3.zero); // 遅れはアンカー側で作るため、カメラ自体は目印にぴったり付ける
            _anchorVelocity = Vector2.zero; // レール開始時は止まった状態から追いかけ始める(ルルが飛び出していくのを一瞬見送る)
            _targetFov = _fieldOfView;
            _fovVelocity = 0f;
            _cameraTime = 0f;
        }

        foreach (var obj in _hideOnStart) {
            if (obj != null) obj.SetActive(false);
        }

        _fireRoutine = StartCoroutine(_FireLoop());
        for (int i = start_index; i < _coins.Count; i++) {
            var coin = _coins[i];
            if (coin == null) continue;
            _currentIndex = i;
            // コイン1枚あたりの進む時間を実測し、偏差撃ちの予測に使う
            if (_lastStepTime >= 0f) _stepPeriod = Mathf.Lerp(_stepPeriod, Time.time - _lastStepTime, 0.3f);
            _lastStepTime = Time.time;

            yield return warp.WarpToPoint(coin.position, 0f);

            // 急な方向転換: 少し引く(カメラが行き過ぎる動きは、ばねの追従で自然に起きる)
            if (_IsSharpTurn(i)) {
                _targetFov = Mathf.Min(_targetFov + _turnFovKick, _turnMaxFov);
            }

            // 見せ場: 画面外の全周から一斉に撃ち込む
            if (_highlightCoins.Contains(coin)) {
                _FireHighlight();
            }

            // ワープ間隔の待機(カメラはLateUpdateで毎フレーム更新する)
            float wait = _stepInterval;
            do {
                wait -= Time.deltaTime;
                if (wait > 0f) yield return null;
            } while (wait > 0f);
        }

        if (_fireRoutine != null) {
            StopCoroutine(_fireRoutine);
            _fireRoutine = null;
        }
        // ボス部屋に弾を持ち込まないよう、レールを渡り切った時点で残っている弾は全て爆発させる
        foreach (var missile in _activeMissiles) {
            if (missile != null) missile.Explode();
        }
        _activeMissiles.Clear();

        if (_circusCamera != null) {
            var lens = _circusCamera.Lens;
            lens.Dutch = 0f;
            lens.FieldOfView = prev_fov;
            _circusCamera.Lens = lens;
            _circusCamera.Priority = prev_priority;
            _circusCamera.Follow = warp.transform;
            _SetCameraDamping(prev_damping);
        }
        if (player != null) {
            player.IsEventInvincible = prev_invincible;
            player.SetRailTravel(false);
            player.SetLightOnlyAppearance(false);
        }
        if (fallback_dome != null) Destroy(fallback_dome);
        _isPlaying = false;
    }

    // 見た目だけのLightDomeを付ける(光の能力の発動音・当たり判定は出さない)
    private GameObject _CreateFallbackLightDome(Transform parent) {
        // 非アクティブな親の下で生成し、発動音(playOnAwake)が鳴る前に音と当たり判定を止める
        var holder = new GameObject("FallbackLightDomeHolder");
        holder.SetActive(false);
        var dome = Instantiate(_fallbackLightDomePrefab, holder.transform);
        foreach (var audio in dome.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (var col in dome.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        dome.transform.SetParent(parent, false);
        Destroy(holder);
        return dome;
    }

    /// <summary>
    /// 画面外から絶え間なく撃ち込む。ルルを直接狙う弾と、進む先を予測した偏差撃ちの弾を混ぜる
    /// </summary>
    private IEnumerator _FireLoop() {
        float direct_budget = 0f;
        while (_coins.Count - _currentIndex > _fireEndMargin) {
            // 一直線に狙う弾が指定の割合で均等に混ざるようにする
            direct_budget += _directShotRate;
            bool is_direct = direct_budget >= 1f;
            if (is_direct) direct_budget -= 1f;

            _FireFromEdge(_GetSpawnBaseAngle(), Random.value, is_direct);
            yield return new WaitForSeconds(_fireInterval);
        }
        _fireRoutine = null;
    }

    /// <summary>
    /// 見せ場: 撃ってくる側の画面外の端から端まで並べて、弧を描く弾を一斉に撃ち込む(弾の幕)
    /// </summary>
    private void _FireHighlight() {
        float base_angle = _GetSpawnBaseAngle();
        for (int n = 0; n < _highlightMissiles; n++) {
            float t = (n + Random.Range(0.1f, 0.9f)) / _highlightMissiles; // 端から端まで均等に、少しだけずらして並べる
            _FireFromEdge(base_angle, t, false);
        }
        if (CinemachineManager.HasInstance) CinemachineManager.Instance.ShakeCamera(0.6f, 0.2f);
    }

    /// <summary>
    /// 弾が撃ってくる方向(ルルから見た角度)。ルルの大まかな進行方向の反対側の画面外
    /// 横に進んでいる時は左右(右へ進んでいれば左から)、上下に大きく進んでいる時は上下(上へ進んでいれば下から)
    /// </summary>
    private float _GetSpawnBaseAngle() {
        int from = Mathf.Clamp(_currentIndex, 0, _coins.Count - 1);
        int to = Mathf.Clamp(_currentIndex + _travelLookAheadCoins, 0, _coins.Count - 1);
        Vector2 travel = from != to ? (Vector2)(_coins[to].position - _coins[from].position) : _GetRailDirection(from);
        if (Mathf.Abs(travel.y) > Mathf.Abs(travel.x)) {
            return travel.y > 0f ? 270f : 90f; // 上へ進んでいれば下から、下へ進んでいれば上から
        }
        return travel.x >= 0f ? 180f : 0f;     // 右へ進んでいれば左から、左へ進んでいれば右から
    }

    /// <summary>
    /// 撃ってくる側の画面のすぐ外側から撃つ
    /// 左右の画面外からなら縦の位置を、上下の画面外からなら横の位置を、edge_rateで画面の端から端まで選ぶ
    /// </summary>
    /// <param name="side_angle">撃ってくる側(ルルから見た角度。180で左、270で下)</param>
    /// <param name="edge_rate">画面外の辺に沿った位置(0〜1)</param>
    /// <param name="is_direct">trueならルルを一直線に狙う弾、falseなら弧を描いてルルの脇をかすめる弾</param>
    private CircusMissile _FireFromEdge(float side_angle, float edge_rate, bool is_direct) {
        if (_target == null) return null;
        Vector2 origin = _GetEdgeSpawnPosition(side_angle, edge_rate);
        var missile = _SpawnMissile(origin);
        if (missile == null) return null;

        if (is_direct) {
            Vector2 to_target = (Vector2)_target.position - origin;
            missile.Launch(_target, to_target, _OnNearMiss, 0f, _GetBurstTime(to_target.magnitude / missile.Speed));
            return missile;
        }

        // ルルが進む先から、進行方向に対して横へ外した点を通るように狙う
        Vector2 predicted = _PredictTargetPosition(origin, missile.Speed, Random.Range(-_leadJitterCoins, _leadJitterCoins + 1));
        Vector2 travel = _GetRailDirection(_currentIndex);
        Vector2 side = new Vector2(-travel.y, travel.x) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 aim_pos = predicted + side * Random.Range(_missDistanceRange.x, _missDistanceRange.y);

        // 一定の速さで曲がり続けて、円弧がちょうど狙う点を通るように撃ち出す向きを決める
        // (円弧は、弦の向きから曲がる角度の合計の半分だけ手前に傾けて撃ち出すと、弦の端を通る)
        float turn_rate = Random.Range(_arcTurnRateRange.x, _arcTurnRateRange.y) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 chord = aim_pos - origin;
        float total_turn = _GetArcTotalTurn(chord.magnitude, missile.Speed, Mathf.Abs(turn_rate));
        float chord_angle = Mathf.Atan2(chord.y, chord.x) * Mathf.Rad2Deg;
        float launch_angle = chord_angle - total_turn * 0.5f * Mathf.Sign(turn_rate);
        Vector2 launch_dir = new Vector2(Mathf.Cos(launch_angle * Mathf.Deg2Rad), Mathf.Sin(launch_angle * Mathf.Deg2Rad));
        // 狙う点に届くまでの時間(曲がる角度の合計÷曲がる速さ)を基準に、ルルの周りで弾ける時間を決める
        missile.Launch(_target, launch_dir, _OnNearMiss, turn_rate, _GetBurstTime(total_turn / Mathf.Abs(turn_rate)));
        return missile;
    }

    // 弦の長さ・弾の速さ・曲がる速さから、円弧を描いて弦の端に届くまでに曲がる角度の合計(度)を求める
    // 狙う点に届くまでの時間から、弾けて消える時間を決める(一部の弾は弾けずに飛び去る: 負を返す)
    private float _GetBurstTime(float arrive_time) {
        if (Random.value >= _burstRate) return -1f;
        return arrive_time * Random.Range(_burstTimeRateRange.x, _burstTimeRateRange.y);
    }

    private float _GetArcTotalTurn(float chord_length, float speed, float turn_rate) {
        if (speed <= 0f) return 0f;
        float theta = turn_rate * chord_length / speed; // 最初は弦の長さで近似し、円弧の長さで求め直して収束させる
        for (int iter = 0; iter < 4; iter++) {
            float half_rad = Mathf.Clamp(theta, 1f, 170f) * 0.5f * Mathf.Deg2Rad;
            float arc_length = chord_length * half_rad / Mathf.Sin(half_rad);
            theta = turn_rate * arc_length / speed;
        }
        return Mathf.Min(theta, 170f);
    }

    // 撃ってくる側の画面のすぐ外側で、辺に沿ってedge_rateの位置(カメラは透視投影のため、ルルの奥行きでの見える範囲から求める)
    private Vector2 _GetEdgeSpawnPosition(float side_angle, float edge_rate) {
        var cam = Camera.main;
        if (cam == null) return (Vector2)_target.position + (Vector2)(Quaternion.Euler(0, 0, side_angle) * Vector2.right) * 20f;
        Vector2 center = cam.transform.position;
        float depth = Mathf.Abs(_target.position.z - cam.transform.position.z);
        float half_height = depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float half_width = half_height * cam.aspect;
        // カメラが傾いている分、画面の角が外へはみ出すので、その分も余白に足す
        float tilt_margin = Mathf.Abs(Mathf.Sin(cam.transform.eulerAngles.z * Mathf.Deg2Rad)) * Mathf.Max(half_width, half_height);
        float margin = _spawnMargin + tilt_margin;
        float along = edge_rate * 2f - 1f; // -1〜1

        switch (Mathf.RoundToInt(Mathf.Repeat(side_angle, 360f) / 90f) % 4) {
            case 0: return center + new Vector2(half_width + margin, along * half_height);    // 右
            case 1: return center + new Vector2(along * half_width, half_height + margin);    // 上
            case 2: return center + new Vector2(-half_width - margin, along * half_height);   // 左
            default: return center + new Vector2(along * half_width, -half_height - margin);  // 下
        }
    }

    /// <summary>
    /// 弾がルルに追いつく頃にルルがいるレール上の位置を予測する(偏差撃ち)
    /// </summary>
    /// <param name="offset_coins">予測位置からさらに進める(負なら戻す)コインの枚数</param>
    private Vector2 _PredictTargetPosition(Vector2 origin, float bullet_speed, int offset_coins) {
        Vector2 predicted = _target.position;
        if (bullet_speed <= 0f || _stepPeriod <= 0f) return predicted;

        // 「弾が届くまでの時間」と「その時間にルルが進む位置」を交互に求め直して収束させる
        float time = Vector2.Distance(origin, predicted) / bullet_speed;
        for (int iter = 0; iter < 4; iter++) {
            int index = Mathf.Clamp(_currentIndex + Mathf.RoundToInt(time / _stepPeriod) + offset_coins, 0, _coins.Count - 1);
            predicted = _coins[index].position;
            time = Vector2.Distance(origin, predicted) / bullet_speed;
        }
        return predicted;
    }

    private CircusMissile _SpawnMissile(Vector2 origin) {
        if (_missilePrefab == null) return null;
        _activeMissiles.RemoveAll(m => m == null); // 爆発済みの弾を除く
        var missile = Instantiate(_missilePrefab, origin, Quaternion.identity);
        _activeMissiles.Add(missile);
        return missile;
    }

    /// <summary>
    /// 弾がプレイヤーの間近で爆発した: 白フラッシュ+画面揺れ
    /// </summary>
    private void _OnNearMiss(CircusMissile missile) {
        // 演出終了後に残った弾では出さない。連続で起きた時は間引く
        if (!_isPlaying) return;
        if (Time.unscaledTime - _lastNearMissTime < _nearMissCooldown) return;
        _lastNearMissTime = Time.unscaledTime;

        ScreenFlash.Instance?.Flash(_nearMissFlashTime, _nearMissFlashColor);
        if (CinemachineManager.HasInstance) CinemachineManager.Instance.ShakeCamera(_nearMissShake, 0.1f);
    }

    private void LateUpdate() {
        if (!_isPlaying) return;
        _UpdateCameraAnchor();
        _UpdateCameraLens();
    }

    // 目印をばねのようにルルへ追従させる
    // ルルはワープで飛び飛びに動くが、目印は「ルルの位置へ引っ張る力」と「ルルの進む速さに合わせる力」で滑らかに動く
    // ・直線の初動: 止まっていた目印が加速しきるまで引き離され、その後ルルの速さに乗って徐々に追いつく
    // ・急な方向転換: 目印にはそれまでの向きの勢いが残るため、曲がりきれずに行き過ぎてから戻る
    private void _UpdateCameraAnchor() {
        if (_cameraAnchor == null || _target == null) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f); // 処理落ちで大きく飛ばないようにする
        if (dt <= 0f) return;

        Vector2 player_pos = _target.position;
        Vector2 player_velocity = _GetRailVelocity();
        Vector2 anchor_pos = _cameraAnchor.position;

        float omega = _followFrequency;
        Vector2 accel = omega * omega * (player_pos - anchor_pos) + 2f * _followDamping * omega * (player_velocity - _anchorVelocity);
        _anchorVelocity += accel * dt;
        anchor_pos += _anchorVelocity * dt;

        // ルルが画面から出そうな時は引きずられて付いていく(画面の外側へ向かう速さは打ち消す)
        Vector2 offset = anchor_pos - player_pos;
        Vector2 clamped = _ClampLagOffset(offset, _lagMaxViewRate);
        if (clamped != offset) {
            anchor_pos = player_pos + clamped;
            if (!Mathf.Approximately(clamped.x, offset.x) && Mathf.Sign(_anchorVelocity.x - player_velocity.x) == Mathf.Sign(offset.x)) _anchorVelocity.x = player_velocity.x;
            if (!Mathf.Approximately(clamped.y, offset.y) && Mathf.Sign(_anchorVelocity.y - player_velocity.y) == Mathf.Sign(offset.y)) _anchorVelocity.y = player_velocity.y;
        }
        _cameraAnchor.position = new Vector3(anchor_pos.x, anchor_pos.y, _target.position.z);
    }

    // ルルがレールを進む速度(今いるコインからのレールの向き × 実測の進む速さ)。ワープで飛び飛びに動く実際の位置より滑らか
    private Vector2 _GetRailVelocity() {
        if (_stepPeriod <= 0f || _coins.Count < 2) return Vector2.zero;
        int from = Mathf.Clamp(_currentIndex, 0, _coins.Count - 2);
        float spacing = Vector2.Distance(_coins[from + 1].position, _coins[from].position);
        return _GetRailDirection(_currentIndex) * (spacing / _stepPeriod);
    }

    // ルルが画面の外へ出ないよう、目印とのずれを画面の大きさに合わせて制限する
    private Vector2 _ClampLagOffset(Vector2 offset, float view_rate) {
        var cam = Camera.main;
        if (cam == null) return offset;
        float depth = Mathf.Abs(_target.position.z - cam.transform.position.z);
        float half_height = depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * view_rate;
        float half_width = half_height * cam.aspect;
        return new Vector2(Mathf.Clamp(offset.x, -half_width, half_width), Mathf.Clamp(offset.y, -half_height, half_height));
    }

    // 指定コインから次のコインへ向かうレールの向き
    private Vector2 _GetRailDirection(int index) {
        if (_coins.Count < 2) return Vector2.right;
        int from = Mathf.Clamp(index, 0, _coins.Count - 2);
        Vector2 dir = _coins[from + 1].position - _coins[from].position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    // 指定コインで、入ってくる向きと出ていく向きが大きく変わるか
    private bool _IsSharpTurn(int index) {
        if (index <= 0 || index >= _coins.Count - 1) return false;
        Vector2 dir_in = _coins[index].position - _coins[index - 1].position;
        Vector2 dir_out = _coins[index + 1].position - _coins[index].position;
        return Vector2.Angle(dir_in, dir_out) >= _turnAngleThreshold;
    }

    // カメラの追従の遅れを設定し、元の値を返す
    private Vector3 _SetCameraDamping(Vector3 damping) {
        var composer = _circusCamera.GetComponent<CinemachinePositionComposer>();
        if (composer == null) return damping;
        var prev = composer.Damping;
        composer.Damping = damping;
        return prev;
    }

    // 画角: 直線では徐々に寄り、方向転換で引いた分はなめらかに戻る。傾き: ゆっくり揺らす
    private void _UpdateCameraLens() {
        if (_circusCamera == null) return;
        float dt = Time.deltaTime;
        _cameraTime += dt;
        _targetFov = Mathf.MoveTowards(_targetFov, _straightMinFov, _zoomInSpeed * dt);

        var lens = _circusCamera.Lens;
        lens.FieldOfView = Mathf.SmoothDamp(lens.FieldOfView, _targetFov, ref _fovVelocity, _fovSmoothTime, Mathf.Infinity, dt);
        lens.Dutch = Mathf.Sin(_cameraTime * _dutchFrequency * Mathf.PI * 2f) * _dutchAmplitude;
        _circusCamera.Lens = lens;
    }
}
