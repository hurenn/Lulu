using UnityEngine;

// 板野サーカス演出用の弾(当たり判定は持たない演出専用)。撃ち方は2通り
// ・弧を描く弾: 発射時に決めた一定の速さで曲がり続けて円弧を描く(目標は追わない)。曲がった角度の合計が上限に達したら直進する
//   目標に当たる向きに進んでいる時は、目標の脇をかすめるように少しだけ向きをそらす
//   それでも目標の周りの円(_avoidDistance)に入ったら、円の外周へ押し出して外周に沿う向きにする(目標の光の外側を回り込んでかすめる)
//   発射直後は一度バラバラの方向へ散ってから、旋回速度に上限を付けて本来の弧の向きへ戻る(軌跡が交差しやすくなる)
// ・ホーミング弾: 目標の方へゆるく曲がる。目標を追い越すか、曲がった角度の合計が上限に達したらそれ以上は曲がらない
// 目標の間近をかすめた時に一度だけニアミスを通知し、画面を横切って画面外へ出たら消える
public class CircusMissile : MonoBehaviour {
    [SerializeField] private float _speed = 55.0f;           // ワープ中のルル(約31ユニット/秒)を次々と追い越せる速さ
    [SerializeField] private Vector2 _speedRateRange = new Vector2(0.8f, 1.2f); // 弾ごとの速さの倍率の範囲(速さがばらけて軌跡が交差する)

    [Header("奥行き(弾ごとに手前・奥を決め、手前ほど大きく・速く・濃く・前面に描く)")]
    [SerializeField] private Vector2 _sizeRateRange = new Vector2(0.6f, 1.4f); // 大きさ(弾と軌跡の太さ)の倍率の範囲。奥〜手前
    [SerializeField, Range(0f, 1f)] private float _farAlpha = 0.7f;   // 一番奥の弾の濃さ(手前は1)
    [SerializeField] private int _depthSortingLayers = 3;              // 手前・奥で描画順を何段階に分けるか
    [SerializeField] private float _lifetime = 4.0f;         // 画面に入らないまま飛び続けた時の保険の寿命
    [SerializeField] private float _exitViewportMargin = 0.1f; // 画面からこの分(画面サイズ比)外へ出たら消える
    [SerializeField] private float _nearMissDistance = 1.5f; // 目標がこの距離以内をかすめたらニアミス
    [SerializeField] private float _homingTurnRate = 35.0f;  // 目標の方へ曲がる速さの上限(度/秒)。弧が見える程度の弱いホーミング
    [SerializeField] private float _maxHomingAngle = 45.0f;  // 1発が曲がれる角度の合計の上限(度)
    [SerializeField] private float _maxArcAngle = 120.0f;    // 弧を描く弾が曲がれる角度の合計の上限(度)。ぐるぐる回り込まないようにする
    [SerializeField] private Vector2 _scatterAngleRange = new Vector2(25f, 60f); // 発射直後に本来の向きから左右どちらかへ散らす角度の範囲(度)
    [SerializeField] private float _scatterTime = 0.15f;       // 散った向きのまま進む時間
    [SerializeField] private float _scatterReturnTurnRate = 240f; // 散った後、本来の弧の向きへ戻る旋回速度の上限(度/秒)
    [SerializeField] private float _avoidDistance = 2.0f;    // 弧を描く弾が目標から最低限離れて通る距離
    [SerializeField] private float _avoidTurnRate = 180.0f;  // 目標をよけるために向きをそらす速さの上限(度/秒)
    [SerializeField] private float _arcNearMissMargin = 0.5f; // 弧を描く弾は、目標の周りの円(_avoidDistance)からこの距離以内をかすめたらニアミス
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private AudioClip _seLaunch;            // 生成(発射)時の効果音
    [SerializeField] private float _seLaunchVolume = 1.0f;

    // 発射音を鳴らす音源(全ての弾で共有し、新しい弾が出るたびに鳴っている音を止めて鳴らし直す)
    private static AudioSource _seLaunchSource;

