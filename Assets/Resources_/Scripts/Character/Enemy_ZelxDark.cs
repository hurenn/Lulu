using System.Collections;
using UnityEngine;

/// <summary>
/// ボス「闇のゼルクス」
/// 第一形態: その場から動かず、SylxDarkを一定間隔でランダムな数だけ生み出す(1回分は全員同じ動き)。闇の盾で通常攻撃を防ぐ
///          必殺技は当たるが、必殺技が終わるまでダメージを受けない。必殺技が終わった時点で盾が壊れ、HPが大きく減ってダウンする
/// ダウン: 真下に落ちて着地し、HPが半分になるまで削られるか一定時間経ったら第一形態でいた位置まで上がり、第二形態へ移る
/// 第二形態: SylxDarkを生み出す・体当たりのどちらかの行動を繰り返す
///          それとは別に、能力使用不可エリアを一定間隔で展開し直す(最初は1つ目のエリアに決まっている)
/// 第三形態: HPが1/3を切ると移る。第一形態でいた位置に戻り、画面全体を能力使用不可エリアで覆う
///          行動は、2点の間を移動する(追尾しない)SylxDarkを生み出すことだけになる
/// 第四形態: HPが一定値(50)を切ると、HPをその値に戻して移る。ダウンして地面に落ち、エリアを全て閉じてから、右の画面外へ逃げ出す
///          プレイヤーがワープで追いかけた後は、MP無限モードのプレイヤーから画面の右側へ逃げ続ける。氷の必殺技でしか倒れない
/// </summary>
public class Enemy_ZelxDark : Enemy_Base, IActivatable, IDamageSourceBlocker, IDamageSourceDeflector {
    public override bool IsBossSide => true; // ボス(氷の必殺技ゲージが多く溜まる)

    private enum ePhase {
        First,  // 第一形態(闇の盾で通常攻撃を防ぐ)
        Down,   // ダウン(落ちて着地し、HPが半分になったら上がる)
        Second, // 第二形態
        Third,  // 第三形態
        Fourth, // 第四形態(ダウンしてイベントの後、画面外へ逃げ続ける)
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
    [Tooltip("形態が変わるたびに画面を白く光らせる時間(秒)")]
    [SerializeField] private float _phaseChangeFlashTime = 0.4f;

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

    [Header("■ 第三形態(HPが一定の割合を切ったら移る。第一形態でいた位置に戻り、2点の間を移動するSylxDarkだけを生み出す)")]
    [Tooltip("第三形態へ移るHP(最大HPに対する割合)。これを切ったら移る")]
    [SerializeField] private float _thirdPhaseHPRate = 1f / 3f;
    [Tooltip("第三形態で展開する、画面全体を覆うエリア(第二形態のエリアは全て畳む)")]
    [SerializeField] private GameObject _finalSealArea;
    [Tooltip("第三形態のエリアを展開した時のScale(X・Y)")]
    [SerializeField] private Vector2 _finalSealAreaScale = new Vector2(10f, 10f);
    [Tooltip("第三形態のエリアを展開する時間(秒)")]
    [SerializeField] private float _finalSealAreaOpenTime = 0.5f;
    [Tooltip("第一形態でいた位置に戻る速さ")]
    [SerializeField] private float _thirdPhaseReturnSpeed = 8.0f;
    [Tooltip("第一形態でいた位置に戻る前に、上に浮かぶ高さ")]
    [SerializeField] private float _thirdPhaseFloatHeight = 2.0f;
    [Tooltip("上に浮かぶ時間(秒)")]
    [SerializeField] private float _thirdPhaseFloatTime = 0.5f;
    [Tooltip("戻ってから、カメラを揺らすまでの時間(秒)")]
    [SerializeField] private float _thirdPhaseShakeWait = 2.0f;
    [Tooltip("カメラを揺らしてから、エリアを展開するまでの時間(秒)")]
    [SerializeField] private float _thirdPhaseOpenWait = 1.0f;
    [Tooltip("第三形態でSylxDarkを生み出す間隔(秒)")]
    [SerializeField] private float _thirdGenerateInterval = 5.0f;
    [Tooltip("第三形態で1回に生み出すSylxDarkの数")]
    [SerializeField] private int _thirdGenerateCount = 2;

    private ePhase _phase = ePhase.First;
    private bool _isActive = false; // false中はTimelineの合図があるまで行動を止めておく
    private float _generateTimer = 0f;
    private bool _isSpecialHit = false; // 第一形態で必殺技を受けた(必殺技が終わったら盾を壊す)
    private bool _isHalfHPReached = false; // HPが半分まで削られた(OnHalfHPを呼んだ)
    private Coroutine _actionRoutine = null;
    private Coroutine _sealAreaRoutine = null;
    private Coroutine _thirdPhaseRoutine = null;
    private bool _isThirdPhaseStarting = false; // 第三形態へ移ってから、エリアの展開が始まるまで(ダメージを受けない)

