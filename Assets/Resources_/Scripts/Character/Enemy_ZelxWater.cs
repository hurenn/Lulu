using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボス「水のゼルクス」
/// 最初は止まっていて、Timelineの合図(ActivateMarker)で行動を開始する
/// 行動開始後は陣取る画面端(既定は右端)へ移動し、カメラに映るギリギリの画面端を維持して漂いながら、
/// 向いている方向(右端なら左)へ扇状の小さな弾幕を数回撃つ→巨大な弾をチャージして一直線に撃つ を繰り返す
/// 跳ね返された巨大な弾が当たると、吹き飛んで壁に叩きつけられ、地上に落ちてダウンする
/// ダウンはHPが次の段階(2/3, 1/3)まで減ると強制解除され、再び画面端へ戻って行動する
/// </summary>
public class Enemy_ZelxWater : Enemy_Base, IActivatable, IKnockdownTarget {
    private enum eState {
        Waiting,     // Timelineの合図を待っている
        Acting,      // 画面端で漂いながら攻撃する
        KnockedBack, // 吹き飛ばされて壁へ向かっている
        Falling,     // 壁に叩きつけられた後、地上へ落ちている
        Down,        // ダウン中(無防備で何もしない)
    }

    private enum eAttackState {
        Barrage,     // 扇状の小さな弾幕を一定間隔で撃つ
        Charging,    // 巨大な弾をチャージ中
        AfterCharge, // 巨大な弾を撃った後の待機
    }

    [Header("位置")]
    [SerializeField] private bool _isOnRightSide = true;  // true: 画面右端に陣取り、左を向いて左へ撃つ / false: 画面左端に陣取り、右を向いて右へ撃つ
    [SerializeField] private float _edgeMargin = 0.0f;   // 画面端からの余白(0で体がちょうど画面に収まる位置)
    [SerializeField] private float _entrySpeed = 6.0f;   // 行動開始時・ダウンからの復帰時に、陣取る画面端へ向かう速さ
    [SerializeField] private Transform _muzzle;          // 弾の発射位置(未設定なら自身の位置)

    [Header("漂う")]
    [SerializeField] private float _driftSpeed = 1.0f;   // 漂う速さ
    [SerializeField] private float _driftRangeX = 0.5f;  // 画面端から内側へ漂う幅(画面外へははみ出さない)
    [SerializeField] private float _driftRangeY = 2.0f;  // 上下に漂う幅

    [Header("扇状の弾幕(小)")]
    [SerializeField] private ZelxFireBullet _bulletPrefab; // 発射する弾のプレハブ(プールで再利用する)
    [SerializeField] private float _fireInterval = 1.0f;   // 弾幕の発射間隔
    [SerializeField] private int _bulletCount = 5;         // 1回の弾幕で発射する弾数
    [SerializeField] private float _fanAngle = 90f;        // 扇の開き角度(向いている方向を中心に上下へ振り分ける)
    [SerializeField] private int _barrageCountBeforeCharge = 3; // 巨大な弾をチャージするまでに撃つ弾幕の回数
    [SerializeField] private AudioClip _seShoot;           // 発射時の効果音

    [Header("チャージ弾(巨大)")]
    [SerializeField] private ZelxFireBullet _chargeBulletPrefab; // チャージして撃つ巨大な弾のプレハブ(プールで再利用する)
    [SerializeField] private float _chargeTime = 3.0f;     // チャージ時間
    [SerializeField] private float _chargeStartScale = 0.2f; // チャージ開始時の弾の大きさ(プレハブの大きさに対する割合。チャージ完了で1になる)
    [SerializeField] private float _afterChargeWait = 1.5f; // 巨大な弾を撃ってから弾幕を再開するまでの時間
    [SerializeField] private AudioClip _seCharge;          // チャージ開始時の効果音
    [SerializeField] private AudioClip _seChargeShoot;     // 巨大な弾の発射時の効果音
    [SerializeField] private ParticleSystem _chargeEffectPrefab; // チャージ中に弾へ光の粒が集まる演出
    [SerializeField] private Vector2 _chargeEffectRateRange = new Vector2(15f, 90f); // チャージ演出の粒の量(チャージ開始→完了)
    [SerializeField] private float _chargeShakeInterval = 0.2f; // チャージ中に画面を揺らす間隔
    [SerializeField] private Vector2 _chargeShakeRange = new Vector2(0.05f, 0.4f); // チャージ中の画面の揺れの強さ(チャージ開始→完了)

