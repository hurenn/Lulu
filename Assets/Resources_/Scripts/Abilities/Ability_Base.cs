using UnityEngine;
using UnityEngine.Playables;

public enum eAbilityResult {
    None,
    IceSlash1,
    IceSlash2,
    IceSlash3,
    IceSeparate,
    IceLockonSlash,
    IceSpecial,
    FireShot,
    FireSpecial,
    LightParry,
    LightDome,
    LightSpecial,
    SpecialEnd,
}

public class Ability_Base : MonoBehaviour {

    /// <summary>
    /// 地上にいるか確認
    /// </summary>
    public bool _isGround = true;

    /// <summary>
    /// 必殺技チャージタイム
    /// </summary>
    [SerializeField]
    private float _specialChargeTime = 60.0f;
    private float _currentSpecialChargeTime = 0.0f;
    /// <summary>
    /// 必殺技チャージ完了確認
    /// </summary>
    protected bool _isSpecialCharged => _currentSpecialChargeTime >= _specialChargeTime;

    /// <summary>
    /// 必殺技演出タイムライン
    /// </summary>
    protected PlayableDirector _specialTimelineDirector = null;
    [SerializeField] private PlayableDirector _specialTimelinePrefab = null;
    protected bool _isSpecialUsing = false;

    /// <summary>
    /// 必殺技チャージ停止時間
    /// </summary>
    private float _specialChargeStopTime = 1.0f;
    private float _currentSpecialChargeStopTime = 0f;

    // 必殺技終了コールバック
    protected System.Action _onEndSpecialAttack = null;
    public void SetOnEndSpecialAttack(System.Action callback) {
        _onEndSpecialAttack = callback;
        _onEndSpecialAttack += () => {
            _isSpecialUsing = false;
        };
    }

    // 必殺技アニメーションコールバック
    protected System.Action<string> _onStartSpecialAnim = null;
    public void SetOnStartSpecialAnim(System.Action<string> onAnim) {
        _onStartSpecialAnim = onAnim;
    }

    // 必殺技チャージコールバック
    protected System.Action<float> _onChargeSpecial = null;
    public void SetOnChargeSpecialCallback(System.Action<float> callback) {
        _onChargeSpecial = callback;
    }

    [SerializeField]
    private float _returnTime = 1.0f;
    /// <summary>
    /// 帰還までの時間計測
    /// </summary>
    private float _currentReturnTime = 0f;
    /// <summary>
    /// まだ出現中
    /// </summary>
    protected bool _isAppearing { get { return _currentReturnTime > 0f; } }
    /// <summary>
    /// 帰還タイマーセット
    /// </summary>
    protected void _ResetReturnTimer() {
        _currentReturnTime = _returnTime;
    }
    protected void _ResetReturnTimer(float time) {
        _currentReturnTime = time;
    }
    // 強制帰還距離
    private float _forceReturnDistance = 5.0f;
    protected bool _canForceReturn = true;

    // オーバーヒートによるキャンセル
    protected bool _cancelByOverheat => _charaParam == null || (_charaParam.isOverheat && !_isAppearing);

    // キャラクター情報
    protected Transform _playerTransform = default;
    protected CharacterParameter_Player _charaParam = null;
    protected CommonParameter _param = null;
    protected WarpControl _warpControl = null;
    protected MotorStates _motorStates = null;

    /// <summary>
    /// 装備時のローカルポジション
    /// </summary>
    [SerializeField] protected Vector3 _localPosition = Vector3.zero;
    /// <summary>
    /// キャラクター表示
    /// </summary>
    [SerializeField] protected SpriteRenderer _rend = null;
    [SerializeField] protected Animator _anim = null;

    // ワープエフェクト
    [SerializeField] protected GameObject _warpAnimationPrefab = null;

    // ポーズUI参照キャッシュ
    private Pause_UI _pauseUIInstance;

    protected void Awake() {
        if (_specialTimelinePrefab) {
            var obj = Instantiate(_specialTimelinePrefab.gameObject);
            _specialTimelineDirector = obj.GetComponent<PlayableDirector>();
            _specialTimelineDirector.stopped += _OnSpecialCutInFinished;
            
            // TimelineがTime.timeScaleの影響を受けないように設定
            _specialTimelineDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        }
    }

    /// <summary>
    /// 右向きか確認
    /// </summary>
    protected bool _isRight = true;
    public virtual void UpdateParameter(
        bool is_right, 
        Transform chara_pos, 
        CommonParameter common_param, 
        CharacterParameter_Player chara_param,
        WarpControl warp_control,
        MotorStates motor_states) {
        _isRight = is_right;
        _playerTransform = chara_pos;
        _param = common_param;
        _charaParam = chara_param;
        _warpControl = warp_control;
        _motorStates = motor_states;
    }

    private bool _wasPauseOpen = false;
    private bool _isSpecialCutInViewing = false;

