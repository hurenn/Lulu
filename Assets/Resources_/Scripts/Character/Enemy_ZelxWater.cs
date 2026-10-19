using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ボス「水のゼルクス」
/// 最初は止まっていて、Timelineの合図(ActivateMarker)で行動を開始する
/// 行動開始後は陣取る画面端(既定は右端)へ移動し、カメラに映るギリギリの画面端を維持して漂いながら、
/// 最後に弾を撃ってから一定時間ごとに攻撃する。弾幕を2回撃ったら、巨大な弾をチャージして1回真っすぐ撃つ(その弾が消えるまで次の行動をしない)
/// 弾幕は、HPが2/3より多い間は弾幕1のみ、以下になると確率で弾幕2になる
///   弾幕1: プレイヤーの脇をかすめる弧を描く弾と、プレイヤーを狙う弾を、順不同・初速ばらばらで撃つ
///   弾幕2: 巨大な弾を山なりに撃ち、画面の左上で爆発させ、そこから真下～真右の扇状に高速の弾をまき散らす(着弾で爆発)
/// 跳ね返された巨大な弾が当たると、吹き飛んで壁に叩きつけられ、地上に落ちてダウンする
/// ダウンはHPが次の段階(2/3, 1/3)まで減ると強制解除され、再び画面端へ戻って行動する
/// </summary>
public class Enemy_ZelxWater : Enemy_Base, IActivatable, IKnockdownTarget, IDamageSourceFilter {
    private enum eState {
        Waiting,     // Timelineの合図を待っている
        Acting,      // 画面端で漂いながら攻撃する
        KnockedBack, // 吹き飛ばされて壁へ向かっている
        Falling,     // 壁に叩きつけられた後、地上へ落ちている
        Down,        // ダウン中(無防備で何もしない)
    }

    private enum eAttackState {
        Interval,    // 次の攻撃までの待機(最後に弾を撃ってから一定時間)
        Barrage1,    // 弾幕1: かすめる弾と狙う弾を順不同で撃っている
        Barrage2,    // 弾幕2: 山なりの巨大な弾が爆発し、扇状の弾を撃ち終えるのを待っている
        Charging,    // 巨大な弾をチャージ中(チャージ後に真っすぐ撃つ)
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

    [Header("行動")]
    [SerializeField] private float _attackInterval = 5.0f;   // 最後に弾を撃ってから次の攻撃を始めるまでの時間
    [SerializeField] private float _firstAttackDelay = 1.0f; // 画面端に着いてから最初の攻撃を始めるまでの時間
    [SerializeField] private int _barragesBeforeStraight = 2; // 弾幕をこの回数撃ったら、巨大な弾をチャージして真っすぐ撃つ(HPに関係なく)
    [SerializeField] private float _lobAttackHpRate = 2f / 3f; // HPがこの割合以下になると、弾幕2を撃つことがある
    [SerializeField, Range(0f, 1f)] private float _lobAttackChance = 0.5f; // HPが減った後に弾幕2を撃つ確率(撃たない時は弾幕1)

    [Header("弾幕1: かすめる弾と狙う弾(小さな弾)")]
    [SerializeField] private ZelxFireBullet _bulletPrefab;  // 発射する弾のプレハブ(プールで再利用する)
    [SerializeField] private int _missBulletCount = 10;     // プレイヤーの脇をかすめる、弧を描く弾の数
    [SerializeField] private int _aimBulletCount = 3;       // プレイヤーへ真っすぐ向かう弾の数
    [SerializeField] private float _barrage1FireInterval = 0.06f; // 1発ずつ撃つ間隔
    [SerializeField] private Vector2 _bulletSpeedRateRange = new Vector2(0.7f, 1.3f); // 初速の倍率の範囲(弾ごとにばらばら)
    [SerializeField] private Vector2 _missTurnRateRange = new Vector2(60f, 120f);     // かすめる弾の曲がる速さの範囲(度/秒。向きは左右ランダム)
    [SerializeField] private Vector2 _missDistanceRange = new Vector2(2.5f, 5.0f);    // かすめる弾が、プレイヤーからどれだけ横に外れた所を通るか
    [SerializeField] private Vector2 _barrage1LifetimeRange = new Vector2(1.0f, 2.0f); // 弾の寿命の範囲(撃ってからこの時間で爆発して消える。地形では消えない)
    [SerializeField] private AudioClip _seShoot;            // 発射時の効果音

