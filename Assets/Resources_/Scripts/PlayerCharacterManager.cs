using System;
using UnityEngine;

/// <summary>
/// 現在操作中のキャラクターを管理する。
/// 将来的な操作キャラクター切り替えのため、Player_Character型を直接検索する代わりにここを参照する。
/// </summary>
public static class PlayerCharacterManager {
    private static Character_Base _current;
    private static PlayerController _controller;

    /// <summary>
    /// 現在操作中のキャラクター（未登録の場合はシーンから検索）
    /// </summary>
    public static Character_Base Current {
        get {
            if (_current == null) {
                _current = UnityEngine.Object.FindAnyObjectByType<Player_Character>();
            }
            return _current;
        }
    }

    /// <summary>
    /// 現在の入力受付を行っているPlayerController（未登録の場合はシーンから検索）
    /// </summary>
    public static PlayerController Controller {
        get {
            if (_controller == null) {
                _controller = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }
            return _controller;
        }
    }

    /// <summary>
    /// 操作中のキャラクターが切り替わった時に発火
    /// </summary>
    public static event Action<Character_Base> OnCharacterChanged;

    /// <summary>
    /// 操作中のキャラクターを設定する（PlayerControllerの操作対象切り替え時に呼ぶ）
    /// </summary>
    public static void SetCurrent(Character_Base character) {
        if (_current == character) {
            return;
        }
        _current = character;
        OnCharacterChanged?.Invoke(character);
    }

    /// <summary>
    /// 現在のPlayerControllerを設定する
    /// </summary>
    public static void SetController(PlayerController controller) {
        _controller = controller;
    }
}
