using UnityEngine;

[CreateAssetMenu(fileName = "CommonParameter", menuName = "Game/CommonStatus", order = 1)]
public class CommonParameter : ScriptableObject
{
    [Header("移動力")]
    public float moveSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float slideSpeed = 12.0f; // スライディング速度

    [Header("ジャンプ力")]
    public float jumpForce = 5.0f;
    public float dashJumpForce = 6.0f; // ダッシュジャンプの力
    public float slideJumpForce = 8.0f; // スライディングジャンプの力
    public float maxJumpSpeed = 20.0f;

    [Header("重力")]
    public float gravity = -30.0f;
    public float fallMultiplier = 2.0f;
    public float maxFallSpeed = -20.0f;

    [Header("最大ジャンプ時間")]
    public float maxJumpHoldTime = 0.2f;

    [Header("ダッシュ入力猶予")]
    public float dashInputThreshold = 0.2f;

    [Header("ジャンプ入力猶予")]
    public float jumpBufferTime = 0.1f;

    [Header("ワープダッシュ速度")]
    public Vector2 warpDashUpRight = new Vector2(5.0f, 15.0f);
    public Vector2 warpDashRight = new Vector2(15.0f, 1.0f);
    public Vector2 warpDashDownRight = new Vector2(20.0f, -10.0f);
    public Vector2 warpDashDown = new Vector2(0.0f, -25.0f);
    public Vector2 warpDashDownLeft => new Vector2(-warpDashDownRight.x, warpDashDownRight.y);
    public Vector2 warpDashLeft => new Vector2(-warpDashRight.x, warpDashRight.y);
    public Vector2 warpDashUpLeft => new Vector2(-warpDashUpRight.x, warpDashUpRight.y);
    public Vector2 warpDashUp = new Vector2(0.0f, 10.0f);
    public float warpDashDamping = 0.3f; // ワープダッシュの減衰率

    [Header("ワープ実行までの待機時間")]
    public float warpWaitTime = 0.1f;

    [Header("ワープクールタイム")]
    public float warpCoolTime = 0.1f;

    [Header("ワープダッシュ時間")]
    public float maxWarpDashTime = 0.5f; // ワープダッシュの最大時間

    [Header("スライディング時間")]
    public float maxSlideTime = 1f;

    [Header("壁スライド速度")]
    public float wallSlideSpeed = 2.0f;

    [Header("着地ダッシュ有効時間")]
    public float maxLandingDashTime = 0.5f; // 着地ダッシュの有効時間

    [Header("地形チェック幅")]
    public float groundCheckHeight = 0.05f;
    public float wallCheckWidth = 0.1f;
    public float checkerBuffer = 0.05f;

}

public struct CharacterInputData
{
    public Vector2 move;     // 入力
    public bool jumpPressed; // ジャンプボタンを押した瞬間
    public bool jumpHeld;    // ジャンプボタンを押し続ける
}