    private Transform _target;
    private System.Action<CircusMissile> _onNearMiss; // 目標の間近をかすめた時のコールバック
    private Vector2 _direction;
    private float _timer;
    private bool _isNearMissed;
    private bool _hasEnteredView; // 一度画面に入ったか(画面外から撃たれるため、入る前は消さない)
    private bool _isHoming;       // 目標の方へ曲がっている(目標を追い越すか、曲がれる角度を使い切ったら止める)
    private float _homingTurned;  // これまでに曲がった角度の合計
    private float _arcTurnRate;   // 弧を描く弾の曲がる速さ(度/秒。正で左回り)。0ならホーミング弾
    private float _arcTurned;     // 弧を描く弾がこれまでに曲がった角度の合計
    private Vector2 _arcDirection; // 本来の弧の向き(散っている間も弧に沿って曲がり続ける)
    private bool _isScattering;   // 発射直後に散っている/本来の向きへ戻っている途中
    private float _burstTime;     // この時間が経ったらその場で弾けて消える(負なら弾けずに画面外へ飛び去る)
    private Vector2 _targetVelocity;     // 目標の移動速度(ワープで飛び飛びに動くため、ならして求める)
    private Vector2 _lastTargetPosition;

    /// <summary>
    /// この弾の速さ(生成時に決まる。偏差撃ちの狙いを計算するために発射前に参照する)
    /// </summary>
    public float Speed { get; private set; }

    private void Awake() {
        // 手前らしさ(0で一番奥、1で一番手前)を1つ決め、大きさ・速さ・濃さ・描画順をそろえて変える
        float depth = Random.value;
        Speed = _speed * Mathf.Lerp(_speedRateRange.x, _speedRateRange.y, depth);
        _ApplyDepthLook(depth);
    }