    [Header("ダウン")]
    [SerializeField] private float[] _phaseHpRates = { 2f / 3f, 1f / 3f }; // HPがこの割合以下になるとダウンを強制解除し、次の段階へ移る
    [SerializeField] private float _knockbackSpeed = 25f;   // 吹き飛ぶ速さ
    [SerializeField] private float _knockbackMaxTime = 1.0f; // 壁に当たらなかった場合、この時間で吹き飛びを終えて落下する
    [SerializeField] private float _fallGravity = 40f;      // 落下の重力加速度
    [SerializeField] private float _maxFallSpeed = 20f;     // 落下の最大速度
    [SerializeField] private float _slowTimeScale = 0.2f;   // 被弾時のスローモーションの速さ
    [SerializeField] private float _slowDuration = 0.5f;    // 被弾時のズーム・スローモーションの長さ(実時間)
    [SerializeField] private float _wallHitShakeIntensity = 1.0f; // 壁に叩きつけられた時の画面の揺れ
    [SerializeField] private AudioClip _seWallHit;          // 壁に叩きつけられた時の効果音(爆発音)
    [SerializeField] private float _landShakeIntensity = 0.5f; // 地上に落ちた時の画面の揺れ
    [SerializeField] private AudioClip _seLand;             // 地上に落ちた時の効果音

    [Header("シールド")]
    [SerializeField] private GameObject _waterShield;    // 通常時に表示し、ダウン中は非表示にする

    [Header("初回ダウン")]
    [SerializeField] private GameObject _firstDownMessageObject; // 初めてダウンした時にActiveにするGameObject(メッセージ表示用)
    [SerializeField] private float _specialChargeDelay = 2.0f;   // 初めてダウンしてから必殺技ゲージを最大にするまでの時間

    private eState _state = eState.Waiting;
    private bool _isHalfHp = false;
    private int _phase = 0;      // HPの段階(_phaseHpRatesを下回った数)
    private int _downPhase = 0;  // ダウンした時の段階(段階が進んだらダウンを解除する)

    // 位置は「陣取る画面端からのXオフセット」と「Y座標」で管理し、カメラが動いても画面端を維持する
    private float _entryOffsetX;   // 画面端へ向かう途中の残りXオフセット(0で画面端に到着)
    private float _driftTimer;
    private float _driftAnchorY;   // 漂う中心のY座標
    private float _currentY;

    private eAttackState _attackState = eAttackState.Barrage;
    private float _attackTimer;
    private int _barrageCount;
    private ZelxFireBullet _chargingBullet; // チャージ中の巨大な弾
    private Vector3 _chargeBulletScale;     // 巨大な弾のプレハブの大きさ
    private ParticleSystem _chargeEffect;   // チャージ演出(使い回す)
    private float _chargeShakeTimer;

    // 吹き飛び・落下
    private bool _isKnockdownRequested = false; // ダメージ処理の後に吹き飛ばすための予約
    private Vector2 _knockdownDirection;
    private Vector2 _movePos;       // 吹き飛び・落下中の位置(MovePositionは物理更新時に反映されるため自前で管理する)
    private float _knockbackTimer;
    private float _fallSpeed;
    private bool _isSlowMotion = false;
    private bool _hasDownedBefore = false; // 初回ダウン判定用

    private readonly Queue<ZelxFireBullet> _bulletPool = new Queue<ZelxFireBullet>();
    private readonly Queue<ZelxFireBullet> _chargeBulletPool = new Queue<ZelxFireBullet>();
    private System.Action<ZelxFireBullet> _returnBullet;
    private System.Action<ZelxFireBullet> _returnChargeBullet;

    protected override void _Setup() {
        base._Setup();
        _isRight = !_isOnRightSide; // 画面右端からは左を向き、画面左端からは右を向いて撃つ
        _returnBullet = bullet => _ReturnBullet(_bulletPool, bullet);
        _returnChargeBullet = bullet => _ReturnBullet(_chargeBulletPool, bullet);
        if (_chargeBulletPrefab != null) {
            _chargeBulletScale = _chargeBulletPrefab.transform.localScale;
        }
    }

    // ダウン中だけロックオンできる
    public override bool CanLockOn => _state == eState.Down;

    // Timelineの合図で行動を開始させる
    public void StartActing() {
        if (_state != eState.Waiting) return;

        var cam = Camera.main;
        _driftAnchorY = cam != null ? _ClampYInView(cam, transform.position.y) : transform.position.y;
        _StartActing();
    }