    [Header("■ 第四形態(HPが一定値を切ったら移る。ダウンした後に画面の右の外へ逃げ出し、追いかけっこになる。氷の必殺技でしか倒れない)")]
    [Tooltip("第四形態へ移るHP。これを切ったら、HPをこの値に戻して移る")]
    [SerializeField] private int _fourthPhaseHP = 50;
    [Tooltip("着地して能力使用不可エリアを閉じてから、浮き上がるまでの時間(秒)")]
    [SerializeField] private float _escapeWait = 2.0f;
    [Tooltip("逃げる前に浮き上がる高さ")]
    [SerializeField] private float _escapeFloatHeight = 1.5f;
    [Tooltip("浮き上がる時間(秒)")]
    [SerializeField] private float _escapeFloatTime = 0.6f;
    [Tooltip("右の画面外へ飛んでいく速さの増え方")]
    [SerializeField] private float _escapeAcceleration = 60.0f;
    [Tooltip("右の画面外へ飛んでいく速さの最大値")]
    [SerializeField] private float _escapeMaxSpeed = 40.0f;
    [Tooltip("画面外へ飛んでいってから、プレイヤーがワープで追いかけるまでの時間(秒)")]
    [SerializeField] private float _chaseWarpWait = 1.0f;
    [Tooltip("プレイヤーがワープする先の、壁(または画面の右端)からさらに右への距離")]
    [SerializeField] private float _chaseWarpOverDistance = 3.0f;
    [Tooltip("画面外へ飛び出してから、もう1回右へワープするまでの時間(秒)")]
    [SerializeField] private float _chaseSecondWarpWait = 0.5f;
    [Tooltip("画面外へ飛び出した後の、もう1回の右へのワープの距離")]
    [SerializeField] private float _chaseSecondWarpDistance = 6.0f;
    [Tooltip("画面外へ飛び出した後に、続けて右へワープする回数")]
    [SerializeField] private int _chaseSecondWarpCount = 5;
    [Tooltip("続けて右へワープする間隔(秒)")]
    [SerializeField] private float _chaseSecondWarpInterval = 0.15f;
    [Tooltip("もう1回右へワープしてから、カメラがプレイヤーに切り替わって操作できるようになるまでの時間(秒)")]
    [SerializeField] private float _chaseCameraWait = 1.0f;
    [Tooltip("追いかけっこ中のカメラの視野角(Lens)の最大値(度)。これより離れると、ゼルクスは画面の外に出る")]
    [SerializeField] private float _chaseMaxFov = 65.0f;
    [Tooltip("追いかけっこ中にカメラに映す範囲の高さの最小値(近づいた時に寄りすぎないようにする)")]
    [SerializeField] private float _chaseMinViewHeight = 10.0f;
    [Tooltip("追いかけっこ中に、プレイヤーとゼルクスを映す位置の、画面の左右の端からの余白")]
    [SerializeField] private float _chaseScreenMargin = 3.0f;
    [Tooltip("追いかけっこ中に、カメラが寄る・離れる速さ")]
    [SerializeField] private float _chaseCameraSharpness = 3.0f;
    [Tooltip("追いかけっこが始まった距離から、自動で氷の必殺技を撃つ距離まで近づくのに必要な、右へのワープの回数(ワープの間隔に関わらず、この回数で追いつく)")]
    [SerializeField] private int _chaseWarpsToCatch = 30;
    [Tooltip("追いかけっこ中の、画面全体のグリッチノイズの強さ(0〜1)")]
    [SerializeField] private float _chaseGlitchIntensity = 1.0f;
    [Tooltip("追いかけっこが始まってから、グリッチノイズが最大の強さになるまでの時間(秒)")]
    [SerializeField] private float _chaseGlitchTime = 10.0f;
    [Tooltip("倒された時に、グリッチノイズを弱める時間(秒)")]
    [SerializeField] private float _chaseGlitchEndTime = 0.5f;
    [Tooltip("倒された後に残す、グリッチノイズの強さ(0〜1)")]
    [SerializeField] private float _chaseGlitchDefeatedIntensity = 0.2f;
    [Tooltip("追いかけっこ中に、SylxDarkを生み出す間隔(秒)")]
    [SerializeField] private float _chaseGenerateInterval = 1.0f;
    [Tooltip("追いかけっこ中に、1回に生み出すSylxDarkの数")]
    [SerializeField] private int _chaseGenerateCount = 2;
    [Tooltip("追いかけっこ中のSylxDarkが向かう高さの、プレイヤーの高さからの上下の範囲(この範囲でランダムに決める)")]
    [SerializeField] private float _chaseGenerateYOffset = 2.0f;
    [Tooltip("氷の必殺技が届く距離に対してこの割合まで近づいたら、自動で氷の必殺技を撃たせる")]
    [SerializeField] private float _autoSpecialRangeRate = 0.5f;
    [Tooltip("一番近づいた時の、プレイヤーからの距離")]
    [SerializeField] private float _chaseMinDistance = 4.0f;
    [Tooltip("プレイヤーが右以外へ動いた時に、画面の外へ逃げる速さ")]
    [SerializeField] private float _chaseHideSpeed = 20.0f;
    [Tooltip("プレイヤーが右へ動いているとみなす、右向きの速さ(落ちながら右へ進んでいる時も、右斜めとして数える)")]
    [SerializeField] private float _chaseMoveThreshold = 1.0f;
    [Tooltip("距離が変わった時に、位置を合わせる速さ")]
    [SerializeField] private float _chaseFollowSharpness = 6.0f;
    [Tooltip("上下に漂う幅")]
    [SerializeField] private float _chaseFloatWidth = 0.5f;
    [Tooltip("上下に漂う1回の時間(秒)")]
    [SerializeField] private float _chaseFloatTime = 2.0f;
    [Tooltip("左右に揺れる幅(近づいたり離れたりを繰り返しているように見せる。見た目だけで、追いつくまでの距離には関わらない)")]
    [SerializeField] private float _chaseSwayWidth = 1.5f;
    [Tooltip("左右に揺れる1回の時間(秒)")]
    [SerializeField] private float _chaseSwayTime = 2.5f;
    [Tooltip("飛び出した後の重力が無いプレイヤーの、上下の動きを止める速さ(ワープの勢いが残らないようにする)")]
    [SerializeField] private float _chasePlayerFloatDamping = 10.0f;

