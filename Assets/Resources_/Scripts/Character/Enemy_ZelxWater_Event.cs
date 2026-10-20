using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// イベント用の「水のゼルクス」
/// 逃げるプレイヤーを画面左端から狙い撃つ。攻撃は当たるが倒されることはない
/// 最初から登場し、カメラに映るギリギリの左端を維持して漂いながら、
/// 漂う→発射地点のうちプレイヤーのY座標に近い方のY座標へ移動→巨大な弾をチャージして発射→待機 を繰り返す
/// 射撃モードはプレイヤーが範囲に入るたびに切り替わる(最初はチャージ射撃のみ)
///   _barrageAreaに入る: 弾幕1(Enemy_ZelxWaterと同じ)を決まった回数撃つごとに巨大な弾をチャージして撃つ(入ってから少し後に最初の弾幕1を撃つ)
///   _chargeAreaに入る : チャージ射撃のみを撃つ
/// プレイヤーがゴール範囲に触れたら非表示になる
/// 通常時はWaterShieldでFireBulletを無効化し、スタン中のみWaterShieldを外してFireBulletが当たるようになる
/// </summary>
public class Enemy_ZelxWater_Event : Enemy_Base, IDamageSourceBlocker {
    private enum eState {
        Drifting,   // 左端で漂う
        MovingToFirePoint, // 発射地点と同じY座標を目指す
        Charging,   // 巨大な弾をチャージ中(チャージ後に発射する)
        Barrage,    // 弾幕1を撃っている
        Firing,     // 発射後の待機
    }

    [Header("位置")]
    [SerializeField] private float _edgeMargin = 0.0f;   // 画面左端からの余白(0で体がちょうど画面に収まる位置)
    [SerializeField] private float _edgeSmoothTime = 0.3f; // 画角が急に変わった時に、新しい画面端の位置へ移動するまでの時間の目安
    [SerializeField] private Collider2D _goalArea;       // プレイヤーがこの範囲に触れたら非表示にする

    [Header("シールド")]
    [SerializeField] private GameObject _waterShield;    // 通常時に表示し、FireBulletを無効化する。スタン中は外す

    [Header("漂う")]
    [SerializeField] private float _driftTime = 3.0f;    // 左端で漂う時間
    [SerializeField] private float _driftSpeed = 1.5f;   // 漂う速さ
    [SerializeField] private float _driftRangeX = 0.5f;  // 左端から内側へ漂う幅(画面外へははみ出さない)
    [SerializeField] private float _driftRangeY = 1.0f;  // 上下に漂う幅

    [Header("射撃")]
    [SerializeField] private Transform[] _firePoints = new Transform[2]; // 弾の発射座標(プレイヤーのY座標に近いものを選ぶ)
    [SerializeField] private ZelxFireBullet _bulletPrefab; // 発射する弾(Zelxの位置から右へまっすぐ飛ばす)
    [SerializeField] private float _moveSpeedY = 4.0f;   // 発射地点のY座標へ向かう速さ
    [SerializeField] private float _afterFireWait = 2.0f; // 発射してから再び漂うまでの時間

    [Header("チャージ射撃(巨大な弾)")]
    [SerializeField] private float _chargeTime = 1.0f;       // チャージ時間
    [SerializeField] private float _chargeStartScale = 0.2f; // チャージ開始時の弾の大きさ(プレハブの大きさに対する割合。チャージ完了で1になる)
    [SerializeField] private AudioClip _seCharge;            // チャージ開始時の効果音
    [SerializeField] private AudioClip _seChargeShoot;       // 巨大な弾の発射時の効果音
    [SerializeField] private ParticleSystem _chargeEffectPrefab; // チャージ中に弾へ光の粒が集まる演出
    [SerializeField] private Vector2 _chargeEffectRateRange = new Vector2(15f, 90f); // チャージ演出の粒の量(チャージ開始→完了)
    [SerializeField] private float _chargeShakeInterval = 0.2f; // チャージ中に画面を揺らす間隔
    [SerializeField] private Vector2 _chargeShakeRange = new Vector2(0.05f, 0.4f); // チャージ中の画面の揺れの強さ(チャージ開始→完了)

    [Header("射撃モードの切り替え(範囲に入るたびに切り替える)")]
    [SerializeField] private GameObject _barrageArea;         // この範囲(子も含む全てのCollider2D)に入ると、弾幕1とチャージ射撃を織り交ぜて撃つようになる
    [SerializeField] private GameObject _chargeArea;          // この範囲(子も含む全てのCollider2D)に入ると、チャージ射撃のみを撃つようになる
    [SerializeField] private float _barrageStartDelay = 1.0f; // _barrageAreaに入ってから、最初の弾幕1を撃つまでの時間