    [Header("チャージ弾(巨大)")]
    [SerializeField] private ZelxFireBullet _chargeBulletPrefab; // チャージして撃つ巨大な弾のプレハブ(プールで再利用する)
    [SerializeField] private float _chargeTime = 3.0f;     // チャージ時間
    [SerializeField] private float _chargeStartScale = 0.2f; // チャージ開始時の弾の大きさ(プレハブの大きさに対する割合。チャージ完了で1になる)
    [SerializeField] private AudioClip _seCharge;          // チャージ開始時の効果音
    [SerializeField] private AudioClip _seChargeShoot;     // 巨大な弾の発射時の効果音
    [SerializeField] private ParticleSystem _chargeEffectPrefab; // チャージ中に弾へ光の粒が集まる演出
    [SerializeField] private Vector2 _chargeEffectRateRange = new Vector2(15f, 90f); // チャージ演出の粒の量(チャージ開始→完了)
    [SerializeField] private float _chargeShakeInterval = 0.2f; // チャージ中に画面を揺らす間隔
    [SerializeField] private Vector2 _chargeShakeRange = new Vector2(0.05f, 0.4f); // チャージ中の画面の揺れの強さ(チャージ開始→完了)

    [Header("弾幕2: 山なりの巨大な弾→空中で爆発→扇状にまき散らす弾")]
    [SerializeField] private float _lobFlightTime = 1.5f;  // 撃ってから爆発するまでの時間
    [SerializeField] private float _lobGravity = 6.0f;     // 山なりの重力加速度(大きいほど高く上がる)
    [SerializeField] private Vector2 _lobExplodeViewport = new Vector2(0.15f, 0.85f); // 爆発させる位置(画面内の位置。右端に陣取る時の値で、左端の時は左右反転する)
    [SerializeField] private int _fanBulletCount = 20;        // 爆発地点から扇状にまき散らす弾の数
    [SerializeField] private Vector2 _fanAngleRange = new Vector2(-90f, 0f); // 扇の向きの範囲(度。右端に陣取る時の値で、真下→真右。左端の時は左右反転する)
    [SerializeField] private float _fanFireInterval = 0.03f;  // 1発ずつ撃つ間隔(順不同)
    [SerializeField] private float _lobShotSpeedRate = 1.5f;  // 弾の速さの倍率(小さな弾の元の速さに対して)

    [Header("巨大な弾の跳ね返し返し")]
    [SerializeField] private int[] _reflectBackCountsByPhase = { 0, 1, 3 }; // HPの段階ごとに、跳ね返されてきた巨大な弾を跳ね返し返す回数(段階0: HP2/3超, 1: 2/3以下, 2: 1/3以下)
    [SerializeField] private HitEffect _reflectBackHitEffectPrefab; // 跳ね返し返した時のヒットエフェクト
    [SerializeField] private HitEffect.eType _reflectBackHitEffectType = HitEffect.eType.Ice;
    [SerializeField] private float _reflectBackHitEffectSize = 1.3f;
    [SerializeField] private AudioClip _seReflectBack;               // 跳ね返し返した時の効果音
    [SerializeField] private float _reflectBackHitStopTime = 0.4f;   // 跳ね返し返した時のヒットストップの時間(ゼルクスと弾が止まる)
    [SerializeField] private float _reflectBackHitStopWait = 0.05f;  // ヒットストップが始まるまでの間

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

    [Header("撃破演出")]
    [SerializeField] private AudioClip _seFinish;       // とどめを刺した時の効果音
    [SerializeField] private float _dieExplosionScale = 1.5f; // 倒された時の爆発の大きさ(爆発のプレハブの大きさに対する倍率)
    [SerializeField] private AudioSource _audioBGM;     // ステージのBGM(登場時に止めて行動開始時にボス戦の曲へ差し替える。撃破演出中は止め、演出後に音量を戻す)
    [SerializeField] private AudioClip _bgmBattle;      // ボス戦の曲
    [SerializeField] private float _bgmFadeOutTime = 0.5f; // 登場時にステージのBGMを消すまでの時間
    [SerializeField] private PlayableDirector _battleStartDirector; // ボス戦開始のTimeline(再生が始まったらステージのBGMを消す。未設定なら初期化時に消す)

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