    private void Update() {
        // ポーズ画面の開閉状態を監視し、Timelineを制御
        bool isPauseOpen = Pause_UI.IsOpen;
        if (_specialTimelineDirector != null)
        {
            // カットイン演出中のみ再開処理を許可
            if (isPauseOpen && !_wasPauseOpen)
            {
                _specialTimelineDirector.Pause();
            }
            else if (!isPauseOpen && _wasPauseOpen && _isSpecialCutInViewing)
            {
                _specialTimelineDirector.Play();
            }
        }
        _wasPauseOpen = isPauseOpen;

        // 帰還タイマー
        if (_isAppearing) {
            _currentReturnTime -= Time.deltaTime;
            // 強制帰還距離チェック
            if (_canForceReturn &&
                Vector3.Distance(transform.position, _playerTransform.position) > _forceReturnDistance) {
                _ForceReturn();
            }
            if (_currentReturnTime <= 0f) {
                // 帰還
                _anim.Play("ToHide");
            }
        }

        if (_currentSpecialChargeStopTime > 0f) {
            // チャージ停止中
            _currentSpecialChargeStopTime -= Time.deltaTime;
        } else {
            // 必殺技チャージ
            if (_currentSpecialChargeTime < _specialChargeTime) {
                _currentSpecialChargeTime += Time.deltaTime;
                if (_currentSpecialChargeTime > _specialChargeTime) {
                    _currentSpecialChargeTime = _specialChargeTime;
                }
            }
        }
        // 現在のチャージ量を通知
        if (_onChargeSpecial != null) {
            _onChargeSpecial(_currentSpecialChargeTime / _specialChargeTime);
        }

        _Update();
    }

    /// <summary>
    /// 強制帰還
    /// </summary>
    protected virtual void _ForceReturn() {
        _currentReturnTime = 0f;
    }

    protected virtual void _Update() { }

    /// <summary>
    /// 仲間キャラクターの位置を更新
    /// </summary>
    public virtual void UpdatePartnerTransform(Vector3? target_pos = null) {
        if(_playerTransform == null) {
            return;
        }

        if (target_pos != null) {
            transform.position = target_pos.Value;
        } else {
            transform.position = _playerTransform.position + new Vector3(
                _localPosition.x * (_isRight ? -1 : 1),
                _localPosition.y,
                _localPosition.z);
        }
        _rend.flipX = _isRight;
    }

    /// <summary>
    /// 単押し使用
    /// </summary>
    public virtual eAbilityResult ExecuteSimple() { return eAbilityResult.None; }

    /// <summary>
    /// 長押し使用
    /// </summary>
    public virtual eAbilityResult ExecuteLong() { return eAbilityResult.None; }

    /// <summary>
    /// ボタンを離したときの処理
    /// </summary>
    public virtual void ExecuteRelease() { }

    /// <summary>
    /// ワープ実行時の処理
    /// </summary>
    public virtual void OnWarp() { }

    /// <summary>
    /// 必殺技チャージ停止
    /// </summary>
    protected void _StopSpecialCharge() {
        _currentSpecialChargeStopTime = _specialChargeStopTime;
    }

    /// <summary>
    /// 必殺技演出終了
    /// </summary>
    protected virtual void _OnSpecialCutInFinished(PlayableDirector obj) {
        // カットイン終了時に速度を元に戻す
        var pauseUI = GetPauseUI();
        if (pauseUI != null) pauseUI.canOpen = true;
        Time.timeScale = 1.0f;
        _isSpecialCutInViewing = false;
        _specialTimelineDirector.time = 0;
        _specialTimelineDirector.Stop();
    }

    /// <summary>
    /// 必殺技使用
    /// </summary>
    protected virtual void _UseSpecial() {
        if(!_specialTimelineDirector) {
            return;
        }
        // カットイン演出開始時にゲーム時間をスローにする
        var pauseUI = GetPauseUI();
        if (pauseUI != null) pauseUI.canOpen = false;
        Time.timeScale = 0.1f;
        _isSpecialUsing = true;
        _isSpecialCutInViewing = true;
        _specialTimelineDirector.time = 0;
        _specialTimelineDirector.Evaluate();
        _specialTimelineDirector.Play();
        _currentSpecialChargeTime = 0;
    }

    /// <summary>
    /// 召喚エフェクトとMP消費チェック
    /// </summary>
    /// <param name="ability_type">能力タイプ</param>
    /// <param name="un_recover_time">MP回復開始までのクールタイム</param>
    protected void _AppearCheck(eAbilityType ability_type, float un_recover_time = 0.5f, bool force_appear = false) {
        // 召喚エフェクト判定
        if (!_isAppearing || force_appear) {
            // MP消費
            _charaParam.SetUnRecoverTime_MP(un_recover_time);
            _charaParam.ConsumeMP(ability_type);
            // 召喚エフェクト再生
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity);
        }
        // 帰還タイマーリセット
        _ResetReturnTimer();
    }

    /// <summary>
    /// 必殺技強制チャージ
    /// </summary>
    public void ForceCharge(float rate = 1.0f) {
        _currentSpecialChargeTime = _specialChargeTime * rate;
    }
    private Pause_UI GetPauseUI()
    {
        if (_pauseUIInstance == null)
        {
            _pauseUIInstance = FindAnyObjectByType<Pause_UI>();
        }
        return _pauseUIInstance;
    }
}