    private Coroutine _fourthPhaseRoutine = null;
    private bool _isChasing = false;          // 追いかけっこ中(第四形態のイベントの後)
    private bool _isIceSpecialHit = false;    // 第四形態で氷の必殺技を受けた(最後の一撃で倒れられるようにする)
    private Player_Character _chasePlayer;
    private Rigidbody2D _chasePlayerRb;
    private float _chaseDistance;             // 追いかけっこ中の、プレイヤーからの右への距離(目標)
    private float _chaseShownDistance;        // 実際の距離(目標へ少しずつ合わせる)
    private WarpControl _chaseWarpControl;    // プレイヤーのワープを知るため
    private float _chaseApproachPerWarp;      // 右へのワープ1回で縮まる距離(追いかけっこが始まった時に決める)
    private Ability_Ice _chaseIce;            // 近づいた時に自動で必殺技を撃たせる氷の能力
    private float _chaseGenerateTimer = 0f;   // 追いかけっこ中に、次にSylxDarkを生み出すまでの時間
    private int _playerWarpCount = 0;         // 前回の更新からの間に、プレイヤーがワープした回数
    private int _playerRightWarpCount = 0;    // そのうち、右へワープした回数
    private Transform _chaseFocus;            // 追いかけっこ中にカメラで映す中心
    private float _chaseViewHalfWidth;        // 追いかけっこ中にカメラに映す範囲の、横幅の半分
    private bool _isPlayerFalling = false;    // 飛び出した後のプレイヤーに、重力を戻した
    private Vector3 _finalSealAreaBaseScale = Vector3.one; // 第三形態のエリアの、展開する前のScale(倒された時に戻す)
    private Vector2 _homePosition; // 第一形態でいた位置(ダウンから復帰する時に戻り、第二形態の初期位置になる)
    private int _tackleIndex = -1;  // 今いる体当たりの突っ込む先(-1: 初期位置にいる)
    private int _sealAreaIndex = -1; // 展開しているエリア(-1: 展開していない)

    // 追いかけっこの途中で消えた時(シーンの切り替えなど)も、炎の自動攻撃を止めたままにしない
    private void OnDestroy() {
        if (_isChasing) {
            Ability_Fire.isAutoAttackDisabled = false;
            ScreenGlitch.FadeTo(0f, 0f);
        }
    }

