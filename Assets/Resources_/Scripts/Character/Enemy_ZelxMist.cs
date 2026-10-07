using System.Collections;
using UnityEngine;

/// <summary>
/// ボス「霧のゼルクス」
/// </summary>
public class Enemy_ZelxMist : Enemy_Base, IActivatable {
    private enum eZelxAction {
        None,
        Move,
        Generate,
    }

    [SerializeField] private AudioClip _seFinish;
    [SerializeField] private AudioSource _audioBGM;
    [SerializeField] private AudioClip _bgmFlower;
    private float _currentBGMVolume = 0.7f;

    [Header("移動AIパラメータ")]
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private Collider2D _moveArea; // 移動範囲を指定する別オブジェクトのCollider(Trigger推奨)
    [SerializeField] private Vector2 _initialMoveDirection = new Vector2(1f, 0.5f); // 初動の方向(比率のまま反射し続ける)

    [SerializeField] private GameObject _mistBurstPrefab; // 被ダメ時に発生させるパーティクル
    [SerializeField] private MistShield _mistShield;
    [SerializeField] private GameObject _bodyHitEffectPrefab; // スタン中に本体へFireBulletが当たった時のエフェクト
    [SerializeField] private GameObject _firstStunMessageObject; // 初めてスタンした時にActiveにするGameObject(メッセージ表示用)

    [Header("生成設定")]
    [SerializeField] private GameObject _sylxMistPrefab;    // 生成するSylxMistのプレハブ
    [SerializeField] private Transform[] _generatePoints;   // 生成位置のリスト
    [SerializeField] private float _generateInterval = 5.0f; // 生成間隔

    [Header("スタン設定")]
    [SerializeField] private int _stunBulletsToExplode = 20;    // 爆発するまでのFireBullet被弾数
    [SerializeField] private float _growPerStunHit = 0.05f;     // 1発ごとの巨大化量(通常サイズに対する割合)
    [SerializeField] private float _explosionDamageRate = 0.3f; // 爆発時に削る最大HPの割合
    [SerializeField] private GameObject _explosionEffectPrefab; // 爆発時に発生させるエフェクト
    [SerializeField] private float _explosionShakeIntensity = 0.3f; // 爆発時の画面シェイク強さ
    [SerializeField] private float _explosionShakeDuration = 0.3f;  // 爆発時の画面シェイク時間
    [SerializeField] private float _shieldGrowthPerExplosion = 1.2f; // 爆発するたびにシールドの最大サイズへ掛ける倍率
    [SerializeField] private float _postExplosionDelay = 1.0f;  // 爆発から防御復活までの時間
    [SerializeField] private float _fastMoveSpeedRate = 2.0f;   // 防御復活後の移動速度倍率
    [SerializeField] private float _fastMoveDuration = 5.0f;    // 高速移動する時間
    [SerializeField] private float _stunAutoRecoverTime = 7.0f; // 爆発に至らなくても自動でスタン解除するまでの時間
    [SerializeField] private float _stunMoveSpeedRate = 1f / 3f; // スタン中の移動速度倍率

    private Vector2 _moveDirection;
    private Vector3 _normalScale;

    private bool _isStunned = false;       // true: スタン中(通常ダメージを受け付けず被弾カウント+巨大化として扱う)
    private bool _isFrozen = false;        // true: 完全に移動を止める(爆発の瞬間〜シールド復活まで)
    private float _currentMoveSpeedRate = 1f; // _moveSpeedに対する倍率
    private int _stunHitCount = 0;
    private bool _isExploding = false; // 爆発〜復帰シーケンスが多重起動しないようにするフラグ
    private float _generateTimer;
    private bool _isActive = false; // false中はTimelineの合図があるまで行動(移動・生成)を止めておく
    private Coroutine _stunTimeoutCoroutine;
    private Coroutine _explodeCoroutine;
    private bool _hasStunnedBefore = false; // 初回スタン判定用
    private bool _hasExplodedBefore = false; // 初回爆発判定用
    private int _explosionCount = 0; // 爆発回数

    protected override void _Setup() {
        base._Setup();
        _normalScale = transform.localScale;
        _moveDirection = _initialMoveDirection.sqrMagnitude > 0.0001f ? _initialMoveDirection.normalized : Vector2.right;
        _generateTimer = _generateInterval;
    }