    private eAttackState _attackState = eAttackState.Interval;
    private float _attackTimer;
    private readonly List<bool> _barrage1Queue = new List<bool>(); // 弾幕1でこれから撃つ弾(trueで狙う弾、falseでかすめる弾。順不同)
    private int _barrageCountSinceStraight; // 最後に巨大な弾を真っすぐ撃ってから撃った弾幕の回数
    private ZelxFireBullet _lobBullet;      // 弾幕2で山なりに撃った巨大な弾
    private Coroutine _lobShotRoutine;          // 弾幕2で爆発地点から扇状に撃っている途中
    private ZelxFireBullet _chargingBullet; // チャージ中の巨大な弾
    private ZelxFireBullet _firedGiantBullet; // 真っすぐ撃った巨大な弾(消えるまで次の行動をしない)
    private float _hitStopTimer;   // 巨大な弾を跳ね返し返した時のヒットストップの残り時間(この間は動かない)
    private float _hitStopDelay;   // ヒットストップが始まるまでの残り時間
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

    // BGM
    private float _currentBGMVolume = 0.7f;
    private bool _startBGM = false;
    private Coroutine _bgmFadeRoutine;

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
        // 登場したらステージのBGMを消し、行動開始時にボス戦の曲を流す(Exと同じ流れ)
        // ボスはステージ開始時から置かれているため、ボス戦開始のTimelineが再生されたら消す
        _currentBGMVolume = _audioBGM != null ? _audioBGM.volume : 0.7f;
        if (_battleStartDirector != null) {
            _battleStartDirector.played += _OnBattleStartPlayed;
        } else {
            _StopStageBGM();
        }
    }

    private void _OnBattleStartPlayed(PlayableDirector director) {
        _StopStageBGM();
    }

    private void _StopStageBGM() {
        if (_audioBGM == null || _startBGM || _bgmFadeRoutine != null) return;
        _bgmFadeRoutine = StartCoroutine(_FadeOutStageBGM());
    }

    private IEnumerator _FadeOutStageBGM() {
        float timer = 0f;
        while (timer < _bgmFadeOutTime) {
            timer += Time.deltaTime;
            _audioBGM.volume = Mathf.Lerp(_currentBGMVolume, 0f, timer / _bgmFadeOutTime);
            yield return null;
        }
        _audioBGM.enabled = false;
        _bgmFadeRoutine = null;
    }