    protected override void _Setup() {
        base._Setup();
        _generateTimer = _generateInterval;
        _isActive = _actOnStart;
        if (_darkShield != null) _darkShield.SetActive(true);
        // エリアは畳んだ状態から始める
        if (_sealAreas != null) {
            foreach (var area in _sealAreas) _SetAreaScaleX(area, 0f);
        }
        if (_finalSealArea != null) _finalSealAreaBaseScale = _finalSealArea.transform.localScale;
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

        // 氷の必殺技が終わったら、最後の一撃で倒れられる状態を終える(最後の一撃は、必殺技の終わりと同じフレームで当たる)
        if (_isIceSpecialHit && !Ability_Base.isAnySpecialUsing) _isIceSpecialHit = false;
        if (_isChasing) {
            _UpdateChase();
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
        if (_phase == ePhase.Fourth) return _DamageFourth(damage, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
        // 第三形態へ移ってから、エリアの展開が始まるまでは、当たったように見せるがダメージは受けない
        if (_isThirdPhaseStarting) return base.Damage(0, Vector2.zero, 0f, 0f, is_trap_damage, ignore_invincibility);
        if (_phase == ePhase.Down) {
            // ダウン中は、HPが半分より減らない(半分まで削られたら、時間切れを待たずに上がって第二形態へ移る)
            int half_hp = _GetHalfHP();
            damage = Mathf.Clamp(damage, 0, Mathf.Max(0, _charaParam.CurrentHP - half_hp));
            bool result = base.Damage(damage, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
            _CheckHalfHP();
            return result;
        }
        // 第四形態へ移る前に倒れないよう、HPは1より減らない
        damage = Mathf.Clamp(damage, 0, Mathf.Max(0, _charaParam.CurrentHP - 1));
        // 移動するボスのため、被弾時の吹き飛びは無効にする
        bool second_result = base.Damage(damage, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
        _CheckHalfHP();
        if (_isDead) return second_result;
        if (_charaParam.CurrentHP < _fourthPhaseHP) {
            // HPが一定値を切ったら、第四形態へ移る
            _StartFourthPhase();
        } else if (_phase == ePhase.Second && _charaParam.CurrentHP < _charaParam.MaxHP * _thirdPhaseHPRate) {
            // 第二形態でHPが一定の割合を切ったら、第三形態へ移る
            _StartThirdPhase();
        }
        return second_result;
    }

    // 第四形態のダメージ: 追いかけっこが始まるまでは当たるがダメージは受けない。追いかけっこ中は、氷の必殺技でしか倒れない(それ以外ではHPが1より減らない)
    private bool _DamageFourth(int damage, float invincible_time, float damage_reaction_time, bool is_trap_damage, bool ignore_invincibility) {
        // 追いかけっこが始まるまでは、当たったように見せるが、ダメージは受けない
        if (!_isChasing) return base.Damage(0, Vector2.zero, 0f, 0f, is_trap_damage, ignore_invincibility);
        bool is_ice_special = Ability_Base.isAnySpecialUsing && Ability_Base.specialUsingAbility is Ability_Ice;
        if (is_ice_special) _isIceSpecialHit = true;
        // 氷の必殺技の最後の一撃は、必殺技が終わった直後に当たる
        bool can_kill = is_ice_special || (_isIceSpecialHit && !Ability_Base.isAnySpecialUsing);
        if (!can_kill) damage = Mathf.Clamp(damage, 0, Mathf.Max(0, _charaParam.CurrentHP - 1));
        return base.Damage(damage, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
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
        _FlashPhaseChange();
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
        yield return _FallToGround();
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
        _FlashPhaseChange();
        _sealAreaRoutine = StartCoroutine(_SealAreaRoutine());
        _actionRoutine = StartCoroutine(_SecondPhaseRoutine());
    }

    // 真下の地面まで、だんだん速くなりながら落ちる
    private IEnumerator _FallToGround() {
        float land_y = _GetGroundY(transform.position) ?? transform.position.y;
        float speed = 0f;
        while (transform.position.y > land_y) {
            yield return new WaitForFixedUpdate();
            speed = Mathf.Min(speed + _fallGravity * Time.deltaTime, _fallMaxSpeed);
            _MoveTo(new Vector2(transform.position.x, Mathf.Max(land_y, transform.position.y - speed * Time.deltaTime)));
        }
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

    // 形態が変わった瞬間: 画面を白く光らせる
    private void _FlashPhaseChange() {
        ScreenFlash.Instance?.Flash(_phaseChangeFlashTime, Color.white);
    }

    // 画面の端や外で起きた衝撃:カメラを揺らし、離れていても小さくならないよう、カメラの位置で音を鳴らす
    private void _PlayImpactAtCamera() {
        CinemachineManager.Instance?.ShakeCamera(_impactShakeIntensity, _impactShakeDuration);
        var cam = Camera.main;
        if (_seExplosion != null && cam != null) AudioUtil.PlayClipAtPointUnclamped(_seExplosion, cam.transform.position, 1f);
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

    /// <summary>
    /// 第二形態のエリアを全て畳み、第三形態のエリアをfinal_scaleの大きさにする
    /// </summary>
    private IEnumerator _ChangeAllSealAreas(Vector3 final_scale, float duration, bool is_unscaled) {
        _sealAreaIndex = -1;
        var areas = new System.Collections.Generic.List<GameObject>();
        var targets = new System.Collections.Generic.List<Vector3>();
        if (_sealAreas != null) {
            foreach (var area in _sealAreas) {
                if (area == null) continue;
                var scale = area.transform.localScale;
                scale.x = 0f;
                areas.Add(area);
                targets.Add(scale);
            }
        }
        if (_finalSealArea != null) {
            areas.Add(_finalSealArea);
            targets.Add(final_scale);
        }
        var starts = new Vector3[areas.Count];
        for (int i = 0; i < areas.Count; i++) starts[i] = areas[i].transform.localScale;

        float time = 0f;
        while (time < duration) {
            time += is_unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float rate = Mathf.Clamp01(time / Mathf.Max(duration, 0.001f));
            for (int i = 0; i < areas.Count; i++) {
                if (areas[i] != null) areas[i].transform.localScale = Vector3.Lerp(starts[i], targets[i], rate);
            }
            yield return null;
        }
        for (int i = 0; i < areas.Count; i++) {
            if (areas[i] != null) areas[i].transform.localScale = targets[i];
        }
    }

    private static void _SetAreaScaleX(GameObject area, float x) {
        if (area == null) return;
        var scale = area.transform.localScale;
        scale.x = x;
        area.transform.localScale = scale;
    }

    #endregion

    #region 第三形態

    // 第二形態の行動(体当たりの途中でも)とエリアの展開を止めて、第三形態へ移る
    private void _StartThirdPhase() {
        _phase = ePhase.Third;
        _FlashPhaseChange();
        _isThirdPhaseStarting = true;
        if (_actionRoutine != null) {
            StopCoroutine(_actionRoutine);
            _actionRoutine = null;
        }
        if (_sealAreaRoutine != null) {
            StopCoroutine(_sealAreaRoutine);
            _sealAreaRoutine = null;
        }
        _tackleIndex = -1;
        _thirdPhaseRoutine = StartCoroutine(_ThirdPhaseRoutine());
    }

    // 第一形態でいた位置に戻り、少し待ってカメラを揺らし、さらに少し待ってから画面全体をエリアで覆う
    // その後は、2点の間を移動するSylxDarkを一定間隔で生み出し続ける
    private IEnumerator _ThirdPhaseRoutine() {
        // 上に浮かんでから、第一形態でいた位置へ向かう
        Vector2 float_from = transform.position;
        Vector2 float_to = float_from + Vector2.up * _thirdPhaseFloatHeight;
        float float_time = 0f;
        while (float_time < _thirdPhaseFloatTime) {
            yield return new WaitForFixedUpdate();
            float_time += Time.deltaTime;
            _MoveTo(Vector2.Lerp(float_from, float_to, Mathf.SmoothStep(0f, 1f, float_time / Mathf.Max(_thirdPhaseFloatTime, 0.001f))));
        }
        yield return _MoveLinear(_homePosition, _thirdPhaseReturnSpeed, false);
        yield return new WaitForSeconds(_thirdPhaseShakeWait);
        CinemachineManager.Instance?.ShakeCamera(_impactShakeIntensity, _impactShakeDuration);
        yield return new WaitForSeconds(_thirdPhaseOpenWait);

        // エリアの展開が始まったら、ダメージを受けるようになる
        _isThirdPhaseStarting = false;
        _PlayImpact();
        var final_scale = new Vector3(_finalSealAreaScale.x, _finalSealAreaScale.y, _finalSealAreaBaseScale.z);
        yield return _ChangeAllSealAreas(final_scale, _finalSealAreaOpenTime, false);

        bool can_path = _sylxDarkPrefab != null && _pathPoints != null && System.Array.Exists(_pathPoints, p => p != null && p.PointA != null && p.PointB != null);
        while (!_isDead) {
            yield return new WaitForSeconds(_thirdGenerateInterval);
            if (can_path) _GeneratePath(Mathf.Max(1, _thirdGenerateCount));
        }
    }

    #endregion

    #region 第四形態

    // それまでの行動を全て止めて、HPを戻し、第四形態へ移る
    private void _StartFourthPhase() {
        _phase = ePhase.Fourth;
        _FlashPhaseChange();
        _isThirdPhaseStarting = false;
        foreach (var routine in new[] { _actionRoutine, _sealAreaRoutine, _thirdPhaseRoutine }) {
            if (routine != null) StopCoroutine(routine);
        }
        _actionRoutine = null;
        _sealAreaRoutine = null;
        _thirdPhaseRoutine = null;
        _tackleIndex = -1;

        int lack = _fourthPhaseHP - _charaParam.CurrentHP;
        if (lack > 0) _charaParam.RecoverHP(lack);

        // 出ているSylxDarkを片付ける
        foreach (var sylx in FindObjectsByType<SylxDark>(FindObjectsSortMode.None)) sylx.Vanish();

        _fourthPhaseRoutine = StartCoroutine(_FourthPhaseRoutine());
    }

    /// <summary>
    /// 第四形態のイベント: 真下に落ちて着地し、能力使用不可エリアを閉じる
    /// 少し待ってから浮き上がり、右の画面外へ飛んでいく(壁を突き抜けた瞬間にカメラを揺らす)
    /// 少し後にプレイヤーが画面外へワープして追いかけ、追いかけっこが始まる
    /// </summary>
    private IEnumerator _FourthPhaseRoutine() {
        // イベントの間はプレイヤーを操作できなくする。MP無限モードにし、氷の必殺技をすぐ使えるようにする
        _chasePlayer = PlayerCharacterManager.Current as Player_Character;
        if (_chasePlayer != null) {
            _chasePlayerRb = _chasePlayer.GetComponent<Rigidbody2D>();
            _chasePlayer.PlayerCharaParam?.SetInfiniteMP(true);
        }
        foreach (var ice in FindObjectsByType<Ability_Ice>(FindObjectsSortMode.None)) ice.ForceCharge();
        var controller = PlayerCharacterManager.Controller;
        if (controller != null) controller.isEnabledCharacterInput = false;

        yield return _FallToGround();
        _PlayImpact();
        yield return _ChangeAllSealAreas(_finalSealAreaBaseScale, _sealAreaOpenTime, false);

        // 少し待ってから浮き上がる
        yield return new WaitForSeconds(_escapeWait);
        Vector2 start = transform.position;
        Vector2 float_to = start + Vector2.up * _escapeFloatHeight;
        float time = 0f;
        while (time < _escapeFloatTime) {
            yield return new WaitForFixedUpdate();
            time += Time.deltaTime;
            _MoveTo(Vector2.Lerp(start, float_to, Mathf.SmoothStep(0f, 1f, time / Mathf.Max(_escapeFloatTime, 0.001f))));
        }

        // 右の画面外へ飛んでいく(だんだん速くなる)。壁を突き抜けた瞬間にカメラを揺らす
        float width = _col != null ? _col.bounds.size.x : 2f;
        var wall = Physics2D.Raycast(transform.position, Vector2.right, 200f, _wallLayer);
        float? wall_x = wall.collider != null ? wall.point.x : (float?)null;
        float end_x = Mathf.Max(wall_x ?? transform.position.x, _GetCameraRightX()) + width * 2f;
        float speed = 0f;
        while (transform.position.x < end_x) {
            yield return new WaitForFixedUpdate();
            speed = Mathf.Min(speed + _escapeAcceleration * Time.deltaTime, _escapeMaxSpeed);
            float before_x = transform.position.x;
            _MoveTo(new Vector2(transform.position.x + speed * Time.deltaTime, transform.position.y));
            float front_x = transform.position.x + width * 0.5f;
            if (wall_x.HasValue && front_x >= wall_x.Value && before_x + width * 0.5f < wall_x.Value) {
                _PlayImpactAtCamera();
            }
        }

        // 少し後に、プレイヤーが右の画面外へ1回でワープして追いかける(同時にカメラを揺らす)
        yield return new WaitForSeconds(_chaseWarpWait);
        if (_chasePlayer != null) {
            // 空中を追いかけるため重力を無くし、ワープの慣性移動の時間制限と減衰を無くす(追いかけっこが終わるまで)
            _chasePlayer.enableGravity = false;
            _chasePlayer.isWarpDashUnlimited = true;
            Vector2 player_position = _chasePlayer.transform.position;
            float player_width = _chasePlayer.GetCharacterSize().x;
            float warp_x = Mathf.Max((wall_x ?? player_position.x) + player_width, _GetCameraRightX()) + _chaseWarpOverDistance;
            _chasePlayer.EventWarp(new Vector2(warp_x, player_position.y), true);
        }
        _PlayImpactAtCamera();

        // 少し後に、もう1回右へワープする
        yield return new WaitForSeconds(_chaseSecondWarpWait);
        if (_chasePlayer != null) {
            Vector2 from = _chasePlayer.transform.position;
            _chasePlayer.EventWarp(from + Vector2.right * _chaseSecondWarpDistance, true);
            for (int i = 1; i < _chaseSecondWarpCount; i++) {
                yield return new WaitForSeconds(_chaseSecondWarpInterval);
                if (_chasePlayer == null) break;
                from = _chasePlayer.transform.position;
                _chasePlayer.EventWarp(from + Vector2.right * _chaseSecondWarpDistance, true);
            }
        }

        // 少し待ってから、ワープの慣性で動き続けるプレイヤーにカメラを切り替え、操作できるようにする
        yield return new WaitForSeconds(_chaseCameraWait);
        _StartChase();
        if (controller != null) controller.isEnabledCharacterInput = true;
    }

    // 今カメラに映っている範囲の右端(Z=0の平面上)
    private float _GetCameraRightX() {
        var cam = Camera.main;
        if (cam == null) return transform.position.x;
        float depth = -cam.transform.position.z;
        return cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x;
    }

    // 追いかけっこを始める: カメラの範囲を一番大きく広げてプレイヤーを左端に映し、自分は画面の右の外から右端へ漂ってくる
    private void _StartChase() {
        if (_chasePlayer == null) return;
        _isChasing = true;
        Ability_Fire.isAutoAttackDisabled = true; // 追いかけっこ中は炎の自動攻撃を出さない
        ScreenGlitch.FadeTo(_chaseGlitchIntensity, _chaseGlitchTime); // 画面全体のグリッチノイズを少しずつ強める
        _isPlayerFalling = false;
        if (_chaseFocus == null) _chaseFocus = new GameObject("ZelxDarkChaseCameraFocus").transform;

        Vector2 player_position = _chasePlayer.transform.position;
        _chaseDistance = Mathf.Max(_chaseMinDistance, _GetChaseMaxHalfWidth() * 2f - _chaseScreenMargin * 2f);
        _chaseShownDistance = _GetChaseMaxDistance();
        _chaseViewHalfWidth = _GetChaseMaxHalfWidth();
        // 右へのワープ1回で縮まる距離(始まった距離から、自動で氷の必殺技を撃つ距離まで、決まった回数で届くようにする)
        _chaseApproachPerWarp = Mathf.Max(0.01f, (_chaseDistance - _GetAutoSpecialDistance()) / Mathf.Max(1, _chaseWarpsToCatch));
        _chaseGenerateTimer = _chaseGenerateInterval;
        _playerWarpCount = 0;
        _playerRightWarpCount = 0;
        _chaseWarpControl = _chasePlayer.GetComponentInChildren<WarpControl>(true);
        if (_chaseWarpControl != null) _chaseWarpControl.onWarped += _OnPlayerWarped;
        _UpdateChaseCamera(player_position, true);
    }

    // プレイヤーがワープした瞬間に数える(右へのワープは、距離を縮める)
    private void _OnPlayerWarped(Vector2 from, Vector2 to) {
        _playerWarpCount++;
        if (to.x - from.x > 0.1f) _playerRightWarpCount++;
        // プレイヤーと一緒に瞬間移動してブレないよう、今いる位置から少しずつ新しい距離へ合わせる
        _chaseShownDistance -= to.x - from.x;
    }

    /// <summary>
    /// 追いかけっこ中の動き
    /// ・プレイヤーが右(右斜め)へ動いている間は、距離を保つ
    /// ・プレイヤーが右へワープすると、少しだけ距離が縮む
    /// ・それ以外の時は、すぐに画面の右の外へ隠れる(離れるのは、カメラを一番広げた時の画面の右端から、自分1体分の所まで)
    /// ・飛び出した後のプレイヤーは重力が無いが、ワープするか左へ動いたら、重力を戻して落ちるようにする
    /// </summary>
    private void _UpdateChase() {
        if (_chasePlayer == null) return;
        float delta = Time.deltaTime;
        Vector2 player_position = _chasePlayer.transform.position;
        Vector2 player_velocity = _chasePlayerRb != null ? _chasePlayerRb.linearVelocity : Vector2.zero;
        bool is_warping = _chasePlayer._motorStates.isWarpDashing;
        // 前回からの間にワープしたか(ワープの慣性が続いている間に、続けてワープした時も数える)
        int warp_count = _playerWarpCount;
        int right_warp_count = _playerRightWarpCount;
        _playerWarpCount = 0;
        _playerRightWarpCount = 0;

        // 飛び出した後、ワープするか左へ動いたら、重力を戻して落ちるようにする
        if (!_isPlayerFalling && (warp_count > 0 || !_chasePlayer.isRight || player_velocity.x <= -_chaseMoveThreshold)) {
            _isPlayerFalling = true;
            _chasePlayer.enableGravity = true;
        }
        // 重力が無い間は、ワープの勢い以外では上下に流れていかないようにする
        if (!_isPlayerFalling && _chasePlayerRb != null && !is_warping) {
            player_velocity.y = Mathf.MoveTowards(player_velocity.y, 0f, _chasePlayerFloatDamping * delta);
            _chasePlayerRb.linearVelocity = player_velocity;
        }

        float max_distance = _GetChaseMaxDistance();
        // 右・右斜め(落ちながら右へ進んでいる時も含む)へ動いているか
        bool is_moving_right = player_velocity.x >= _chaseMoveThreshold;
        if (right_warp_count > 0) {
            _chaseDistance = Mathf.Max(_chaseMinDistance, _chaseDistance - _chaseApproachPerWarp * right_warp_count);
        } else if (!is_moving_right && !_chasePlayer.isWarpDelay) {
            // ワープする直前の一瞬止まっている間は、止まったとみなさない
            _chaseDistance = Mathf.MoveTowards(_chaseDistance, max_distance, _chaseHideSpeed * delta);
        }
        _chaseDistance = Mathf.Min(_chaseDistance, max_distance);
        _chaseShownDistance = Mathf.Lerp(_chaseShownDistance, _chaseDistance, 1f - Mathf.Exp(-_chaseFollowSharpness * delta));

        float float_y = Mathf.Sin(Time.time / Mathf.Max(_chaseFloatTime, 0.01f) * Mathf.PI * 2f) * _chaseFloatWidth;
        float y = Mathf.Lerp(transform.position.y, player_position.y + float_y, 1f - Mathf.Exp(-_chaseFollowSharpness * delta));
        // 左右にも揺れて、近づいたり離れたりを繰り返しているように見せる
        float sway_x = Mathf.Sin(Time.time / Mathf.Max(_chaseSwayTime, 0.01f) * Mathf.PI * 2f) * _chaseSwayWidth;
        _MoveTo(new Vector2(player_position.x + _chaseShownDistance + sway_x, y));
        _UpdateChaseCamera(player_position, false);

        // 一定間隔で、プレイヤーに付いて動くSylxDarkを生み出す(向かう高さは、プレイヤーの高さ±指定の範囲でランダム)
        _chaseGenerateTimer -= delta;
        if (_chaseGenerateTimer <= 0f) {
            _chaseGenerateTimer = _chaseGenerateInterval;
            if (_sylxDarkPrefab != null) {
                for (int i = 0; i < _chaseGenerateCount; i++) {
                    var sylx = Instantiate(_sylxDarkPrefab, _GetGeneratePosition(i), Quaternion.identity);
                    sylx.SetupEscort(Random.Range(-_chaseGenerateYOffset, _chaseGenerateYOffset));
                }
            }
        }

        // 氷の必殺技が届く距離の一定割合まで近づいたら、自動で氷の必殺技を撃たせる
        if (_chaseIce == null) _chaseIce = FindAnyObjectByType<Ability_Ice>();
        // (左右の揺れや、ワープ直後に一瞬近づいて見える分は除いて、追いかけっこの距離そのもので判定する)
        float distance = _chaseDistance;
        // (ワープの直前に止まっている間は、撃った後にワープしてしまうため撃たない)
        if (_chaseIce != null && _chaseIce.CanUseSpecial && !_chasePlayer.isWarpDelay && distance > 0f && distance <= _GetAutoSpecialDistance() + 0.01f) {
            _chasePlayer.ForceIceSpecial(true);
        }
    }

    /// <summary>
    /// 追いかけっこ中のカメラ: プレイヤーを画面の左端、自分を右端に映すように、寄ったり離れたりする
    /// (視野角の最大値より離れた時は、プレイヤーを左端に映したまま、自分は画面の外に出る)
    /// </summary>
    private void _UpdateChaseCamera(Vector2 player_position, bool is_immediate) {
        if (_chaseFocus == null) return;
        float aspect = _GetCameraAspect();
        float min_half_width = _chaseMinViewHeight * 0.5f * aspect;
        float target_half_width = Mathf.Clamp((_chaseShownDistance + _chaseScreenMargin * 2f) * 0.5f, min_half_width, _GetChaseMaxHalfWidth());
        _chaseViewHalfWidth = is_immediate ? target_half_width
            : Mathf.Lerp(_chaseViewHalfWidth, target_half_width, 1f - Mathf.Exp(-_chaseCameraSharpness * Time.deltaTime));
        _chaseFocus.position = new Vector3(player_position.x - _chaseScreenMargin + _chaseViewHalfWidth, player_position.y, 0f);
        CinemachineManager.Instance?.ZoomOnTarget(_chaseFocus, _chaseViewHalfWidth * 2f / aspect);
    }

    // 自動で氷の必殺技を撃つ距離
    private float _GetAutoSpecialDistance() {
        if (_chaseIce == null) _chaseIce = FindAnyObjectByType<Ability_Ice>();
        return _chaseIce != null ? _chaseIce.SpecialRange * _autoSpecialRangeRate : _chaseMinDistance;
    }

    private static float _GetCameraAspect() {
        var cam = Camera.main;
        return cam != null ? cam.aspect : 16f / 9f;
    }

    // カメラを一番広げた時(視野角の最大値)に映る範囲の、横幅の半分
    private float _GetChaseMaxHalfWidth() {
        float height = CinemachineManager.Instance != null ? CinemachineManager.Instance.FovToViewHeight(_chaseMaxFov) : 30f;
        return height * 0.5f * _GetCameraAspect();
    }

    // プレイヤーから一番離れる距離(カメラを一番広げた時の画面の右端から、自分1体分外側)
    private float _GetChaseMaxDistance() {
        float width = _col != null ? _col.bounds.size.x : 2f;
        return _GetChaseMaxHalfWidth() * 2f - _chaseScreenMargin + width;
    }

    // 倒された時などに、追いかけっこのためにプレイヤーへ変えたものを戻す
    private void _EndChase() {
        _isChasing = false;
        Ability_Fire.isAutoAttackDisabled = false;
        ScreenGlitch.FadeTo(_chaseGlitchDefeatedIntensity, _chaseGlitchEndTime);
        if (_chaseWarpControl != null) {
            _chaseWarpControl.onWarped -= _OnPlayerWarped;
            _chaseWarpControl = null;
        }
        if (_chaseFocus != null) {
            Destroy(_chaseFocus.gameObject);
            _chaseFocus = null;
        }
        if (_chasePlayer != null) {
            _chasePlayer.enableGravity = true;
            _chasePlayer.isWarpDashUnlimited = false;
            _chasePlayer.PlayerCharaParam?.SetInfiniteMP(false);
        }
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
        if (_isDead || _phase == ePhase.Fourth) yield break;
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
        if (_thirdPhaseRoutine != null) {
            StopCoroutine(_thirdPhaseRoutine);
            _thirdPhaseRoutine = null;
        }
        if (_fourthPhaseRoutine != null) {
            StopCoroutine(_fourthPhaseRoutine);
            _fourthPhaseRoutine = null;
        }
        _EndChase();
        if (_darkShield != null) _darkShield.SetActive(false);
        // 展開していたエリアを全て畳む(倒された演出中は時間が遅くなるため、実時間で畳む)
        StartCoroutine(_ChangeAllSealAreas(_finalSealAreaBaseScale, _sealAreaOpenTime, true));

        OnDied?.Invoke();

        if (_seFinish != null) {
            if (_seDead != null) _audioSource?.PlayOneShot(_seDead);
            _audioSource?.PlayOneShot(_seFinish);
        }

        var cinemachineManager = CinemachineManager.Instance;
        var flash = ScreenFlash.Instance;

        // === 2. カメラズーム ===
        cinemachineManager?.ZoomOnTarget(transform);
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

        cinemachineManager?.ReturnToPlayer();

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
            cinemachineManager?.ShakeCamera(duration: 0.5f);
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