    // Timelineの合図で行動を開始させる
    public void StartActing() {
        _isActive = true;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();

        if (_isDead || _damageReactionTimer > 0 || !_isActive) {
            return;
        }

        if (_isFrozen) {
            // 爆発の瞬間〜シールド復活までは完全に停止する。速度が蓄積して暴走しないようにもする
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _Move();

        if (!_isStunned) {
            // 通常移動中(スタン〜爆発〜復帰のシーケンス中は除く)のみ一定間隔でSylxMistを生成する
            _UpdateGenerate();
        }
    }

    // 一定間隔で、登録済みの全てのGenerate用TransformからSylxMistを生成する
    private void _UpdateGenerate() {
        _generateTimer -= Time.deltaTime;
        if (_generateTimer > 0f) return;
        _generateTimer = _generateInterval;

        if (_sylxMistPrefab == null || _generatePoints == null) return;
        foreach (var point in _generatePoints) {
            if (point == null) continue;
            Instantiate(_sylxMistPrefab, point.position, Quaternion.identity);
        }
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        if (_isStunned) {
            // スタン中はシールドが無く攻撃が本体に直接当たるため、通常ダメージにはせず被弾カウント+巨大化として扱う
            _OnStunBulletHit();
            return false;
        }

        bool result = base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);

        if (result) {
            // 本体への攻撃が通った瞬間、スタン開始。通常の3倍のバーストを発生させる
            for (int i = 0; i < 3; i++) {
                if (_mistBurstPrefab != null) Instantiate(_mistBurstPrefab, transform.position, Quaternion.identity);
            }
            // とどめの一撃の場合はDie()が先に走っているため、スタン(シールド復活タイマー)を開始しない
            if (!_isDead) {
                _EnterStun();
            }
        }

        return result;
    }

    // スタン開始: 1/3速度での移動に切り替え、シールドを当たり判定ごと完全に消す(スタン中に本体へ攻撃が当たらない不具合の対処)。
    // 巨大化・被弾数は爆発するまで持ち越すため、ここではリセットしない
    private void _EnterStun() {
        _isStunned = true;
        _currentMoveSpeedRate = _stunMoveSpeedRate;
        if (_mistShield != null) _mistShield.gameObject.SetActive(false);
        _stunTimeoutCoroutine = StartCoroutine(_StunTimeoutCo());

        if (!_hasStunnedBefore) {
            // 初めてスタンした時だけ、指定したGameObjectをActiveにする(メッセージ表示用)
            _hasStunnedBefore = true;
            if (_firstStunMessageObject != null) _firstStunMessageObject.SetActive(true);
        }
    }

    // 爆発に至らなくても、スタンしてから一定時間が経てば自動で復帰する
    private IEnumerator _StunTimeoutCo() {
        yield return new WaitForSeconds(_stunAutoRecoverTime);

        if (_isExploding) yield break; // 既に爆発側の復帰シーケンスが進行中なら何もしない
        _isExploding = true;

        // 巨大化した体・被弾カウントは爆発と違いそのまま引き継ぐが、シールドは最大状態に戻す
        if (_mistShield != null) {
            _mistShield.ResetToFull();
            _mistShield.gameObject.SetActive(true);
        }

        yield return StartCoroutine(_RecoverFastMoveCo());

        _isStunned = false;
        _isExploding = false;
    }

    // スタン中にFireBulletを受けた時: 僅かに巨大化し、一定数で爆発させる
    private void _OnStunBulletHit() {
        if (_isExploding) return; // 爆発〜復帰シーケンス中の被弾はカウントしない(多重爆発防止)

        _audioSource?.PlayOneShot(_seDamage);
        if (_bodyHitEffectPrefab != null) {
            Instantiate(_bodyHitEffectPrefab, transform.position, Quaternion.identity);
        }

        _stunHitCount++;
        transform.localScale = _normalScale * (1f + _growPerStunHit * _stunHitCount);

        if (_stunHitCount >= _stunBulletsToExplode) {
            _isExploding = true;
            _explodeCoroutine = StartCoroutine(_ExplodeAndRecover());
        }
    }

    // 爆発: 巨大化と被弾カウントをリセットし、最大HPの一定割合を削る。1秒後にシールドを完全復活させ、その後一定時間高速移動する
    private IEnumerator _ExplodeAndRecover() {
        // 爆発の瞬間は完全に動きを止める
        _isFrozen = true;

        if (!_hasExplodedBefore) {
            // 初めて爆発した時だけ、ダウン演出等のOnDownedを呼ぶ
            _hasExplodedBefore = true;
            OnDowned?.Invoke();
        }

        _explosionCount++;
        if (_explosionCount == 2) {
            // 2回爆発した後は本気モードへ
            OnSeriously?.Invoke();
        }

        transform.localScale = _normalScale;
        _stunHitCount = 0;

        if (_explosionEffectPrefab != null) {
            Vector3 spriteCenter = _sprite != null ? _sprite.bounds.center : transform.position;
            Instantiate(_explosionEffectPrefab, spriteCenter, Quaternion.identity);
        }

        CinemachineManager.Instance?.ShakeCamera(_explosionShakeIntensity, _explosionShakeDuration);

        _mistShield?.GrowMaxSize(_shieldGrowthPerExplosion);
        _stunBulletsToExplode = Mathf.RoundToInt(_stunBulletsToExplode * _shieldGrowthPerExplosion); // シールド拡大に合わせて必要撃破数も増加

        int explosionDamage = Mathf.RoundToInt(_charaParam.MaxHP * _explosionDamageRate);
        base.Damage(explosionDamage, Vector2.zero, 0f, 0f, false, true); // 無敵を貫通して確実にダメージを与える

        // 爆発直後から大暴れ(高速移動フェーズ)が収まるまで本体を無敵にする
        IsEventInvincible = true;

        yield return new WaitForSeconds(_postExplosionDelay);

        // 大暴れに入る直前で死亡判定を行う。爆発ダメージがとどめだった場合はシールドを復活させず、
        // 大暴れフェーズにも入らない(Die()側でシールドを消したままにしているのを上書きしないため)
        if (_isDead) {
            yield break;
        }

        if (_mistShield != null) {
            _mistShield.ResetToFull();
            _mistShield.gameObject.SetActive(true);
        }

        yield return StartCoroutine(_RecoverFastMoveCo());

        _isStunned = false;
        _isExploding = false;
    }

