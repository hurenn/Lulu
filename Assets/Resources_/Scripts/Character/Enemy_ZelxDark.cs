using System.Collections;
using UnityEngine;

/// <summary>
/// ボス「闇のゼルクス」
/// 第一形態: その場から動かず、SylxDarkを一定間隔でランダムな数だけ生み出す(1回分は全員同じ動き)。闇の盾で通常攻撃を防ぐ
///          必殺技は当たるが、必殺技が終わるまでダメージを受けない。必殺技が終わった時点で盾が壊れ、HPが大きく減って第二形態へ移る
/// 第二形態: 通常攻撃が効く(動きは未実装)
/// </summary>
public class Enemy_ZelxDark : Enemy_Base, IActivatable, IDamageSourceBlocker {
    public override bool IsBossSide => true; // ボス(氷の必殺技ゲージが多く溜まる)

    private enum ePhase {
        First,  // 第一形態(闇の盾で通常攻撃を防ぐ)
        Second, // 第二形態
    }

    [SerializeField] private AudioClip _seFinish;
    [SerializeField] private AudioSource _audioBGM;
    private float _currentBGMVolume = 0.7f;

    [Tooltip("Timelineの合図を待たずに、最初から行動する(テスト用)")]
    [SerializeField] private bool _actOnStart = false;

    // SylxDarkが間を移動する2点
    [System.Serializable]
    private class PathPoints {
        public Transform PointA;
        public Transform PointB;
    }

    [Header("■ 第一形態:SylxDarkを生み出す")]
    [Tooltip("生み出すSylxDarkのプレハブ")]
    [SerializeField] private SylxDark _sylxDarkPrefab;
    [Tooltip("生み出す間隔(秒)")]
    [SerializeField] private float _generateInterval = 3.0f;
    [Tooltip("1回に生み出す数の最小値")]
    [SerializeField] private int _generateCountMin = 1;
    [Tooltip("1回に生み出す数の最大値")]
    [SerializeField] private int _generateCountMax = 5;
    [Tooltip("①2点の間を移動するSylxDarkの、2点の組(1匹ごとにどれか1組が選ばれ、どちらの点から出るかもランダム)。未登録なら①は選ばれない")]
    [SerializeField] private PathPoints[] _pathPoints;
    [Tooltip("①で同じ組が選ばれた時、後から出るSylxDarkを遅らせる時間(秒)。重なって出ないようにする")]
    [SerializeField] private float _pathSameDelay = 0.5f;
    [Tooltip("②プレイヤーへ突撃するSylxDarkを生み出す位置(生み出す数より少なければ順に使い回す)。未設定なら自分の位置")]
    [SerializeField] private Transform[] _generatePoints;

    [Header("■ 第一形態:闇の盾(通常攻撃を防ぎ、必殺技を受けると壊れる)")]
    [Tooltip("闇の盾の見た目")]
    [SerializeField] private GameObject _darkShield;
    [Tooltip("盾が壊れた時に出すエフェクト(未設定なら出さない)")]
    [SerializeField] private GameObject _shieldBreakEffect;
    [Tooltip("盾が壊れた時のカメラの揺れの強さ")]
    [SerializeField] private float _shieldBreakShakeIntensity = 0.5f;
    [Tooltip("盾が壊れた時のカメラの揺れの時間(秒)")]
    [SerializeField] private float _shieldBreakShakeDuration = 0.5f;
    [Tooltip("盾が壊れた時に減らすHP(最大HPに対する割合)")]
    [SerializeField] private float _shieldBreakDamageRate = 1f / 3f;

    private ePhase _phase = ePhase.First;
    private bool _isActive = false; // false中はTimelineの合図があるまで行動を止めておく
    private float _generateTimer = 0f;
    private bool _isSpecialHit = false; // 第一形態で必殺技を受けた(必殺技が終わったら盾を壊す)

    protected override void _Setup() {
        base._Setup();
        _generateTimer = _generateInterval;
        _isActive = _actOnStart;
        if (_darkShield != null) _darkShield.SetActive(true);
    }

