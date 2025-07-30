using UnityEngine;

[CreateAssetMenu(fileName = "CommonParameter", menuName = "Game/CommonStatus", order = 1)]
public class CommonParameter : ScriptableObject
{
    [Header("移動力")]
    public float moveSpeed = 5.0f;

    [Header("ジャンプ力")]
    public float jumpForce = 5.0f;

    [Header("重力")]
    public float gravity = -30.0f;
    public float fallMultiplier = 2.0f;
    public float maxFallSpeed = -20.0f;

    [Header("最大ジャンプ時間")]
    public float maxJumpHoldTime = 0.2f;

    [Header("ジャンプ入力猶予")]
    public float jumpBufferTime = 0.1f;
}

public struct CharacterInputData
{
    public Vector2 move;     // 入力
    public bool jumpPressed; // ジャンプボタンを押した瞬間
    public bool jumpHeld;    // ジャンプボタンを押し続ける
}