    // 現在の位置から陣取る画面端へ向かって移動し、漂いながら攻撃を始める
    private void _StartActing() {
        _state = eState.Acting;
        _SetWaterShieldVisible(true);

        var cam = Camera.main;
        _entryOffsetX = cam != null ? transform.position.x - _GetEdgeX(cam) : 0f;
        _currentY = transform.position.y;
        _driftTimer = 0f;

        _attackState = eAttackState.Barrage;
        _attackTimer = _fireInterval;
        _barrageCount = 0;
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        // 吹き飛びは自前で行うため、被弾時の吹き飛びは無効にする
        // 跳ね返された自分の弾の無敵時間はプレイヤー向けの値(長い)のため、被弾時の無敵時間は付けない
        bool result = base.Damage(damage, Vector2.zero, 0f, damage_reaction_time, is_trap_damage, ignore_invincibility);
        if (_isDead) {
            return result;
        }

        // HPが半分を切ったらHalfHPを通知する(StageEvent_BossBattleのメッセージ表示等)
        if (!_isHalfHp && _charaParam.CurrentHP <= _charaParam.MaxHP / 2.0f) {
            _isHalfHp = true;
            OnHalfHP?.Invoke();
        }

        // HPが次の段階まで減ったら段階を進める(ダウン中ならダウンが解除される)
        while (_phase < _phaseHpRates.Length && _charaParam.CurrentHP <= _charaParam.MaxHP * _phaseHpRates[_phase]) {
            _phase++;
            _OnPhaseChanged();
        }
        return result;
    }

    // HPの段階が進んだ時の処理
    private void _OnPhaseChanged() {
        // TODO: 弾幕の性質の変化は後程実装
    }

    // 跳ね返された巨大な弾が当たった時に呼ばれる(ダメージ処理の直前)
    public void Knockdown(Vector2 direction) {
        if (_state != eState.Acting) return;
        _isKnockdownRequested = true;
        _knockdownDirection = direction;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        _rb.linearVelocity = Vector2.zero;

        if (_isDead) {
            _CancelCharge();
            return;
        }

        float dt = Time.deltaTime;
        switch (_state) {
            case eState.Acting: {
                    // ダメージ処理で減ったHPを反映した後に吹き飛ばす
                    if (_isKnockdownRequested) {
                        _isKnockdownRequested = false;
                        _StartKnockback();
                        break;
                    }
                    var cam = Camera.main;
                    if (cam == null) break;
                    var position = _UpdateMove(cam, dt);
                    _UpdateAttack(dt, position);
                    break;
                }

            case eState.KnockedBack:
                _UpdateKnockback(dt);
                break;

            case eState.Falling:
                _UpdateFall(dt);
                break;

            case eState.Down:
                // HPが次の段階まで減ったらダウンを強制解除し、陣取る画面端へ戻る
                if (_phase > _downPhase) {
                    _StartActing();
                }
                break;
        }
    }

    // 陣取る画面端を維持して漂う。移動後の位置を返す
    private Vector2 _UpdateMove(Camera cam, float dt) {
        _entryOffsetX = Mathf.MoveTowards(_entryOffsetX, 0f, _entrySpeed * dt);

        // 漂い始めの位置から途切れずに動き出すよう、どちらの軸もオフセット0から始まるサイン波で漂う
        _driftTimer += dt;
        float t = _driftTimer * _driftSpeed;
        float drift_x = (1f - Mathf.Cos(t)) * 0.5f * _driftRangeX * _InwardSign; // 0～_driftRangeXの範囲で画面の内側にだけ漂う
        float target_y = _driftAnchorY + Mathf.Sin(t * 1.3f) * _driftRangeY;
        // 配置位置からの登場中・ダウンからの復帰中は、Y座標も漂う位置へ徐々に合わせる
        _currentY = Mathf.MoveTowards(_currentY, target_y, Mathf.Max(_entrySpeed, _driftSpeed * _driftRangeY * 2f) * dt);
        _currentY = _ClampYInView(cam, _currentY);

        var position = new Vector2(_GetEdgeX(cam) + _entryOffsetX + drift_x, _currentY);
        _rb.MovePosition(position);
        return position;
    }