    // Timelineの合図で行動を開始させる
    public void StartActing() {
        _isActive = true;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        // その場から動かない
        _rb.linearVelocity = Vector2.zero;
        if (_isDead) return;

        // 第一形態で必殺技を受けていたら、必殺技が終わった時点で盾を壊す
        if (_isSpecialHit && !Ability_Base.isAnySpecialUsing) {
            _isSpecialHit = false;
            _BreakShield();
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

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        if (_phase == ePhase.First) {
            // 第一形態は必殺技以外では傷つかない
            if (!Ability_Base.isAnySpecialUsing) return false;
            // 必殺技は当たったように見せるが、必殺技が終わるまでダメージは受けない
            _isSpecialHit = true;
            return base.Damage(0, Vector2.zero, 0f, 0f, is_trap_damage, true);
        }
        return base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
    }

    /// <summary>
    /// 闇の盾が壊れる: 盾を消し、SparkEffectを出し(ステージ側がOnDownedで出す)、最大HPの一定割合を減らして第二形態へ移る
    /// </summary>
    private void _BreakShield() {
        _phase = ePhase.Second;

        if (_darkShield != null) {
            if (_shieldBreakEffect != null) Instantiate(_shieldBreakEffect, _darkShield.transform.position, Quaternion.identity);
            _darkShield.SetActive(false);
        }
        CinemachineManager.Instance?.ShakeCamera(_shieldBreakShakeIntensity, _shieldBreakShakeDuration);

        OnDowned?.Invoke();

        int damage = Mathf.RoundToInt(_charaParam.MaxHP * _shieldBreakDamageRate);
        base.Damage(damage, Vector2.zero, 0f, 0f, false, true); // 無敵を貫通して確実にダメージを与える
    }

    // 一定間隔でSylxDarkを生み出す(数はランダム。1回に生み出すSylxDarkは全員同じ動きをする)
    private void _UpdateGenerate() {
        _generateTimer -= Time.deltaTime;
        if (_generateTimer > 0f) return;
        _generateTimer = _generateInterval;

        if (_sylxDarkPrefab == null) return;
        int count = Random.Range(Mathf.Max(1, _generateCountMin), Mathf.Max(_generateCountMin, _generateCountMax) + 1);
        bool can_path = _pathPoints != null && System.Array.Exists(_pathPoints, p => p != null && p.PointA != null && p.PointB != null);
        if (can_path && Random.value < 0.5f) {
            _GeneratePath(count);
        } else {
            _GenerateCharge(count);
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
        if (_isDead || _phase != ePhase.First) yield break;
        _SpawnPath(start, end);
    }

    private void _SpawnPath(Vector2 start, Vector2 end) {
        var sylx = Instantiate(_sylxDarkPrefab, start, Quaternion.identity);
        sylx.SetupPath(start, end);
    }

    // ②: 今のプレイヤーの位置へ向かって突撃させる
    private void _GenerateCharge(int count) {
        var controller = PlayerCharacterManager.Controller;
        if (controller == null) return;
        Vector2 target = controller.Character.transform.position;
        for (int i = 0; i < count; i++) {
            var sylx = Instantiate(_sylxDarkPrefab, _GetGeneratePosition(i), Quaternion.identity);
            sylx.SetupCharge(target);
        }
    }

    private Vector3 _GetGeneratePosition(int index) {
        if (_generatePoints != null && _generatePoints.Length > 0) {
            var point = _generatePoints[index % _generatePoints.Length];
            if (point != null) return point.position;
        }
        return transform.position;
    }

    protected override IEnumerator Die() {
        if (_darkShield != null) _darkShield.SetActive(false);

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
        }

        yield return new WaitForSeconds(1.0f);

        OnDieEnded?.Invoke();

        yield return new WaitForSeconds(5.0f);

        // BGMの音量を戻す
        if (_audioBGM != null) _audioBGM.volume = _currentBGMVolume;
    }
}
