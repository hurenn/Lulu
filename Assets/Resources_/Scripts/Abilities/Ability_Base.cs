using UnityEngine;

public enum eAbilityResult {
    None,
    IceSlash1,
    IceSlash2,
    IceSlash3,
    IceSeparate,
    IceLockonSlash,
    FireShot,
    LightParry,
    LightDome,
}

public class Ability_Base : MonoBehaviour {

    /// <summary>
    /// 地上にいるか確認
    /// </summary>
    public bool _isGround = true;

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

    /// <summary>
    /// 右向きか確認
    /// </summary>
    protected bool _isRight = true;
    public virtual void UpdateParameter(
        bool is_right, 
        Transform chara_pos, 
        CommonParameter common_param, 
        CharacterParameter_Player chara_param,
        WarpControl warp_control) {
        _isRight = is_right;
        _playerTransform = chara_pos;
        _param = common_param;
        _charaParam = chara_param;
        _warpControl = warp_control;
    }

    private void Update() {
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
    public virtual void UpdatePartnerTransform() {
        transform.position = _playerTransform.position + new Vector3(
            _localPosition.x * (_isRight ? -1 : 1),
            _localPosition.y,
            _localPosition.z);
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
}