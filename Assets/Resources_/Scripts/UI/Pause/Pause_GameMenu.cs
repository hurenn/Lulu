using UnityEngine;

public class Pause_GameMenu : Pause_MenuBase {
    public override void OnInputVertical(int dir) {
        MoveMenu(dir);
    }

    public override void OnInputHorizontal(int dir) {
        OnSwitchMenu(dir);
    }

    // PlayerControllerから呼ばれる上下入力処理
    public void MoveMenu(int dir) {
        int prevIndex = _selectedIndex;
        _selectedIndex = Mathf.Clamp(_selectedIndex + dir, 0, _menuButtonImages.Length - 1);
        if (_selectedIndex != prevIndex) {
            MoveFrameToSelected();

            // 選択音を再生
            if (_audioSource != null && _seSelect != null) {
                _audioSource.PlayOneShot(_seSelect);
            }
        }
    }

    public override void ExecuteSelectedMenu() {
        base.ExecuteSelectedMenu();
        // 決定音を再生
        if (_audioSource != null && _seDecide != null) {
            _audioSource.PlayOneShot(_seDecide);
        }
        switch (_selectedIndex) {
            case 0: // ゲームに戻る
                OnCloseMenu();
                break;
            case 1: // ステージセレクト
                // 空の動作
                break;
        }
    }
}
