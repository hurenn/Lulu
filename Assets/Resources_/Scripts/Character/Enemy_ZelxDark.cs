using System.Collections;
using UnityEngine;

/// <summary>
/// ボス「闇のゼルクス」
/// 第一形態: その場から動かず、SylxDarkを一定間隔でランダムな数だけ生み出す(1回分は全員同じ動き)。闇の盾で通常攻撃を防ぐ
///          必殺技は当たるが、必殺技が終わるまでダメージを受けない。必殺技が終わった時点で盾が壊れ、HPが大きく減ってダウンする
/// ダウン: 真下に落ちて着地し、HPが半分になるまで削られるか一定時間経ったら第一形態でいた位置まで上がり、第二形態へ移る
/// 第二形態: SylxDarkを生み出す・体当たりのどちらかの行動を繰り返す
///          それとは別に、能力使用不可エリアを一定間隔で展開し直す(最初は1つ目のエリアに決まっている)
/// </summary>
public class Enemy_ZelxDark : Enemy_Base, IActivatable, IDamageSourceBlocker, IDamageSourceDeflector {
    public override bool IsBossSide => true; // ボス(氷の必殺技ゲージが多く溜まる)

    private enum ePhase {
        First,  // 第一形態(闇の盾で通常攻撃を防ぐ)
        Down,   // ダウン(落ちて着地し、HPが半分になったら上がる)
        Second, // 第二形態
    }

    private enum eAction {
        Generate, // ①SylxDarkを生み出す
        Tackle,   // ②体当たり
    }

    [SerializeField] private AudioClip _seFinish;
    [SerializeField] private AudioSource _audioBGM;
    private float _currentBGMVolume = 0.7f;

    [Tooltip("Timelineの合図を待たずに、最初から行動する(テスト用)")]
    [SerializeField] private bool _actOnStart = false;

    [Tooltip("着地・体当たり・エリア展開の時に鳴らす音")]
    [SerializeField] private AudioClip _seExplosion;
    [Tooltip("着地・体当たり・エリア展開の時のカメラの揺れの強さ")]
    [SerializeField] private float _impactShakeIntensity = 0.5f;
    [Tooltip("着地・体当たり・エリア展開の時のカメラの揺れの時間(秒)")]
    [SerializeField] private float _impactShakeDuration = 0.4f;

    // SylxDarkが間を移動する2点
    [System.Serializable]
    private class PathPoints {
        public Transform PointA;
        public Transform PointB;
    }

    [Header("■ SylxDarkを生み出す(第一形態・第二形態)")]
    [Tooltip("生み出すSylxDarkのプレハブ")]
    [SerializeField] private SylxDark _sylxDarkPrefab;
    [Tooltip("第一形態で生み出す間隔(秒)")]
    [SerializeField] private float _generateInterval = 3.0f;
    [Tooltip("1回に生み出す数の最小値")]
    [SerializeField] private int _generateCountMin = 1;
    [Tooltip("1回に生み出す数の最大値")]
    [SerializeField] private int _generateCountMax = 5;
    [Tooltip("①2点の間を移動するSylxDarkの、2点の組(1匹ごとにどれか1組が選ばれ、どちらの点から出るかもランダム)。未登録なら①は選ばれない")]
    [SerializeField] private PathPoints[] _pathPoints;
    [Tooltip("①で同じ組が選ばれた時、後から出るSylxDarkを遅らせる時間(秒)。重なって出ないようにする")]
    [SerializeField] private float _pathSameDelay = 0.5f;
    [Tooltip("②プレイヤーを追尾するSylxDarkを生み出す位置(生み出す数より少なければ順に使い回す)。未設定なら自分の位置")]
    [SerializeField] private Transform[] _generatePoints;