    private void _UpdateAttack(float dt, Vector2 position) {
        // 画面端に着くまでは撃たない
        if (_entryOffsetX != 0f) {
            return;
        }
        _attackTimer -= dt;

        switch (_attackState) {
            case eAttackState.Barrage:
                if (_attackTimer > 0f) break;
                _attackTimer += _fireInterval;
                _FireBarrage(_GetMuzzlePosition(position));
                _barrageCount++;
                if (_barrageCount >= _barrageCountBeforeCharge && _chargeBulletPrefab != null) {
                    _StartCharge(_GetMuzzlePosition(position));
                }
                break;

            case eAttackState.Charging: {
                    // WaterBulletで消された場合はチャージを中断する
                    if (_chargingBullet == null || _chargingBullet.IsReleased) {
                        _chargingBullet = null;
                        _StopChargeEffect();
                        _ChangeAttackState(eAttackState.AfterCharge, _afterChargeWait);
                        break;
                    }
                    // 本体に付いて動きながら、徐々に大きくなる
                    float rate = _chargeTime > 0f ? Mathf.Clamp01(1f - _attackTimer / _chargeTime) : 1f;
                    _chargingBullet.transform.position = _GetMuzzlePosition(position);
                    _chargingBullet.transform.localScale = _chargeBulletScale * Mathf.Lerp(_chargeStartScale, 1f, rate);
                    _UpdateChargeEffect(_chargingBullet.transform.position, rate, dt);
                    if (_attackTimer > 0f) break;

                    if (_seChargeShoot != null) _audioSource?.PlayOneShot(_seChargeShoot);
                    _chargingBullet.Fire(_FacingDirection, _returnChargeBullet, this);
                    _chargingBullet = null;
                    _StopChargeEffect(); // 撃った瞬間のエフェクトと画面の揺れは、弾側(ZelxFireBullet)で出す
                    _ChangeAttackState(eAttackState.AfterCharge, _afterChargeWait);
                    break;
                }

            case eAttackState.AfterCharge:
                if (_attackTimer > 0f) break;
                _barrageCount = 0;
                _ChangeAttackState(eAttackState.Barrage, _fireInterval);
                break;
        }
    }

    private void _ChangeAttackState(eAttackState state, float timer) {
        _attackState = state;
        _attackTimer = timer;
    }

    // 吹き飛び開始。一瞬ズームとスローモーションを掛ける
    private void _StartKnockback() {
        _CancelCharge();
        _state = eState.KnockedBack;
        _downPhase = _phase;
        _movePos = transform.position;
        _knockbackTimer = 0f;
        _knockdownDirection = new Vector2(_knockdownDirection.x < 0f ? -1f : 1f, 0f); // 横へ真っすぐ吹き飛ぶ
        StartCoroutine(_KnockdownEffectCo());
    }

    private IEnumerator _KnockdownEffectCo() {
        var cinemachine_manager = CinemachineManager.Instance;
        cinemachine_manager?.ZoomOnTarget(transform);
        _isSlowMotion = true;
        Time.timeScale = _slowTimeScale;

        yield return new WaitForSecondsRealtime(_slowDuration);

        _EndSlowMotion();
        cinemachine_manager?.ReturnToPlayer();
    }

    private void _EndSlowMotion() {
        if (!_isSlowMotion) return;
        _isSlowMotion = false;
        Time.timeScale = 1f;
    }

    private void OnDisable() {
        // スローモーション中に消えた場合も時間の流れを戻す
        if (_isSlowMotion) {
            _EndSlowMotion();
            CinemachineManager.Instance?.ReturnToPlayer();
        }
    }

    private void OnDestroy() {
        // 使い回しているチャージ演出は本体と別に置いているため、一緒に消す
        if (_chargeEffect != null) Destroy(_chargeEffect.gameObject);
    }

    // 壁に当たるまで横へ吹き飛ぶ
    private void _UpdateKnockback(float dt) {
        _knockbackTimer += dt;
        float distance = _knockbackSpeed * dt;
        if (_CastBody(_knockdownDirection, distance, _wallLayer, out float hit_distance)) {
            // 壁に叩きつけられた
            _movePos += _knockdownDirection * hit_distance;
            _rb.MovePosition(_movePos);
            if (_seWallHit != null) _audioSource?.PlayOneShot(_seWallHit);
            CinemachineManager.Instance?.ShakeCamera(_wallHitShakeIntensity, 0.2f);
            _StartFall();
            return;
        }
        _movePos += _knockdownDirection * distance;
        _rb.MovePosition(_movePos);

        if (_knockbackTimer >= _knockbackMaxTime) {
            _StartFall();
        }
    }