    private void _ApplyDepthLook(float depth) {
        float size = Mathf.Lerp(_sizeRateRange.x, _sizeRateRange.y, depth);
        float alpha = Mathf.Lerp(_farAlpha, 1f, depth);
        int sorting_offset = _depthSortingLayers > 1 ? Mathf.Min(Mathf.FloorToInt(depth * _depthSortingLayers), _depthSortingLayers - 1) * 2 : 0;

        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>(true)) {
            sprite.transform.localScale *= size;
            var color = sprite.color;
            color.a *= alpha;
            sprite.color = color;
            sprite.sortingOrder += sorting_offset;
        }
        if (_trail != null) {
            _trail.widthMultiplier *= size;
            var gradient = _trail.colorGradient;
            var alpha_keys = gradient.alphaKeys;
            for (int i = 0; i < alpha_keys.Length; i++) alpha_keys[i].alpha *= alpha;
            gradient.SetKeys(gradient.colorKeys, alpha_keys);
            _trail.colorGradient = gradient;
            _trail.sortingOrder += sorting_offset;
        }
    }

    /// <summary>
    /// 発射
    /// </summary>
    /// <param name="arc_turn_rate">0以外なら、この速さ(度/秒。正で左回り)で曲がり続けて弧を描く弾にする(目標は追わない)。0ならホーミング弾</param>
    /// <param name="burst_time">この時間が経ったらその場で弾けて消える。負なら弾けずに画面外へ飛び去る</param>
    public void Launch(Transform target, Vector2 direction, System.Action<CircusMissile> on_near_miss = null, float arc_turn_rate = 0f, float burst_time = -1f) {
        _target = target;
        _burstTime = burst_time;
        _arcTurnRate = arc_turn_rate;
        _arcTurned = 0f;
        _arcDirection = direction.normalized;
        _targetVelocity = Vector2.zero;
        if (target != null) _lastTargetPosition = target.position;
        _onNearMiss = on_near_miss;
        _direction = direction.normalized;
        _timer = 0f;
        _isNearMissed = false;
        _PlayLaunchSE();
        _hasEnteredView = false;
        _isHoming = Mathf.Approximately(arc_turn_rate, 0f);
        // 弧を描く弾は、発射直後に一度バラバラの方向へ散らす
        _isScattering = !_isHoming && _scatterTime > 0f;
        if (_isScattering) {
            float scatter = Random.Range(_scatterAngleRange.x, _scatterAngleRange.y) * (Random.value < 0.5f ? -1f : 1f);
            _direction = (Quaternion.Euler(0f, 0f, scatter) * _direction).normalized;
        }
        _homingTurned = 0f;
        _trail?.Clear();
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
    }

    private void Update() {
        float dt = Time.deltaTime;
        _timer += dt;
        if (_burstTime >= 0f && _timer >= _burstTime) {
            _Explode();
            return;
        }
        if (_timer >= _lifetime) {
            _Vanish();
            return;
        }

        _UpdateArc(dt);
        _UpdateAvoid(dt);
        _UpdateHoming(dt);
        transform.position += (Vector3)(_direction * Speed * dt);
        _KeepOutOfAvoidCircle();

        // 画面を横切って画面外へ出たら、爆発させずに消す(画面内で消えて見えないように)
        var cam = Camera.main;
        if (cam != null) {
            var vp = cam.WorldToViewportPoint(transform.position);
            bool is_in_view = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            if (is_in_view) {
                _hasEnteredView = true;
            } else if (_hasEnteredView && (vp.x < -_exitViewportMargin || vp.x > 1f + _exitViewportMargin || vp.y < -_exitViewportMargin || vp.y > 1f + _exitViewportMargin)) {
                _Vanish();
                return;
            }
        }

        float near_miss_distance = _IsArc ? _avoidDistance + _arcNearMissMargin : _nearMissDistance;
        if (!_isNearMissed && _target != null && Vector2.Distance(_target.position, transform.position) <= near_miss_distance) {
            _isNearMissed = true;
            _onNearMiss?.Invoke(this);
        }
    }

    private bool _IsArc => !Mathf.Approximately(_arcTurnRate, 0f);

    // 弧を描く弾: 目標の周りの円に入ったら、円の外周へ押し出して外周に沿う向きにする(必ず目標から離れて通る)
    private void _KeepOutOfAvoidCircle() {
        if (!_IsArc || _target == null || _avoidDistance <= 0f) return;
        Vector2 target_pos = _target.position;
        Vector2 from_target = (Vector2)transform.position - target_pos;
        float distance = from_target.magnitude;
        if (distance >= _avoidDistance || distance < 0.0001f) return;

        Vector2 normal = from_target / distance;
        transform.position = new Vector3(target_pos.x + normal.x * _avoidDistance, target_pos.y + normal.y * _avoidDistance, transform.position.z);
        // 目標の方へ向かっている時だけ、今の進み方に近い側の外周の向きへ変える
        if (Vector2.Dot(_direction, normal) < 0f) {
            Vector2 tangent = new Vector2(-normal.y, normal.x);
            if (Vector2.Dot(tangent, _direction) < 0f) tangent = -tangent;
            _direction = tangent;
            _arcDirection = tangent;   // 弧の向きも外周に沿う向きにし、散っている途中でも戻らずにそのまま飛び去る
            _isScattering = false;
            _arcTurned = _maxArcAngle; // 外周に沿ってからは曲がらずに飛び去る(目標の周りを回り続けないように)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
        }
    }

    // 一定の速さで曲がり続けて円弧を描く(曲がれる角度を使い切ったら直進する)
    // 発射直後に散っている間も本来の弧の向きは曲がり続け、散り終わったら旋回速度の上限付きでその向きへ戻る
    private void _UpdateArc(float dt) {
        if (Mathf.Approximately(_arcTurnRate, 0f)) return;
        if (_arcTurned < _maxArcAngle) {
            float step = Mathf.Min(Mathf.Abs(_arcTurnRate) * dt, _maxArcAngle - _arcTurned);
            _arcTurned += step;
            float arc_angle = Mathf.Atan2(_arcDirection.y, _arcDirection.x) * Mathf.Rad2Deg + step * Mathf.Sign(_arcTurnRate);
            _arcDirection = new Vector2(Mathf.Cos(arc_angle * Mathf.Deg2Rad), Mathf.Sin(arc_angle * Mathf.Deg2Rad));
        }

        float angle;
        if (_isScattering) {
            // 散った向きのまま少し進み、その後本来の弧の向きへ旋回速度の上限付きで戻る
            if (_timer < _scatterTime) return;
            float current = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(_arcDirection.y, _arcDirection.x) * Mathf.Rad2Deg;
            angle = Mathf.MoveTowardsAngle(current, target, _scatterReturnTurnRate * dt);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, target)) < 0.5f) _isScattering = false; // 戻りきったら弧に乗る
        } else {
            angle = Mathf.Atan2(_arcDirection.y, _arcDirection.x) * Mathf.Rad2Deg;
        }
        _direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 弧を描く弾: このまま進むと目標の近くを通りすぎる時は、目標から最低限離れて通るように向きを少しそらす
    // 目標も動いているため、目標から見た弾の動き(相対速度)で、どれだけ近くを通るかを判断する
    private void _UpdateAvoid(float dt) {
        if (_target == null || dt <= 0f) return;
        Vector2 target_pos = _target.position;
        _targetVelocity = Vector2.Lerp(_targetVelocity, (target_pos - _lastTargetPosition) / dt, 0.2f);
        _lastTargetPosition = target_pos;
        if (Mathf.Approximately(_arcTurnRate, 0f) || _avoidDistance <= 0f) return;

        Vector2 to_target = target_pos - (Vector2)transform.position;
        float distance = to_target.magnitude;
        Vector2 relative = _direction * Speed - _targetVelocity;
        // 目標から離れていく(通り過ぎた)か、近すぎてもうよけられない時は何もしない
        if (distance <= _avoidDistance || relative.sqrMagnitude < 0.0001f || Vector2.Dot(relative, to_target) <= 0f) return;

        // 今の動きのまま進んだ時に目標の横を通る距離(正で目標が左側)
        Vector2 relative_dir = relative.normalized;
        float cross = relative_dir.x * to_target.y - relative_dir.y * to_target.x;
        if (Mathf.Abs(cross) >= _avoidDistance) return;

        // 目標が左側にあれば右へ、右側にあれば左へそらす。必要な角度だけ、上限の速さの範囲で曲げる
        float need = (Mathf.Asin(Mathf.Clamp01(_avoidDistance / distance)) - Mathf.Asin(Mathf.Clamp01(Mathf.Abs(cross) / distance))) * Mathf.Rad2Deg;
        float step = Mathf.Min(need, _avoidTurnRate * dt) * (cross >= 0f ? -1f : 1f);
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + step;
        _direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        if (!_isScattering) _arcDirection = _direction; // 弧に乗っている時は、そらした向きを弧の向きにも反映する
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 目標の方へ、旋回の速さの上限の範囲でゆるく曲がる
    private void _UpdateHoming(float dt) {
        if (!_isHoming || _target == null || _homingTurnRate <= 0f) return;
        Vector2 to_target = (Vector2)_target.position - (Vector2)transform.position;
        // 目標を追い越した(目標が後ろ側になった)らホーミングをやめ、そのまま真っすぐ飛び去る
        if (Vector2.Dot(_direction, to_target) <= 0f) {
            _isHoming = false;
            return;
        }
        float current = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        float desired = Mathf.Atan2(to_target.y, to_target.x) * Mathf.Rad2Deg;
        float step = Mathf.Min(_homingTurnRate * dt, _maxHomingAngle - _homingTurned);
        float next = Mathf.MoveTowardsAngle(current, desired, step);
        _homingTurned += Mathf.Abs(Mathf.DeltaAngle(current, next));
        if (_homingTurned >= _maxHomingAngle) _isHoming = false;
        _direction = new Vector2(Mathf.Cos(next * Mathf.Deg2Rad), Mathf.Sin(next * Mathf.Deg2Rad));
        transform.rotation = Quaternion.Euler(0, 0, next);
    }

    // 発射音を鳴らす。既に鳴っている発射音は止めて、最初から鳴らし直す
    private void _PlayLaunchSE() {
        if (_seLaunch == null) return;
        // 共有の音源はシーンを読み直すと消えるため、無ければ作り直す
        if (_seLaunchSource == null) {
            var go = new GameObject("CircusMissileLaunchSE");
            _seLaunchSource = go.AddComponent<AudioSource>();
            _seLaunchSource.playOnAwake = false;
            _seLaunchSource.spatialBlend = 0f;
        }
        _seLaunchSource.Stop();
        _seLaunchSource.clip = AudioUtil.GetClipForGain(_seLaunch, _seLaunchVolume);
        _seLaunchSource.volume = Mathf.Min(_seLaunchVolume, 1f);
        _seLaunchSource.Play();
    }

    /// <summary>
    /// その場で爆発して消える(演出の終了時など)
    /// </summary>
    public void Explode() {
        _Explode();
    }

    private void _Explode() {
        if (_explosionPrefab != null) {
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        }
        _Vanish();
    }

    private void _Vanish() {
        // 煙が途中で消えないよう、トレイルだけ切り離して自然に消えるのを待つ
        // (画面外のトレイルは更新されず自動破棄(autodestruct)が働かないため、表示時間が過ぎたら明示的に破棄する)
        if (_trail != null) {
            _trail.transform.SetParent(null, true);
            _trail.emitting = false;
            Destroy(_trail.gameObject, _trail.time + 0.1f);
        }
        Destroy(gameObject);
    }
}