    [Header("■ 第一形態:闇の盾(通常攻撃を防ぎ、必殺技を受けると壊れる)")]
    [Tooltip("闇の盾の見た目")]
    [SerializeField] private GameObject _darkShield;
    [Tooltip("通常攻撃を盾で弾いた時に、当たった位置に出すエフェクト(未設定なら出さない)")]
    [SerializeField] private GameObject _shieldDeflectEffect;
    [Tooltip("盾が壊れた時に出すエフェクト(未設定なら出さない)")]
    [SerializeField] private GameObject _shieldBreakEffect;
    [Tooltip("盾が壊れた時のカメラの揺れの強さ")]
    [SerializeField] private float _shieldBreakShakeIntensity = 0.5f;
    [Tooltip("盾が壊れた時のカメラの揺れの時間(秒)")]
    [SerializeField] private float _shieldBreakShakeDuration = 0.5f;
    [Tooltip("盾が壊れた時に減らすHP(最大HPに対する割合)")]
    [SerializeField] private float _shieldBreakDamageRate = 1f / 3f;

    [Header("■ ダウン(真下に落ちて着地し、HPが半分になるか一定時間経ったら第一形態でいた位置まで上がる)")]
    [Tooltip("着地してから、HPが半分になっていなくても復帰するまでの時間(秒)")]
    [SerializeField] private float _downTime = 5.0f;
    [Tooltip("落ちる速さの増え方(重力)")]
    [SerializeField] private float _fallGravity = 40.0f;
    [Tooltip("落ちる速さの最大値")]
    [SerializeField] private float _fallMaxSpeed = 25.0f;
    [Tooltip("第一形態でいた位置まで上がる速さ")]
    [SerializeField] private float _riseSpeed = 6.0f;

    [Header("■ 第二形態:行動")]
    [Tooltip("行動を終えてから、次の行動を始めるまでの時間(秒)")]
    [SerializeField] private float _actionInterval = 3.0f;

    [Header("第二形態:②体当たり")]
    [Tooltip("体当たりで突っ込む先(地面の上。プレイヤーに一番近い所へ突っ込む)")]
    [SerializeField] private Transform[] _tacklePoints;
    [Tooltip("突っ込む前に、その場で震える時間(秒)")]
    [SerializeField] private float _tackleShakeTime = 0.8f;
    [Tooltip("突っ込む前に震える幅")]
    [SerializeField] private float _tackleShakeWidth = 0.15f;
    [Tooltip("初期位置から突っ込む速さ(最後の速さ。だんだん速くなる)")]
    [SerializeField] private float _tackleSpeed = 25.0f;
    [Tooltip("別の突っ込む先へ、山なりに飛んで突っ込む時間(秒)")]
    [SerializeField] private float _tackleJumpTime = 1.0f;
    [Tooltip("別の突っ込む先へ飛ぶ時の、山の高さ(初期位置の高さより上には上がらない)")]
    [SerializeField] private float _tackleJumpHeight = 8.0f;
    [Tooltip("突っ込んだ先から、次の体当たりで初期位置に戻る確率(0〜1)。それ以外は別の突っ込む先へ飛ぶ")]
    [SerializeField] private float _tackleReturnRate = 0.5f;
    [Tooltip("初期位置に戻る速さ")]
    [SerializeField] private float _tackleReturnSpeed = 8.0f;

    [Header("第二形態:能力使用不可エリアの展開(行動とは別に、一定間隔で行う)")]
    [Tooltip("展開するエリア(ScaleのXを0から1にして展開する)。第二形態になった時に、1つ目のエリアを必ず展開する")]
    [SerializeField] private GameObject[] _sealAreas;
    [Tooltip("エリアを展開し直す間隔(秒)。他の行動中でも関係なく展開する")]
    [SerializeField] private float _sealAreaInterval = 5.0f;
    [Tooltip("エリアを展開する(畳む)時間(秒)")]
    [SerializeField] private float _sealAreaOpenTime = 0.2f;

    private ePhase _phase = ePhase.First;
    private bool _isActive = false; // false中はTimelineの合図があるまで行動を止めておく
    private float _generateTimer = 0f;
    private bool _isSpecialHit = false; // 第一形態で必殺技を受けた(必殺技が終わったら盾を壊す)
    private bool _isHalfHPReached = false; // HPが半分まで削られた(OnHalfHPを呼んだ)
    private Coroutine _actionRoutine = null;
    private Coroutine _sealAreaRoutine = null;
    private Vector2 _homePosition; // 第一形態でいた位置(ダウンから復帰する時に戻り、第二形態の初期位置になる)
    private int _tackleIndex = -1;  // 今いる体当たりの突っ込む先(-1: 初期位置にいる)
    private int _sealAreaIndex = -1; // 展開しているエリア(-1: 展開していない)

