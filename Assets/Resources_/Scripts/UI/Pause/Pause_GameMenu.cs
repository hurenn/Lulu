using System;
using UnityEngine;

public class Pause_GameMenu : Pause_MenuBase {
    [SerializeField] private RectTransform _MenuReturnGame;     // ゲームに戻るメニュー
    [SerializeField] private RectTransform _MenuStageSelect;    // ステージセレクトメニュー

    private enum eMenuIndex {
        ReturnGame = 0,
        StageSelect = 1,
    }
    private eMenuIndex _currentMenu = eMenuIndex.ReturnGame;

    public override void Open(Action<int> onSwitchMenu, Action onCloseMenu, AudioSource audio_source, AudioClip se_select, AudioClip se_decide) {
        base.Open(onSwitchMenu, onCloseMenu, audio_source, se_select, se_decide);

        // 最初の選択肢に枠を移動
        MoveFrameToSelected(_MenuReturnGame, true);
    }

    public override void OnInputVertical(int dir) {
        MoveMenu(dir);
    }

    public override void OnInputHorizontal(int dir) {
        OnSwitchMenu(dir);
    }

    // PlayerControllerから呼ばれる上下入力処理
    public void MoveMenu(int dir) {
        if (dir > 0) {
            // 上入力
            MoveFrameToSelected(_MenuReturnGame);
            _currentMenu = eMenuIndex.ReturnGame;
        } else if (dir < 0) {
            // 下入力
            MoveFrameToSelected(_MenuStageSelect);
            _currentMenu = eMenuIndex.StageSelect;
        }
    }

    public override void ExecuteSelectedMenu() {
        base.ExecuteSelectedMenu();
        // 決定音を再生
        if (_audioSource != null && _seDecide != null) {
            _audioSource.PlayOneShot(_seDecide);
        }
        switch (_currentMenu) {
            case eMenuIndex.ReturnGame: // ゲームに戻る
                OnCloseMenu();
                break;
            case eMenuIndex.StageSelect: // ステージセレクト
                // 空の動作
                break;
        }
    }
}