    [Header("弾幕1")]
    [SerializeField] private int _barragesBeforeCharge = 2;   // 弾幕1をこの回数撃ったら、巨大な弾をチャージして撃つ
    [SerializeField] private ZelxFireBullet _smallBulletPrefab; // 弾幕1の弾
    [SerializeField] private int _missBulletCount = 10;       // プレイヤーの脇をかすめる、弧を描く弾の数
    [SerializeField] private int _aimBulletCount = 3;         // プレイヤーへ真っすぐ向かう弾の数
    [SerializeField] private float _barrage1FireInterval = 0.06f; // 1発ずつ撃つ間隔
    [SerializeField] private Vector2 _bulletSpeedRateRange = new Vector2(0.7f, 1.3f); // 初速の倍率の範囲
    [SerializeField] private Vector2 _missTurnRateRange = new Vector2(60f, 120f);     // かすめる弾の曲がる速さの範囲(度/秒)
    [SerializeField] private Vector2 _missDistanceRange = new Vector2(2.5f, 5.0f);    // かすめる弾が、プレイヤーからどれだけ横に外れた所を通るか
    [SerializeField] private Vector2 _barrage1LifetimeRange = new Vector2(1.0f, 2.0f); // 弾の寿命の範囲
    [SerializeField] private AudioClip _seShoot;              // 弾幕1の発射時の効果音

    private eState _state = eState.Drifting;
    private float _stateTimer = 0f;
    private int _firePointIndex = 0;

    // 位置は「カメラ左端からのXオフセット」と「Y座標」で管理し、カメラが動いても左端を維持する
    private float _offsetX;     // 左端の定位置からのXオフセット(正で画面内側)
    private float _currentY;
    private float _driftAnchorY; // 漂い始めた時のY座標(ここを中心に上下に漂う)
    private bool _isEdgeInitialized = false;
    private float _edgeFromCameraX;  // カメラから見た画面左端の定位置のXオフセット(画角の変化に滑らかに追従する)
    private float _edgeVelocityX;    // SmoothDamp用
    private float _clampVelocityY;   // SmoothDamp用

    private ZelxFireBullet _chargingBullet; // チャージ中の巨大な弾
    private Vector3 _chargeBulletScale;     // 巨大な弾のプレハブの大きさ
    private ParticleSystem _chargeEffect;   // チャージ演出(使い回す)
    private float _chargeShakeTimer;
    private bool _isBarrageEnabled = false; // true: 弾幕1とチャージ射撃を織り交ぜて撃つ / false: チャージ射撃のみ
    private float _barrageStartTimer = -1f;  // 最初の弾幕1を撃つまでの残り時間(負なら待っていない)
    private Collider2D[] _barrageAreaColliders;
    private Collider2D[] _chargeAreaColliders;
    private bool _wasInBarrageArea;          // 前のフレームで_barrageAreaに入っていたか(入った瞬間だけ切り替える)
    private bool _wasInChargeArea;
    private int _barrageCount;              // 最後に巨大な弾を撃ってから撃った弾幕1の回数
    private readonly List<bool> _barrage1Queue = new List<bool>(); // 弾幕1でこれから撃つ弾(trueで狙う弾)
    private float _barrageTimer;

    private bool _isStunned = false; // true: スタン中(WaterShieldを外し、FireBulletが当たる)
    public bool IsStunned => _isStunned;

    protected override void _Setup() {
        base._Setup();
        _isRight = true; // 画面左端から右向きに狙う
        _currentY = transform.position.y;
        _ChangeState(eState.Drifting); // 最初から左端に登場して漂う
        _SetStunned(false); // 通常時はWaterShieldを付けておく
        if (_bulletPrefab != null) _chargeBulletScale = _bulletPrefab.transform.localScale;
        _barrageAreaColliders = _barrageArea != null ? _barrageArea.GetComponentsInChildren<Collider2D>(true) : new Collider2D[0];
        _chargeAreaColliders = _chargeArea != null ? _chargeArea.GetComponentsInChildren<Collider2D>(true) : new Collider2D[0];
    }

    /// <summary>
    /// スタンさせる。WaterShieldを外し、FireBulletが当たるようになる
    /// </summary>
    public void StartStun() {
        _SetStunned(true);
    }

    /// <summary>
    /// スタンを解除する。WaterShieldを付け直し、FireBulletが当たらなくなる
    /// </summary>
    public void EndStun() {
        _SetStunned(false);
    }