    private void _StartBattleBGM() {
        if (_startBGM || _audioBGM == null) return;
        _startBGM = true;
        if (_bgmFadeRoutine != null) {
            StopCoroutine(_bgmFadeRoutine);
            _bgmFadeRoutine = null;
        }
        _audioBGM.enabled = true;
        _audioBGM.volume = _currentBGMVolume;
        if (_bgmBattle != null) _audioBGM.clip = _bgmBattle;
        _audioBGM.Play();
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
        _StartBattleBGM();

        var cam = Camera.main;
        _entryOffsetX = cam != null ? transform.position.x - _GetEdgeX(cam) : 0f;
        _currentY = transform.position.y;
        _driftTimer = 0f;

        _ChangeAttackState(eAttackState.Interval, _firstAttackDelay);
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

    // HPが減るほど、跳ね返されてきた巨大な弾を何回か跳ね返し返す(跳ね返し返した時はヒットしない)
    // プレイヤーがそれを決まった回数だけ跳ね返し続けると、ヒットする
    public bool IsIgnoredDamageSource(DamageZone source) {
        if (_state != eState.Acting || _isDead) return false;
        var bullet = source.GetComponentInParent<ZelxFireBullet>();
        if (bullet == null || !bullet.IsReflected || !bullet.IsKnockdownOnReflectHit) return false;
        int max_count = _reflectBackCountsByPhase.Length > 0 ? _reflectBackCountsByPhase[Mathf.Min(_phase, _reflectBackCountsByPhase.Length - 1)] : 0;
        if (bullet.ReflectBackCount >= max_count) return false;
        bullet.ReflectBack(_FacingDirection, _reflectBackHitStopTime);
        _PlayReflectBackReaction(bullet.transform.position);
        return true;
    }

    // 跳ね返し返した時のヒットエフェクト・効果音・ヒットストップ
    private void _PlayReflectBackReaction(Vector3 position) {
        if (_reflectBackHitEffectPrefab != null) {
            var effect = EffectPool.Instance.Spawn(_reflectBackHitEffectPrefab.gameObject, position);
            if (effect != null) {
                foreach (var hit_effect in effect.GetComponentsInChildren<HitEffect>()) {
                    hit_effect.Setup(_reflectBackHitEffectType, _reflectBackHitEffectSize);
                }
                effect.GetComponent<LocalTimePause>()?.StartPause(_reflectBackHitStopTime);
            }
        }
        if (_seReflectBack != null) _audioSource?.PlayOneShot(_seReflectBack);
        _hitStopDelay = _reflectBackHitStopWait;
        _hitStopTimer = _reflectBackHitStopTime;
    }

    // 巨大な弾と打ち合っている間(ゼルクスが初めて跳ね返し返してから弾が消えるまで)
    private bool _IsExchangingGiantBullet() {
        return _IsExchanging(_firedGiantBullet) || _IsExchanging(_lobBullet);
    }

    private static bool _IsExchanging(ZelxFireBullet bullet) {
        return bullet != null && !bullet.IsReleased && bullet.ReflectBackCount > 0;
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
                    // 巨大な弾を跳ね返し返した時のヒットストップ: 少し間を置いてから、決まった時間だけその場で止まる
                    if (_UpdateHitStop(dt)) break;
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

    private void _ResetHitStop() {
        _hitStopDelay = 0f;
        _hitStopTimer = 0f;
        if (_anim != null) _anim.speed = 1f;
    }

    // ヒットストップ中ならtrue(アニメーションも止める)
    private bool _UpdateHitStop(float dt) {
        if (_hitStopDelay > 0f) {
            _hitStopDelay -= dt;
            return false;
        }
        if (_hitStopTimer <= 0f) return false;
        _hitStopTimer -= dt;
        bool is_stopping = _hitStopTimer > 0f;
        if (_anim != null) _anim.speed = is_stopping ? 0f : 1f;
        return is_stopping;
    }

    // 陣取る画面端を維持して漂う。移動後の位置を返す
    private Vector2 _UpdateMove(Camera cam, float dt) {
        _entryOffsetX = Mathf.MoveTowards(_entryOffsetX, 0f, _entrySpeed * dt);

        // 漂い始めの位置から途切れずに動き出すよう、どちらの軸もオフセット0から始まるサイン波で漂う
        // 巨大な弾と打ち合っている間は漂う動きを止め、その場で構える
        if (!_IsExchangingGiantBullet()) _driftTimer += dt;
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
            case eAttackState.Interval:
                if (_attackTimer > 0f) break;
                // 真っすぐ撃った巨大な弾が消えるまで(跳ね返し合っている間も)次の行動をしない
                if (_firedGiantBullet != null) {
                    if (!_firedGiantBullet.IsReleased) break;
                    _firedGiantBullet = null;
                }
                _StartNextAttack(_GetMuzzlePosition(position));
                break;

            case eAttackState.Barrage1:
                if (_attackTimer > 0f) break;
                if (_barrage1Queue.Count > 0) {
                    bool is_aim = _barrage1Queue[_barrage1Queue.Count - 1];
                    _barrage1Queue.RemoveAt(_barrage1Queue.Count - 1);
                    _FireBarrage1Bullet(_GetMuzzlePosition(position), is_aim);
                }
                if (_barrage1Queue.Count == 0) {
                    _ChangeAttackState(eAttackState.Interval, _attackInterval); // 最後に撃ってから一定時間待つ
                } else {
                    _attackTimer += _barrage1FireInterval;
                }
                break;

            case eAttackState.Barrage2:
                // 扇状に撃ち終えるまで待つ。爆発せずに消えた(跳ね返された等)場合もここで次へ進む
                if (_lobShotRoutine != null) break;
                if (_lobBullet != null && !_lobBullet.IsReleased) break;
                _lobBullet = null;
                _ChangeAttackState(eAttackState.Interval, _attackInterval); // 最後に撃ってから一定時間待つ
                break;

            case eAttackState.Charging: {
                    // WaterBulletで消された場合はチャージを中断する
                    if (_chargingBullet == null || _chargingBullet.IsReleased) {
                        _chargingBullet = null;
                        _StopChargeEffect();
                        _ChangeAttackState(eAttackState.Interval, _attackInterval);
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
                    _firedGiantBullet = _chargingBullet;
                    _chargingBullet = null;
                    _StopChargeEffect(); // 撃った瞬間のエフェクトと画面の揺れは、弾側(ZelxFireBullet)で出す
                    _ChangeAttackState(eAttackState.Interval, _attackInterval); // 最後に撃ってから一定時間待つ
                    break;
                }
        }
    }

    // 次の攻撃を選ぶ。弾幕を決まった回数撃ったら、巨大な弾をチャージして真っすぐ撃つ
    // 弾幕は、HPが一定以下になるまでは弾幕1だけ、それ以降は確率で弾幕2も撃つ
    private void _StartNextAttack(Vector3 muzzle_position) {
        if (_barrageCountSinceStraight >= _barragesBeforeStraight && _chargeBulletPrefab != null) {
            _barrageCountSinceStraight = 0;
            _StartCharge(muzzle_position);
            return;
        }
        _barrageCountSinceStraight++;

        bool is_hp_low = _charaParam.CurrentHP <= _charaParam.MaxHP * _lobAttackHpRate;
        if (is_hp_low && _chargeBulletPrefab != null && Random.value < _lobAttackChance) {
            _FireLobBarrage(muzzle_position);
            return;
        }
        _StartBarrage1();
    }

    // 弾幕1を始める。かすめる弾と狙う弾を混ぜ、撃つ順番をばらばらにする
    private void _StartBarrage1() {
        ZelxBarrage1.CreateQueue(_barrage1Queue, _missBulletCount, _aimBulletCount);
        _ChangeAttackState(eAttackState.Barrage1, 0f);
    }

    // 弾幕1の弾を1発撃つ。狙う弾はプレイヤーへ真っすぐ、かすめる弾は弧を描いてプレイヤーの脇を通る
    private void _FireBarrage1Bullet(Vector3 spawn_pos, bool is_aim) {
        if (_bulletPrefab == null) return;
        if (_seShoot != null) _audioSource?.PlayOneShot(_seShoot);

        var bullet = _GetBullet(_bulletPool, _bulletPrefab, spawn_pos);
        var settings = new ZelxBarrage1.Settings {
            speedRateRange = _bulletSpeedRateRange,
            missTurnRateRange = _missTurnRateRange,
            missDistanceRange = _missDistanceRange,
            lifetimeRange = _barrage1LifetimeRange,
        };
        ZelxBarrage1.Fire(bullet, spawn_pos, is_aim, _FacingDirection, settings, _returnBullet, this);
    }

    // 弾幕2: 巨大な弾をチャージせずに、画面の左上へ山なりに撃つ。そこで爆発し、扇状に高速の弾をまき散らす
    private void _FireLobBarrage(Vector3 spawn_pos) {
        var cam = Camera.main;
        if (cam == null) {
            _ChangeAttackState(eAttackState.Interval, _attackInterval);
            return;
        }
        if (_seChargeShoot != null) _audioSource?.PlayOneShot(_seChargeShoot);

        var bullet = _GetBullet(_chargeBulletPool, _chargeBulletPrefab, spawn_pos);
        bullet.transform.localScale = _chargeBulletScale;
        float viewport_x = _isOnRightSide ? _lobExplodeViewport.x : 1f - _lobExplodeViewport.x;
        Vector2 target = _ViewportToWorld(cam, viewport_x, _lobExplodeViewport.y);
        // 決まった時間で爆発地点に届く初速を求める(重力は下向き)
        float time = Mathf.Max(0.1f, _lobFlightTime);
        Vector2 velocity = (target - (Vector2)spawn_pos) / time + Vector2.up * (0.5f * _lobGravity * time);
        bullet.FireLob(velocity, _lobGravity, _returnChargeBullet, this, time, _OnLobExploded);
        _lobBullet = bullet;
        _ChangeAttackState(eAttackState.Barrage2, 0f);
    }

    // 山なりの巨大な弾が爆発したら、その位置から扇状に弾をまき散らす
    private void _OnLobExploded(Vector2 position) {
        if (this == null || _isDead) return;
        if (_lobShotRoutine != null) StopCoroutine(_lobShotRoutine);
        _lobShotRoutine = StartCoroutine(_FireFanCo(position));
    }

    // 真下から真右(左端に陣取る時は真左)までの扇状に並べた向きへ、順番をばらばらにして1発ずつ撃ち、ステージ全体にまき散らす
    // 弾はプレイヤーを狙わず真っすぐ高速に飛び(ホーミングしない)、着弾したら爆発して消える
    private IEnumerator _FireFanCo(Vector2 origin) {
        var order = new List<int>();
        for (int i = 0; i < _fanBulletCount; i++) order.Add(i);
        for (int i = order.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (int i in order) {
            if (_bulletPrefab == null) break;
            float t = _fanBulletCount > 1 ? (float)i / (_fanBulletCount - 1) : 0.5f;
            float angle = Mathf.Lerp(_fanAngleRange.x, _fanAngleRange.y, t) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (!_isOnRightSide) direction.x = -direction.x;
            var bullet = _GetBullet(_bulletPool, _bulletPrefab, origin);
            bullet.FireStraight(direction, _lobShotSpeedRate, _returnBullet, this);
            bullet.SetExplodeOnImpact();
            if (_seShoot != null) _audioSource?.PlayOneShot(_seShoot);
            yield return new WaitForSeconds(_fanFireInterval);
        }
        _lobShotRoutine = null;
    }

    private void _ChangeAttackState(eAttackState state, float timer) {
        _attackState = state;
        _attackTimer = timer;
    }

    // 吹き飛び開始。一瞬ズームとスローモーションを掛ける
    private void _StartKnockback() {
        _CancelCharge();
        _ResetHitStop();
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
        if (_battleStartDirector != null) _battleStartDirector.played -= _OnBattleStartPlayed;
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

    /// <summary>
    /// 撃破演出(炎・霧のゼルクスと同じ流れ)
    /// とどめ→カメラが寄ってスロー・フラッシュ・BGM停止→時間を戻す→フラッシュ2回→爆発して消える→撃破演出の終了を通知(ステージクリア表示)
    /// </summary>
    protected override IEnumerator Die() {
        // 撃破演出の途中で攻撃や吹き飛びの処理が残らないように止める
        _CancelCharge();
        _ResetHitStop();
        if (_lobShotRoutine != null) {
            StopCoroutine(_lobShotRoutine);
            _lobShotRoutine = null;
        }
        _EndSlowMotion();
        _SetWaterShieldVisible(false);
        foreach (var damage_zone in GetComponentsInChildren<DamageZone>(true)) {
            damage_zone.gameObject.SetActive(false);
        }
        // 画面に残っている弾は全て弾けて消す(撃破演出中にプレイヤーが被弾しないように)
        foreach (var bullet in FindObjectsByType<ZelxFireBullet>(FindObjectsSortMode.None)) {
            if (!bullet.IsReleased) bullet.BurstByBullet();
        }

        OnDied?.Invoke();

        // 倒された時の効果音(_seDead)は被弾処理側で鳴っているので、ここではとどめの効果音だけ鳴らす
        if (_seFinish != null) _audioSource?.PlayOneShot(_seFinish);

        var cinemachine_manager = CinemachineManager.Instance;
        var flash = ScreenFlash.Instance;

        // カメラズーム
        cinemachine_manager?.ZoomOnTarget(transform);
        yield return new WaitForSeconds(0.05f);

        // とどめの瞬間: スローとフラッシュ
        Time.timeScale = 0.1f;
        flash?.Flash();
        if (_anim != null && _anim.HasState(0, Animator.StringToHash("Die"))) _anim.Play("Die");
        yield return new WaitForSecondsRealtime(0.2f);

        // BGM停止
        if (_audioBGM != null) _audioBGM.volume = 0f;

        yield return new WaitForSecondsRealtime(4.0f);

        cinemachine_manager?.ReturnToPlayer();

        // 徐々に時間を戻す
        float time_scale = Time.timeScale;
        while (time_scale < 1f) {
            time_scale += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Min(time_scale, 1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // 爆発
        flash?.Flash();
        yield return new WaitForSecondsRealtime(0.2f);
        flash?.Flash();
        yield return new WaitForSecondsRealtime(0.5f);

        if (_dieExplosion != null) {
            var explosion = Instantiate(_dieExplosion, transform.position, Quaternion.identity);
            explosion.transform.localScale *= _dieExplosionScale;
        }
        cinemachine_manager?.ShakeCamera(duration: 0.5f);
        if (_sprite != null) _sprite.enabled = false;

        yield return new WaitForSeconds(1.0f);

        // 撃破演出の終了を通知する(StageEvent_BossBattleがステージクリア表示を出す)
        OnDieEnded?.Invoke();

        yield return new WaitForSeconds(5.0f);

        // BGMの音量を戻す
        if (_audioBGM != null) _audioBGM.volume = _currentBGMVolume;
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