    // 復帰後の高速移動フェーズ(移動再開・無敵化・一定時間後に通常速度へ)。爆発/タイムアウト両方の復帰経路で共用する
    private IEnumerator _RecoverFastMoveCo() {
        _isFrozen = false;
        _currentMoveSpeedRate = _fastMoveSpeedRate;
        IsEventInvincible = true;
        _mistShield?.SetInvincible(true);

        yield return new WaitForSeconds(_fastMoveDuration);

        _currentMoveSpeedRate = 1f;
        IsEventInvincible = false;
        _mistShield?.SetInvincible(false);
    }

    // _moveAreaの範囲内を等速直線運動で動き続ける。壁とは衝突せず、範囲のX端/Y端でそれぞれの軸だけ反転する。
    // 自身とMistShieldの当たり判定サイズを考慮し、中心ではなく端が範囲を超えた時点で反転させる
    private void _Move() {
        if (_moveArea == null) return;

        var bounds = _moveArea.bounds;
        Vector2 halfSize = GetCharacterSize() * 0.5f;

        if (_mistShield != null && _mistShield.gameObject.activeInHierarchy) {
            Vector2 shieldHalfSize = _mistShield.GetWorldSize() * 0.5f;
            halfSize = Vector2.Max(halfSize, shieldHalfSize);
        }

        Vector3 pos = transform.position;

        float minX = bounds.min.x + halfSize.x;
        float maxX = bounds.max.x - halfSize.x;
        float minY = bounds.min.y + halfSize.y;
        float maxY = bounds.max.y - halfSize.y;

        if (pos.x >= maxX && _moveDirection.x > 0) _moveDirection.x *= -1f;
        else if (pos.x <= minX && _moveDirection.x < 0) _moveDirection.x *= -1f;

        if (pos.y >= maxY && _moveDirection.y > 0) _moveDirection.y *= -1f;
        else if (pos.y <= minY && _moveDirection.y < 0) _moveDirection.y *= -1f;

        Vector3 velocity = new Vector3(_moveDirection.x, _moveDirection.y, 0f) * _moveSpeed * _currentMoveSpeedRate;
        _rb.MovePosition(transform.position + velocity * Time.deltaTime);
    }

    protected override IEnumerator Die() {
        // Die実行直後、進行中のスタン/爆発復帰シーケンスを止めてからMistShieldを完全に非表示にする
        // (放置すると後からシールドを復活させてしまうため)
        if (_stunTimeoutCoroutine != null) StopCoroutine(_stunTimeoutCoroutine);
        if (_explodeCoroutine != null) StopCoroutine(_explodeCoroutine);
        if (_mistShield != null) _mistShield.gameObject.SetActive(false);

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
        yield return new WaitForSecondsRealtime(0.2f);
        //flash?.Flash(3.0f);

        // BGM停止
        if (_audioBGM != null) {
            _audioBGM.volume = 0f;
        }

        yield return new WaitForSecondsRealtime(4.0f);

        cinemachineManager.ReturnToPlayer();

        // 徐々に時間を戻す
        float timeScale = Time.timeScale;
        while (timeScale < 1f) {
            timeScale += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Min(timeScale, 1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // === 4. 爆発 ===
        if (_dieExplosion != null) {
            // 爆発エフェクト生成
            flash?.Flash();
            yield return new WaitForSecondsRealtime(0.2f);
            flash?.Flash();
            yield return new WaitForSecondsRealtime(0.5f);

            Instantiate(_dieExplosion, transform.position, Quaternion.identity);
            cinemachineManager.ShakeCamera(duration: 0.5f);
            _sprite.enabled = false;
        }

        yield return new WaitForSeconds(1.0f);

        OnDieEnded?.Invoke();

        yield return new WaitForSeconds(5.0f);

        // BGM切り替え
        _audioBGM.volume = _currentBGMVolume;
        _audioBGM.clip = _bgmFlower;
        _audioBGM.Play();
    }
}
