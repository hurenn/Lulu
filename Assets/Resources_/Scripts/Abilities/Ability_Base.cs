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

    /// <summary>
    /// 方向入力
    /// </summary>
    protected Vector2 _inputDir = Vector2.zero;
    public virtual void DirectionInput(Vector2 dir) {
        _inputDir = dir;
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