    private void _StartFall() {
        _state = eState.Falling;
        _fallSpeed = 0f;
    }

    // 地上に着くまで落下する
    private void _UpdateFall(float dt) {
        _fallSpeed = Mathf.Min(_fallSpeed + _fallGravity * dt, _maxFallSpeed);
        float distance = _fallSpeed * dt;
        if (_CastBody(Vector2.down, distance, _groundLayer, out float hit_distance)) {
            // 地上に落ちてダウンする
            _movePos += Vector2.down * hit_distance;
            _rb.MovePosition(_movePos);
            if (_seLand != null) _audioSource?.PlayOneShot(_seLand);
            CinemachineManager.Instance?.ShakeCamera(_landShakeIntensity, 0.2f);
            _StartDown();
            return;
        }
        _movePos += Vector2.down * distance;
        _rb.MovePosition(_movePos);
    }

    private void _StartDown() {
        _state = eState.Down;
        _SetWaterShieldVisible(false); // ダウン中は無防備なのでシールドを外す

        // 初めてダウンした時は、メッセージを表示し、少し後に必殺技ゲージを最大にする
        if (!_hasDownedBefore) {
            _hasDownedBefore = true;
            if (_firstDownMessageObject != null) _firstDownMessageObject.SetActive(true);
            StartCoroutine(_ChargeSpecialCo());
        }
    }

    private void _SetWaterShieldVisible(bool is_visible) {
        if (_waterShield != null) _waterShield.SetActive(is_visible);
    }

    private IEnumerator _ChargeSpecialCo() {
        yield return new WaitForSeconds(_specialChargeDelay);
        foreach (var ability in FindObjectsByType<Ability_Base>(FindObjectsSortMode.None)) {
            ability.ForceCharge();
        }
    }