    protected override void _Setup() {
        base._Setup();
        _generateTimer = _generateInterval;
        _isActive = _actOnStart;
        if (_darkShield != null) _darkShield.SetActive(true);
        // エリアは畳んだ状態から始める
        if (_sealAreas != null) {
            foreach (var area in _sealAreas) _SetAreaScaleX(area, 0f);
        }
    }

    // Timelineの合図で行動を開始させる
    public void StartActing() {
        _isActive = true;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        // 被弾で吹き飛ばされない(動く時は位置を直接動かす)
        _rb.linearVelocity = Vector2.zero;
        if (_isDead) return;

        // 第一形態で必殺技を受けていたら、必殺技が終わった時点で盾を壊す
        if (_isSpecialHit && !Ability_Base.isAnySpecialUsing) {
            _isSpecialHit = false;
            _BreakShield();
            return;
        }

        if (!_isActive) return;
        if (_phase == ePhase.First) {
            _UpdateGenerate();
        }
    }

    // 第一形態は、闇の盾で通常攻撃(DamageZoneを通す攻撃)を全て防ぐ。必殺技(DamageZoneを通さない攻撃)は防がない
    public bool IsBlockedDamageSource(DamageZone source) {
        return _phase == ePhase.First && source != null && !_isDead;
    }

    // 盾で防いでいる間も、炎の弾によるロックオンはされる
    public bool IsLockonOnBlocked => true;

