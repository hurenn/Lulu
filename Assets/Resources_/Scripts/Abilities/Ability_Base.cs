using UnityEngine;

public enum eAbilityResult {
    None,
    IceSlash1,
    IceSlash2,
    IceSlash3,
    IceSeparate,
    FireShot,
    LightParry,
    LightDome,
}

public class Ability_Base : MonoBehaviour {

    /// <summary>
    /// 地上にいるか確認
    /// </summary>
    public bool _isGround = true;

    protected Transform _charaTransform = default;
    /// <summary>
    /// 装備時のローカルポジション
    /// </summary>
    [SerializeField] protected Vector3 _localPosition = Vector3.zero;
    /// <summary>
    /// キャラクター表示
    /// </summary>
    [SerializeField] protected SpriteRenderer _rend = null;
    [SerializeField] protected Animator _anim = null;

    [SerializeField] protected GameObject _warpAnimationPrefab = null;

    /// <summary>
    /// 右向きか確認
    /// </summary>
    protected bool _isRight = true;
    public virtual void SetCharacterTransform(bool is_right, Transform character_pos) {
        _isRight = is_right;
        _charaTransform = character_pos;
    }

    /// <summary>
    /// 方向入力
    /// </summary>
    protected Vector2 _inputDir = Vector2.zero;
    public virtual void UpdateTransform(Vector3 character_pos, Vector2 dir) {
        _inputDir = dir;
        transform.position = character_pos + new Vector3(
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
}