    // 体の当たり判定を指定方向へ動かした時に地形に当たるか調べる
    // (体の当たり判定はトリガーのため、物理演算では地形に止められない)
    private bool _CastBody(Vector2 direction, float distance, LayerMask layer, out float hit_distance) {
        var bounds = _col.bounds;
        Vector2 offset = (Vector2)bounds.center - (Vector2)transform.position;
        // 進行方向と垂直な幅は少し細くする(壁に叩きつけられた後に落下する時、横の壁に触れて着地扱いにならないように)
        Vector2 size = bounds.size;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) {
            size.y *= 0.8f;
        } else {
            size.x *= 0.8f;
        }
        var hit = Physics2D.BoxCast(_movePos + offset, size, 0f, direction, distance, layer);
        hit_distance = hit.collider != null ? hit.distance : 0f;
        return hit.collider != null;
    }

    // 発射位置から向いている方向へ扇状に弾幕を発射する
    private void _FireBarrage(Vector3 spawn_pos) {
        if (_bulletPrefab == null || _bulletCount <= 0) return;

        if (_seShoot != null) _audioSource?.PlayOneShot(_seShoot);

        for (int i = 0; i < _bulletCount; i++) {
            // 向いている方向(右なら0度、左なら180度)を中心に、_fanAngleの範囲へ等間隔に並べる
            float t = _bulletCount > 1 ? (float)i / (_bulletCount - 1) : 0.5f;
            float center = _isOnRightSide ? 180f : 0f;
            float angle = (center - _fanAngle * 0.5f + _fanAngle * t) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var bullet = _GetBullet(_bulletPool, _bulletPrefab, spawn_pos);
            bullet.Fire(direction, _returnBullet, this);
        }
    }

    // 巨大な弾のチャージを開始する
    private void _StartCharge(Vector3 spawn_pos) {
        if (_seCharge != null) _audioSource?.PlayOneShot(_seCharge);

        _chargingBullet = _GetBullet(_chargeBulletPool, _chargeBulletPrefab, spawn_pos);
        _chargingBullet.transform.localScale = _chargeBulletScale * _chargeStartScale;
        _chargingBullet.Hold(_returnChargeBullet, this);
        _ChangeAttackState(eAttackState.Charging, _chargeTime);
        _StartChargeEffect(spawn_pos);
    }

    // チャージ演出を始める(光の粒が弾へ集まり始め、画面が小さく揺れ始める)
    private void _StartChargeEffect(Vector3 position) {
        _chargeShakeTimer = 0f;
        if (_chargeEffectPrefab == null) return;
        if (_chargeEffect == null) {
            _chargeEffect = Instantiate(_chargeEffectPrefab, position, Quaternion.identity);
        }
        _chargeEffect.transform.position = position;
        var emission = _chargeEffect.emission;
        emission.rateOverTime = _chargeEffectRateRange.x;
        _chargeEffect.Play();
    }

    // チャージが進むほど、集まる光の粒を増やし、画面の揺れを強くする
    private void _UpdateChargeEffect(Vector3 position, float rate, float dt) {
        if (_chargeEffect != null) {
            _chargeEffect.transform.position = position;
            var emission = _chargeEffect.emission;
            emission.rateOverTime = Mathf.Lerp(_chargeEffectRateRange.x, _chargeEffectRateRange.y, rate);
        }
        _chargeShakeTimer -= dt;
        if (_chargeShakeTimer <= 0f) {
            _chargeShakeTimer = _chargeShakeInterval;
            CinemachineManager.Instance?.ShakeCamera(Mathf.Lerp(_chargeShakeRange.x, _chargeShakeRange.y, rate), _chargeShakeInterval);
        }
    }

    // チャージ演出を止める(新しい粒は出さず、出ている粒は自然に消える)
    private void _StopChargeEffect() {
        if (_chargeEffect != null) {
            _chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // チャージ中の弾を発射せずに消す
    private void _CancelCharge() {
        _StopChargeEffect();
        if (_chargingBullet == null) return;
        if (!_chargingBullet.IsReleased) {
            _chargingBullet.Release();
        }
        _chargingBullet = null;
    }

    private Vector3 _GetMuzzlePosition(Vector2 body_position) {
        // MovePositionは物理更新時に反映されるため、移動先の位置から発射位置を求める
        var muzzle_offset = _muzzle != null ? _muzzle.position - transform.position : Vector3.zero;
        // 発射位置は右向きを基準に置かれているため、左を向いている時は左右を反転する
        if (_isOnRightSide) muzzle_offset.x = -muzzle_offset.x;
        return new Vector3(body_position.x, body_position.y, transform.position.z) + muzzle_offset;
    }

    // プールから弾を取り出す(空なら新規生成)
    private ZelxFireBullet _GetBullet(Queue<ZelxFireBullet> pool, ZelxFireBullet prefab, Vector3 position) {
        while (pool.Count > 0) {
            var pooled = pool.Dequeue();
            if (pooled == null) continue; // シーン遷移等で破棄済みのものは捨てる
            pooled.transform.position = position;
            pooled.gameObject.SetActive(true);
            return pooled;
        }
        return Instantiate(prefab, position, Quaternion.identity);
    }

    // 弾をプールへ返却する
    private void _ReturnBullet(Queue<ZelxFireBullet> pool, ZelxFireBullet bullet) {
        // 本体が先に破棄されていた場合は、返却先が無いので弾ごと消す
        if (this == null) {
            Destroy(bullet.gameObject);
            return;
        }
        bullet.gameObject.SetActive(false);
        pool.Enqueue(bullet);
    }

    // 体がちょうどカメラに収まる、陣取る側の画面端のX座標
    private float _GetEdgeX(Camera cam) {
        float half_width = GetCharacterSize().x * 0.5f + _edgeMargin;
        return _isOnRightSide
            ? _ViewportToWorld(cam, 1f, 0.5f).x - half_width
            : _ViewportToWorld(cam, 0f, 0.5f).x + half_width;
    }

    // 画面の内側へ向かう向き(右端に陣取っていれば左、左端なら右)
    private float _InwardSign => _isOnRightSide ? -1f : 1f;

    // 向いている方向(撃つ方向)
    private Vector2 _FacingDirection => _isOnRightSide ? Vector2.left : Vector2.right;

    // 自身の奥行き(Z)におけるビューポート座標のワールド座標
    // (カメラは透視投影のため、orthographicSizeでは実際に映る範囲を求められない)
    private Vector3 _ViewportToWorld(Camera cam, float x, float y) {
        float depth = transform.position.z - cam.transform.position.z;
        return cam.ViewportToWorldPoint(new Vector3(x, y, depth));
    }

    // 体が画面の上下からはみ出さないようにY座標を制限する
    private float _ClampYInView(Camera cam, float y) {
        float body_half_height = GetCharacterSize().y * 0.5f;
        float min_y = _ViewportToWorld(cam, 0.5f, 0f).y + body_half_height;
        float max_y = _ViewportToWorld(cam, 0.5f, 1f).y - body_half_height;
        return min_y <= max_y ? Mathf.Clamp(y, min_y, max_y) : y;
    }
}