    // 盾で通常攻撃を防いだ時は、弾いたエフェクトを出す(ヒットストップはDamageZone側でかける)
    public void OnDeflected(DamageZone source, Vector3 position) {
        if (_shieldDeflectEffect != null) Instantiate(_shieldDeflectEffect, position, Quaternion.identity);
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        if (_phase == ePhase.First) {
            // 第一形態は必殺技以外では傷つかない
            if (!Ability_Base.isAnySpecialUsing) return false;
            // 必殺技は当たったように見せるが、必殺技が終わるまでダメージは受けない
            _isSpecialHit = true;
            return base.Damage(0, Vector2.zero, 0f, 0f, is_trap_damage, true);
        }
        if (_phase == ePhase.Down) {
            // ダウン中は、HPが半分より減らない(半分まで削られたら、時間切れを待たずに上がって第二形態へ移る)
            int half_hp = _GetHalfHP();
            damage = Mathf.Clamp(damage, 0, Mathf.Max(0, _charaParam.CurrentHP - half_hp));
            bool result = base.Damage(damage, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
            _CheckHalfHP();
            return result;
        }
        // 移動するボスのため、被弾時の吹き飛びは無効にする
        bool second_result = base.Damage(damage, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
        _CheckHalfHP();
        return second_result;
    }

    // HPが半分まで減ったら、一度だけOnHalfHPを呼ぶ(ダウンから時間切れで復帰した時は、第二形態で半分まで減った時に呼ぶ)
    private void _CheckHalfHP() {
        if (_isHalfHPReached || _isDead || _charaParam.CurrentHP > _GetHalfHP()) return;
        _isHalfHPReached = true;
        OnHalfHP?.Invoke();
    }

    private int _GetHalfHP() {
        return Mathf.CeilToInt(_charaParam.MaxHP * 0.5f);
    }

    /// <summary>
    /// 闇の盾が壊れる: 盾を消し、SparkEffectを出し(ステージ側がOnDownedで出す)、最大HPの一定割合を減らしてダウンする
    /// </summary>
    private void _BreakShield() {
        _phase = ePhase.Down;
        _homePosition = transform.position; // 第一形態は動かないため、今の位置が第一形態でいた位置

        if (_darkShield != null) {
            if (_shieldBreakEffect != null) Instantiate(_shieldBreakEffect, _darkShield.transform.position, Quaternion.identity);
            _darkShield.SetActive(false);
        }
        CinemachineManager.Instance?.ShakeCamera(_shieldBreakShakeIntensity, _shieldBreakShakeDuration);

        OnDowned?.Invoke();

        int damage = Mathf.RoundToInt(_charaParam.MaxHP * _shieldBreakDamageRate);
        base.Damage(damage, Vector2.zero, 0f, 0f, false, true); // 無敵を貫通して確実にダメージを与える
        if (_isDead) return;
        _CheckHalfHP();

        _actionRoutine = StartCoroutine(_DownRoutine());
    }

    #region ダウン

    // 真下に落ちて着地し、HPが半分になるか一定時間経つまで待ってから真上に上がり、第二形態へ移る
    private IEnumerator _DownRoutine() {
        // 落ちる
        float land_y = _GetGroundY(transform.position) ?? transform.position.y;
        float speed = 0f;
        while (transform.position.y > land_y) {
            yield return new WaitForFixedUpdate();
            speed = Mathf.Min(speed + _fallGravity * Time.deltaTime, _fallMaxSpeed);
            _MoveTo(new Vector2(transform.position.x, Mathf.Max(land_y, transform.position.y - speed * Time.deltaTime)));
        }
        _PlayImpact();

        // HPが半分まで削られるか、一定時間経つのを待つ
        float down_time = 0f;
        while (!_isHalfHPReached && down_time < _downTime) {
            down_time += Time.deltaTime;
            yield return null;
        }

        // 第一形態でいた位置まで上がる(そこが第二形態の初期位置になる)
        while (Vector2.Distance(transform.position, _homePosition) > 0.001f) {
            yield return new WaitForFixedUpdate();
            _MoveTo(Vector2.MoveTowards(transform.position, _homePosition, _riseSpeed * Time.deltaTime));
        }
        _MoveTo(_homePosition);

        _phase = ePhase.Second;
        _sealAreaRoutine = StartCoroutine(_SealAreaRoutine());
        _actionRoutine = StartCoroutine(_SecondPhaseRoutine());
    }

    /// <summary>
    /// 指定した位置の真下の地面に着地した時の位置(自分の位置のY)を返す(見つからなければnull)
    /// </summary>
    private float? _GetGroundY(Vector2 position) {
        float bottom_offset = _GetBottomOffset();
        var hit = Physics2D.Raycast(new Vector2(position.x, position.y - bottom_offset + 0.5f), Vector2.down, 100f, _groundLayer);
        if (hit.collider == null) return null;
        return hit.point.y + bottom_offset;
    }

    // 自分の位置から、当たり判定の下端までの距離
    private float _GetBottomOffset() {
        return _col != null ? transform.position.y - _col.bounds.min.y : 0.5f;
    }

    #endregion

    #region 第二形態

    // 少し待ってから行動を選んで行うことを繰り返す(SylxDarkを生み出すか、体当たり)
    private IEnumerator _SecondPhaseRoutine() {
        while (!_isDead) {
            yield return new WaitForSeconds(_actionInterval);
            switch (_ChooseAction()) {
                case eAction.Generate:
                    _Generate();
                    break;
                case eAction.Tackle:
                    yield return _Tackle();
                    break;
            }
        }
    }

    // 行動とは別に、一定間隔でエリアを展開し直す(最初は1つ目のエリア)
    private IEnumerator _SealAreaRoutine() {
        if (_sealAreas == null || _sealAreas.Length == 0) yield break;
        yield return _OpenSealArea(0);
        while (!_isDead) {
            yield return new WaitForSeconds(_sealAreaInterval);
            yield return _OpenSealArea(_ChooseSealArea());
        }
    }

    private eAction _ChooseAction() {
        var actions = new System.Collections.Generic.List<eAction>();
        if (_sylxDarkPrefab != null) actions.Add(eAction.Generate);
        if (_GetTackleCandidates(-1).Count > 0) actions.Add(eAction.Tackle);
        if (actions.Count == 0) return eAction.Generate;
        return actions[Random.Range(0, actions.Count)];
    }

    // 着地・体当たり・エリア展開の瞬間: カメラを揺らして音を鳴らす
    private void _PlayImpact() {
        CinemachineManager.Instance?.ShakeCamera(_impactShakeIntensity, _impactShakeDuration);
        if (_seExplosion != null) _audioSource?.PlayOneShot(_seExplosion);
    }

    private void _MoveTo(Vector2 position) {
        _rb.MovePosition(position);
        transform.position = position; // 次の計算でもすぐに新しい位置を使う
    }

    /// <summary>
    /// ②体当たり: 初期位置にいる時は、震えてからプレイヤーに一番近い突っ込む先へ突っ込む
    /// 突っ込んだ先にいる時は、初期位置に戻るか、震えてから別の突っ込む先へ山なりに飛んで突っ込む
    /// </summary>
    private IEnumerator _Tackle() {
        var others = _GetTackleCandidates(_tackleIndex);
        bool is_return = _tackleIndex >= 0 && (others.Count == 0 || Random.value < _tackleReturnRate);
        if (is_return) {
            _tackleIndex = -1;
            yield return _MoveLinear(_homePosition, _tackleReturnSpeed, false);
            yield break;
        }
        if (others.Count == 0) yield break;

        int index = _GetNearestToPlayer(others);
        Vector2 target = _GetTacklePosition(_tacklePoints[index]);
        yield return _ShakeInPlace();
        if (_tackleIndex < 0) {
            yield return _MoveLinear(target, _tackleSpeed, true);
        } else {
            yield return _JumpTo(target);
        }
        _tackleIndex = index;
        _PlayImpact();
    }

    // 突っ込む先の候補(exclude_indexは除く)
    private System.Collections.Generic.List<int> _GetTackleCandidates(int exclude_index) {
        var list = new System.Collections.Generic.List<int>();
        if (_tacklePoints == null) return list;
        for (int i = 0; i < _tacklePoints.Length; i++) {
            if (_tacklePoints[i] != null && i != exclude_index) list.Add(i);
        }
        return list;
    }

    private int _GetNearestToPlayer(System.Collections.Generic.List<int> candidates) {
        var controller = PlayerCharacterManager.Controller;
        if (controller == null) return candidates[Random.Range(0, candidates.Count)];
        Vector2 player = controller.Character.transform.position;
        int nearest = candidates[0];
        float nearest_distance = float.MaxValue;
        foreach (int i in candidates) {
            float distance = ((Vector2)_tacklePoints[i].position - player).sqrMagnitude;
            if (distance < nearest_distance) {
                nearest_distance = distance;
                nearest = i;
            }
        }
        return nearest;
    }

    // 突っ込む先の地面に着地した時の位置(地面が見つからなければ、突っ込む先の位置そのもの)
    private Vector2 _GetTacklePosition(Transform point) {
        Vector2 position = point.position;
        float? ground_y = _GetGroundY(position);
        if (ground_y.HasValue) position.y = ground_y.Value;
        return position;
    }

    // 突っ込む前に、その場で震える
    private IEnumerator _ShakeInPlace() {
        Vector2 base_position = transform.position;
        float time = 0f;
        while (time < _tackleShakeTime) {
            yield return new WaitForFixedUpdate();
            time += Time.deltaTime;
            _MoveTo(base_position + Random.insideUnitCircle * _tackleShakeWidth);
        }
        _MoveTo(base_position);
    }

    // まっすぐ移動する(is_accelなら、だんだん速くなって最後に指定の速さになる)
    private IEnumerator _MoveLinear(Vector2 target, float speed, bool is_accel) {
        Vector2 start = transform.position;
        float duration = Vector2.Distance(start, target) / Mathf.Max(speed, 0.01f);
        if (is_accel) duration *= 2f; // 等加速で、最後の速さがspeedになる時間
        float time = 0f;
        while (time < duration) {
            yield return new WaitForFixedUpdate();
            time += Time.deltaTime;
            float rate = Mathf.Clamp01(time / Mathf.Max(duration, 0.001f));
            if (is_accel) rate *= rate;
            else rate = Mathf.SmoothStep(0f, 1f, rate);
            _MoveTo(Vector2.Lerp(start, target, rate));
        }
        _MoveTo(target);
    }

    // 山なりに大きく飛んで突っ込む(初期位置の高さより上には上がらない)
    private IEnumerator _JumpTo(Vector2 target) {
        Vector2 start = transform.position;
        // 始点と終点の高い方から、山の高さだけ上がる(始点と終点の真ん中で一番高くなる放物線)
        float top_y = Mathf.Min(Mathf.Max(start.y, target.y) + _tackleJumpHeight, Mathf.Max(_homePosition.y, Mathf.Max(start.y, target.y)));
        float bump = top_y - (start.y + target.y) * 0.5f;
        float time = 0f;
        while (time < _tackleJumpTime) {
            yield return new WaitForFixedUpdate();
            time += Time.deltaTime;
            float rate = Mathf.Clamp01(time / Mathf.Max(_tackleJumpTime, 0.001f));
            float x = Mathf.Lerp(start.x, target.x, rate);
            float y = Mathf.Lerp(start.y, target.y, rate) + bump * 4f * rate * (1f - rate);
            _MoveTo(new Vector2(x, y));
        }
        _MoveTo(target);
    }

    // 次に展開するエリア(展開中のエリア以外から選ぶ)
    private int _ChooseSealArea() {
        var candidates = new System.Collections.Generic.List<int>();
        for (int i = 0; i < _sealAreas.Length; i++) {
            if (_sealAreas[i] != null && i != _sealAreaIndex) candidates.Add(i);
        }
        if (candidates.Count == 0) return _sealAreaIndex;
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// ③能力使用不可エリアを展開する(展開していたエリアは同時に畳む)
    /// </summary>
    private IEnumerator _OpenSealArea(int index) {
        if (_sealAreas == null || index < 0 || index >= _sealAreas.Length || _sealAreas[index] == null) yield break;
        if (index == _sealAreaIndex) yield break; // 既に展開している

        var close_area = _sealAreaIndex >= 0 ? _sealAreas[_sealAreaIndex] : null;
        var open_area = _sealAreas[index];
        _sealAreaIndex = index;
        _PlayImpact();

        float time = 0f;
        while (time < _sealAreaOpenTime) {
            time += Time.deltaTime;
            float rate = Mathf.Clamp01(time / Mathf.Max(_sealAreaOpenTime, 0.001f));
            _SetAreaScaleX(open_area, rate);
            _SetAreaScaleX(close_area, 1f - rate);
            yield return null;
        }
        _SetAreaScaleX(open_area, 1f);
        _SetAreaScaleX(close_area, 0f);
    }

    // 倒された時は、展開していたエリアを畳む
    private IEnumerator _CloseSealArea() {
        if (_sealAreas == null || _sealAreaIndex < 0) yield break;
        var area = _sealAreas[_sealAreaIndex];
        _sealAreaIndex = -1;
        float time = 0f;
        while (time < _sealAreaOpenTime) {
            time += Time.unscaledDeltaTime; // 倒された演出中は時間が遅くなるため、実時間で畳む
            _SetAreaScaleX(area, 1f - Mathf.Clamp01(time / Mathf.Max(_sealAreaOpenTime, 0.001f)));
            yield return null;
        }
        _SetAreaScaleX(area, 0f);
    }

    private static void _SetAreaScaleX(GameObject area, float x) {
        if (area == null) return;
        var scale = area.transform.localScale;
        scale.x = x;
        area.transform.localScale = scale;
    }

    #endregion

    #region SylxDarkを生み出す

    // 第一形態: 一定間隔でSylxDarkを生み出す
    private void _UpdateGenerate() {
        _generateTimer -= Time.deltaTime;
        if (_generateTimer > 0f) return;
        _generateTimer = _generateInterval;
        _Generate();
    }

    // SylxDarkを生み出す(数はランダム。1回に生み出すSylxDarkは全員同じ動きをする)
    private void _Generate() {
        if (_sylxDarkPrefab == null) return;
        int count = Random.Range(Mathf.Max(1, _generateCountMin), Mathf.Max(_generateCountMin, _generateCountMax) + 1);
        bool can_path = _pathPoints != null && System.Array.Exists(_pathPoints, p => p != null && p.PointA != null && p.PointB != null);
        if (can_path && Random.value < 0.5f) {
            _GeneratePath(count);
        } else {
            _GenerateChase(count);
        }
    }

    // ①: 2点の組をなるべく重ならないように選び、どちらかの点から出す
    private void _GeneratePath(int count) {
        var candidates = new System.Collections.Generic.List<PathPoints>();
        foreach (var p in _pathPoints) {
            if (p != null && p.PointA != null && p.PointB != null) candidates.Add(p);
        }
        // 組の順番を混ぜ、全組を使い切ったら同じ組を遅らせて使い回す
        for (int i = candidates.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        for (int i = 0; i < count; i++) {
            var points = candidates[i % candidates.Count];
            bool from_a = Random.value < 0.5f;
            Vector2 start = from_a ? points.PointA.position : points.PointB.position;
            Vector2 end = from_a ? points.PointB.position : points.PointA.position;
            float delay = (i / candidates.Count) * _pathSameDelay;
            if (delay > 0f) {
                StartCoroutine(_GeneratePathDelayed(start, end, delay));
            } else {
                _SpawnPath(start, end);
            }
        }
    }

    private IEnumerator _GeneratePathDelayed(Vector2 start, Vector2 end, float delay) {
        yield return new WaitForSeconds(delay);
        if (_isDead) yield break;
        _SpawnPath(start, end);
    }

    private void _SpawnPath(Vector2 start, Vector2 end) {
        var sylx = Instantiate(_sylxDarkPrefab, start, Quaternion.identity);
        sylx.SetupPath(start, end);
    }

    // ②: 弧を描きながらプレイヤーを弱く追尾させる
    private void _GenerateChase(int count) {
        for (int i = 0; i < count; i++) {
            var sylx = Instantiate(_sylxDarkPrefab, _GetGeneratePosition(i), Quaternion.identity);
            sylx.SetupChase();
        }
    }

    private Vector3 _GetGeneratePosition(int index) {
        if (_generatePoints != null && _generatePoints.Length > 0) {
            var point = _generatePoints[index % _generatePoints.Length];
            if (point != null) return point.position;
        }
        return transform.position;
    }

    #endregion

    protected override IEnumerator Die() {
        if (_actionRoutine != null) {
            StopCoroutine(_actionRoutine);
            _actionRoutine = null;
        }
        if (_sealAreaRoutine != null) {
            StopCoroutine(_sealAreaRoutine);
            _sealAreaRoutine = null;
        }
        if (_darkShield != null) _darkShield.SetActive(false);
        StartCoroutine(_CloseSealArea());

        OnDied?.Invoke();

        if (_seFinish != null) {
            if (_seDead != null) _audioSource?.PlayOneShot(_seDead);
            _audioSource?.PlayOneShot(_seFinish);
        }

        var cinemachineManager = CinemachineManager.Instance;
        var flash = ScreenFlash.Instance;

        // === 2. カメラズーム ===
        cinemachineManager.ZoomOnTarget(transform);
        yield return new WaitForSeconds(0.05f);

        // === 1. ヒット時演出 ===
        Time.timeScale = 0.1f;
        flash?.Flash();
        _anim.Play("Die");
        yield return Pause_UI.WaitForUnpausedSeconds(0.2f);

        // BGM停止
        if (_audioBGM != null) {
            _audioBGM.volume = 0f;
        }

        yield return Pause_UI.WaitForUnpausedSeconds(4.0f);

        cinemachineManager.ReturnToPlayer();

        // 徐々に時間を戻す
        yield return Pause_UI.RestoreTimeScaleGradually();

        yield return Pause_UI.WaitForUnpausedSeconds(0.5f);

        // === 4. 爆発 ===
        if (_dieExplosion != null) {
            // 爆発エフェクト生成
            flash?.Flash();
            yield return Pause_UI.WaitForUnpausedSeconds(0.2f);
            flash?.Flash();
            yield return Pause_UI.WaitForUnpausedSeconds(0.5f);

            Instantiate(_dieExplosion, transform.position, Quaternion.identity);
            cinemachineManager.ShakeCamera(duration: 0.5f);
            _sprite.enabled = false;
            ParticleTrail.DetachAll(gameObject); // 姿が消えたら靄も出すのをやめる
        }

        yield return new WaitForSeconds(1.0f);

        OnDieEnded?.Invoke();

        yield return new WaitForSeconds(5.0f);

        // BGMの音量を戻す
        if (_audioBGM != null) _audioBGM.volume = _currentBGMVolume;
    }
}