    private void _SetStunned(bool is_stunned) {
        _isStunned = is_stunned;
        if (_waterShield != null) {
            _waterShield.SetActive(!is_stunned);
        }
    }

    // 通常時(WaterShieldあり)はFireBulletを防ぐ
    // DamageZoneはこの判定で防がれると、ロックオン(ヒットコールバック)・ヒットエフェクト・ダメージ処理を行わずに弾が消える
    public bool IsBlockedDamageSource(DamageZone source) {
        return !_isStunned && source != null && source.GetComponentInParent<FireBullet>() != null;
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        // 攻撃は当たる(被弾演出・効果音あり)が、HPは減らさず倒されないようにする。吹き飛びも無効
        return base.Damage(0, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        _rb.linearVelocity = Vector2.zero;

        var cam = Camera.main;
        if (cam == null || _isDead) {
            return;
        }

        // ゴール範囲に触れたら非表示にする
        if (_IsPlayerInGoalArea()) {
            _CancelCharge();
            gameObject.SetActive(false);
            return;
        }
        float dt = Time.deltaTime;
        // 範囲に入るたびに射撃モードを切り替える。弾幕1を織り交ぜるモードに入ったら、少し後に最初の弾幕1を撃つ
        _UpdateFireMode();
        if (_barrageStartTimer >= 0f) {
            _barrageStartTimer -= dt;
            if (_barrageStartTimer < 0f) _ForceBarrage1();
        }

        _stateTimer += dt;

        switch (_state) {
            case eState.Drifting:
                _UpdateDrift();
                if (_stateTimer >= _driftTime) {
                    // 移動を始める時点で、プレイヤーのY座標に近い発射地点を選ぶ
                    int index = _GetNearestFirePointIndex();
                    if (index >= 0) {
                        _firePointIndex = index;
                        _ChangeState(eState.MovingToFirePoint);
                    }
                }
                break;

            case eState.MovingToFirePoint: {
                    // 漂いで内側に寄った分を戻しながら、発射地点のY座標を目指す
                    float target_y = _ClampYInView(cam, _firePoints[_firePointIndex].position.y);
                    _currentY = Mathf.MoveTowards(_currentY, target_y, _moveSpeedY * dt);
                    _offsetX = Mathf.MoveTowards(_offsetX, 0f, _moveSpeedY * dt);
                    if (Mathf.Approximately(_currentY, target_y) && _offsetX <= 0f) {
                        // 弾幕1を撃つようになって以降は、弾幕1を決まった回数撃つごとに巨大な弾をチャージして撃つ
                        if (_isBarrageEnabled && _barrageCount < _barragesBeforeCharge && _smallBulletPrefab != null) {
                            _barrageCount++;
                            _StartBarrage1();
                        } else {
                            _barrageCount = 0;
                            _StartCharge();
                        }
                    }
                    break;
                }

            case eState.Charging:
                _UpdateCharge(dt);
                break;

            case eState.Barrage:
                _UpdateBarrage1(dt);
                break;

            case eState.Firing:
                if (_stateTimer >= _afterFireWait) {
                    _ChangeState(eState.Drifting);
                }
                break;
        }

        // 画面端の位置はカメラからの相対位置で管理し、カメラの移動にはそのまま付いていく。
        // 画角が急に変わって画面端の位置が変わった時は、そこへ滑らかに移動する
        float target_edge_x = _GetLeftEdgeX(cam) - cam.transform.position.x;
        float clamped_y = _ClampYInView(cam, _currentY);
        if (!_isEdgeInitialized) {
            _isEdgeInitialized = true;
            _edgeFromCameraX = target_edge_x;
            _currentY = clamped_y;
        } else {
            _edgeFromCameraX = Mathf.SmoothDamp(_edgeFromCameraX, target_edge_x, ref _edgeVelocityX, _edgeSmoothTime, Mathf.Infinity, dt);
            _currentY = Mathf.SmoothDamp(_currentY, clamped_y, ref _clampVelocityY, _edgeSmoothTime, Mathf.Infinity, dt);
        }
        float x = cam.transform.position.x + _edgeFromCameraX + _offsetX;
        _rb.MovePosition(new Vector2(x, _currentY));
        // チャージ中の弾は本体に付いて動く(MovePositionは物理更新時に反映されるため、移動先の位置に合わせる)
        if (_chargingBullet != null) {
            var position = new Vector3(x, _currentY, transform.position.z);
            _chargingBullet.transform.position = position;
            if (_chargeEffect != null) _chargeEffect.transform.position = position;
        }
    }

    private void _ChangeState(eState state) {
        _state = state;
        _stateTimer = 0f;
        if (state == eState.Drifting) {
            _driftAnchorY = _currentY;
        }
    }

    // 漂い始めの位置から途切れずに動き出すよう、どちらの軸もオフセット0から始まるサイン波で漂う
    private void _UpdateDrift() {
        float t = _stateTimer * _driftSpeed;
        _offsetX = (1f - Mathf.Cos(t)) * 0.5f * _driftRangeX; // 0～_driftRangeXの範囲で内側にだけ漂う
        _currentY = _driftAnchorY + Mathf.Sin(t * 1.3f) * _driftRangeY;
    }

    // 巨大な弾のチャージを始める(光の粒が弾へ集まり、画面が小さく揺れ、弾が徐々に大きくなる)
    private void _StartCharge() {
        if (_bulletPrefab == null) {
            _ChangeState(eState.Firing);
            return;
        }
        if (_seCharge != null) _audioSource?.PlayOneShot(_seCharge);
        _chargingBullet = Instantiate(_bulletPrefab, transform.position, Quaternion.identity);
        _chargingBullet.transform.localScale = _chargeBulletScale * _chargeStartScale;
        _chargingBullet.Hold(null, this); // 返却先を渡さないため、消える時はDestroyされる。自分自身には当たらない
        _chargeShakeTimer = 0f;
        if (_chargeEffectPrefab != null) {
            if (_chargeEffect == null) _chargeEffect = Instantiate(_chargeEffectPrefab, transform.position, Quaternion.identity);
            _chargeEffect.transform.position = transform.position;
            var emission = _chargeEffect.emission;
            emission.rateOverTime = _chargeEffectRateRange.x;
            _chargeEffect.Play();
        }
        _ChangeState(eState.Charging);
    }

    // チャージが進むほど弾を大きくし、集まる光の粒を増やし、画面の揺れを強くする。チャージが終わったら右へまっすぐ撃つ
    private void _UpdateCharge(float dt) {
        // WaterBulletで消された場合はチャージを中断する
        if (_chargingBullet == null || _chargingBullet.IsReleased) {
            _chargingBullet = null;
            _StopChargeEffect();
            _ChangeState(eState.Firing);
            return;
        }
        float rate = _chargeTime > 0f ? Mathf.Clamp01(_stateTimer / _chargeTime) : 1f;
        _chargingBullet.transform.localScale = _chargeBulletScale * Mathf.Lerp(_chargeStartScale, 1f, rate);
        if (_chargeEffect != null) {
            var emission = _chargeEffect.emission;
            emission.rateOverTime = Mathf.Lerp(_chargeEffectRateRange.x, _chargeEffectRateRange.y, rate);
        }
        _chargeShakeTimer -= dt;
        if (_chargeShakeTimer <= 0f) {
            _chargeShakeTimer = _chargeShakeInterval;
            CinemachineManager.Instance?.ShakeCamera(Mathf.Lerp(_chargeShakeRange.x, _chargeShakeRange.y, rate), _chargeShakeInterval);
        }
        if (rate < 1f) return;

        // 発射(撃った瞬間のエフェクトと画面の揺れは、弾側(ZelxFireBullet)で出す)
        if (_seChargeShoot != null) _audioSource?.PlayOneShot(_seChargeShoot);
        _chargingBullet.Fire(Vector2.right, null, this);
        _chargingBullet = null;
        _StopChargeEffect();
        _ChangeState(eState.Firing);
    }

    // チャージ演出を止める(新しい粒は出さず、出ている粒は自然に消える)
    private void _StopChargeEffect() {
        if (_chargeEffect != null) _chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // チャージ中の弾を発射せずに消す
    private void _CancelCharge() {
        _StopChargeEffect();
        if (_chargingBullet != null && !_chargingBullet.IsReleased) _chargingBullet.Release();
        _chargingBullet = null;
    }

    // 今の行動を中断して、その場から即座に弾幕1を撃つ(チャージ中ならチャージをやめる)
    private void _ForceBarrage1() {
        if (_smallBulletPrefab == null) return;
        _CancelCharge();
        _StartBarrage1();
    }

    // 弾幕1を始める(かすめる弾と狙う弾を混ぜ、撃つ順番をばらばらにする)
    private void _StartBarrage1() {
        ZelxBarrage1.CreateQueue(_barrage1Queue, _missBulletCount, _aimBulletCount);
        _barrageTimer = 0f;
        _ChangeState(eState.Barrage);
    }

    private void _UpdateBarrage1(float dt) {
        _barrageTimer -= dt;
        if (_barrageTimer > 0f) return;
        if (_barrage1Queue.Count > 0) {
            bool is_aim = _barrage1Queue[_barrage1Queue.Count - 1];
            _barrage1Queue.RemoveAt(_barrage1Queue.Count - 1);
            if (_seShoot != null) _audioSource?.PlayOneShot(_seShoot);
            var bullet = Instantiate(_smallBulletPrefab, transform.position, Quaternion.identity);
            var settings = new ZelxBarrage1.Settings {
                speedRateRange = _bulletSpeedRateRange,
                missTurnRateRange = _missTurnRateRange,
                missDistanceRange = _missDistanceRange,
                lifetimeRange = _barrage1LifetimeRange,
            };
            ZelxBarrage1.Fire(bullet, transform.position, is_aim, Vector2.right, settings, null, this); // 消える時はDestroyされる
        }
        if (_barrage1Queue.Count == 0) {
            _ChangeState(eState.Firing);
        } else {
            _barrageTimer += _barrage1FireInterval;
        }
    }

    private void OnDisable() {
        // 非表示になった時(ゴール・コインレール中など)は、チャージ中の弾を残さない
        _CancelCharge();
    }

    private void OnDestroy() {
        // 使い回しているチャージ演出は本体と別に置いているため、一緒に消す
        if (_chargeEffect != null) Destroy(_chargeEffect.gameObject);
    }

    // プレイヤーが_barrageArea・_chargeAreaに入った瞬間に、射撃モードを切り替える
    private void _UpdateFireMode() {
        bool in_barrage = _IsPlayerInAnyArea(_barrageAreaColliders);
        bool in_charge = _IsPlayerInAnyArea(_chargeAreaColliders);
        if (in_barrage && !_wasInBarrageArea && !_isBarrageEnabled) {
            // 弾幕1とチャージ射撃を織り交ぜて撃つ。最初の弾幕1は入ってから少し後に撃つ
            _isBarrageEnabled = true;
            _barrageCount = 0;
            _barrageStartTimer = _barrageStartDelay;
        }
        if (in_charge && !_wasInChargeArea && _isBarrageEnabled) {
            // チャージ射撃のみを撃つ(撃っている途中の弾幕1はそのまま撃ち切る)
            _isBarrageEnabled = false;
            _barrageStartTimer = -1f;
        }
        _wasInBarrageArea = in_barrage;
        _wasInChargeArea = in_charge;
    }

    private bool _IsPlayerInAnyArea(Collider2D[] areas) {
        if (areas == null) return false;
        foreach (var area in areas) {
            if (area != null && area.enabled && _IsPlayerInArea(area)) return true;
        }
        return false;
    }

    private bool _IsPlayerInGoalArea() {
        return _IsPlayerInArea(_goalArea);
    }

    private bool _IsPlayerInArea(Collider2D area) {
        if (area == null) return false;
        var player = PlayerCharacterManager.Current as Player_Character;
        return player != null && area.OverlapPoint(player.transform.position);
    }

    // 発射地点のうち、プレイヤーのY座標に最も近いものの番号を返す(未設定の発射地点は除く。1つも無ければ-1)
    // プレイヤーがいない時は、自身のY座標に近いものを選ぶ
    private int _GetNearestFirePointIndex() {
        if (_firePoints == null) return -1;
        var player = PlayerCharacterManager.Current;
        float target_y = player != null ? player.transform.position.y : _currentY;

        int nearest = -1;
        float nearest_distance = float.MaxValue;
        for (int i = 0; i < _firePoints.Length; i++) {
            if (_firePoints[i] == null) continue;
            float distance = Mathf.Abs(_firePoints[i].position.y - target_y);
            if (distance < nearest_distance) {
                nearest_distance = distance;
                nearest = i;
            }
        }
        return nearest;
    }

    // 体がちょうどカメラに収まる左端のX座標
    private float _GetLeftEdgeX(Camera cam) {
        return _ViewportToWorld(cam, 0f, 0.5f).x + GetCharacterSize().x * 0.5f + _edgeMargin;
    }

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

    private void OnDrawGizmosSelected() {
        // 発射地点のY座標を可視化
        if (_firePoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var point in _firePoints) {
            if (point == null) continue;
            Gizmos.DrawLine(point.position + Vector3.left * 3f, point.position + Vector3.right * 3f);
        }
    }
}
