using UnityEngine;

[CreateAssetMenu(fileName = "CommonParameter", menuName = "Lulu/CommonStatus", order = 1)]
public class CommonParameter : ScriptableObject
{
    [Header("移動力")]
    public float moveSpeed = 5.0f;
    public float dashSpeed = 10.0f;
    public float slideJumpSpeed = 14.0f;
    public float slideSpeed = 15.0f; // スライディング速度

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
    
    [Header("ワープ慣性制御")]
    public float warpDashControlAccel = 10.0f;  // 加速力
    public float warpDashControlDecel = 20.0f;  // 減速力
    public float warpDashControlSteer = 2.0f;   // カーブ力

    [Header("スライディング時間")]
    public float maxSlideTime = 1f;
    [Header("スライディングキャンセル減衰力")]
    public float slideCancelDamping = 0.5f; // スライディングキャンセル減衰力

    [Header("壁スライディング時間")]
    public float maxWallSlideTime = 0.3f; // 壁スライディングの最大時間
    [Header("壁スライド速度")]
    public float wallSlideSpeed = 2.0f;

    [Header("着地ダッシュ有効時間")]
    public float maxLandingDashTime = 0.5f; // 着地ダッシュの有効時間

    [Header("地形チェック幅")]
    public float groundCheckHeight = 0.05f;
    public float wallCheckWidth = 0.1f;
    public float checkerBuffer = 0.05f;

    [Header("氷能力")]
    public float iceSlashInterval = 0.1f;   // 攻撃の隙
    public float slashRebound = 2.0f;       // 空中攻撃の反動ジャンプ力
    public float slashMoveForce = 2.0f;     // 空中攻撃の移動力
    public float comboReceptionTime = 0.7f; // コンボ入力受付時間
    public float comboIntervalTime = 0.2f;  // 1コンボインターバル時間
    public float comboCoolTime = 0.5f;     // コンボ終了後のクールタイム
    public float moveDuration = 0.05f;     // 移動にかける時間

    [Header("レベル関連")]
    public float mpUpPerLevel = 30.0f;    // レベルアップ毎の最大MP増加量
    public int attackUpPerLevel = 5; // レベルアップ毎の攻撃力増加量
}

/// <summary>
/// 能力ボタン1つ分の押下状態
/// </summary>
public struct AbilityButtonState
{
    public bool pressed;  // 押した瞬間
    public bool held;     // 押し続けている
    public bool released; // 離した瞬間

    public void Clear()
    {
        pressed = false;
        held = false;
        released = false;
    }
}

public struct CharacterInputData
{
    public void Clear()
    {
        move = Vector2.zero;
        abilityY.Clear();
        abilityX.Clear();
        abilityA.Clear();
        abilityB.Clear();
        messageNextPressed = false;
    }

    public Vector2 move;     // 方向入力
    public bool isJumpPressed;  // ジャンプボタンを押した瞬間
    public bool isJumpHeld;     // ジャンプボタンを押し続ける
    public bool isJumpReleased; // ジャンプボタンを離した瞬間

    // 能力スロットごとのボタン状態（スロットとボタンの対応はPlayerControllerが管理する）
    public AbilityButtonState abilityY;
    public AbilityButtonState abilityX;
    public AbilityButtonState abilityA;
    public AbilityButtonState abilityB;

    public bool messageNextPressed; // メッセージ送りボタンを押した瞬間

    /// <summary>
    /// スロット指定で能力ボタンの状態を取得
    /// </summary>
    public AbilityButtonState GetAbilityButton(eAbilitySlot slot) {
        return slot switch {
            eAbilitySlot.Y => abilityY,
            eAbilitySlot.X => abilityX,
            eAbilitySlot.A => abilityA,
            eAbilitySlot.B => abilityB,
            _ => default
        };
    }

    /// <summary>
    /// スロット指定で能力ボタンの状態を設定
    /// </summary>
    public void SetAbilityButton(eAbilitySlot slot, AbilityButtonState state) {
        switch (slot) {
            case eAbilitySlot.Y: abilityY = state; break;
            case eAbilitySlot.X: abilityX = state; break;
            case eAbilitySlot.A: abilityA = state; break;
            case eAbilitySlot.B: abilityB = state; break;
        }
    }
}
