using System;
using UnityEngine;

public class Pause_GameMenu : Pause_MenuBase {
    [SerializeField] private RectTransform _MenuReturnGame;     // ゲームに戻るメニュー
    [SerializeField] private RectTransform _MenuStageRetry;     // リトライメニュー
    [SerializeField] private RectTransform _MenuTitle;          // タイトルに戻るメニュー

    private enum eMenuIndex {
        ReturnGame = 0,
        Retry = 1,
        Title = 2
    }
    private eMenuIndex _currentMenu = eMenuIndex.ReturnGame;

    public override void Open(Action<int> onSwitchMenu, Action onCloseMenu, AudioSource audio_source, AudioClip se_select, AudioClip se_decide) {
        base.Open(onSwitchMenu, onCloseMenu, audio_source, se_select, se_decide);

        // 最初の選択肢に枠を移動
        MoveFrameToSelected(_MenuReturnGame, true);
        _currentMenu = eMenuIndex.ReturnGame;
    }

    public override void OnInputVertical(int dir) {
        // メニューのインデックスを上下に移動
        int nextIndex = (int)_currentMenu + (dir > 0 ? -1 : 1);
        nextIndex = Mathf.Clamp(nextIndex, 0, (int)eMenuIndex.Title);
        OnSwitchGameMenu(nextIndex);
    }

    public override void OnInputHorizontal(int dir) {
        OnSwitchMenu(dir);
    }

    private void OnSwitchGameMenu(int index) {
        switch (index) {
            case 0: // ゲームに戻る
                MoveFrameToSelected(_MenuReturnGame);
                _currentMenu = eMenuIndex.ReturnGame;
                break;
            case 1: // やりなおす
                MoveFrameToSelected(_MenuStageRetry);
                _currentMenu = eMenuIndex.Retry;
                break;
            case 2: // タイトルに戻る
                MoveFrameToSelected(_MenuTitle);
                _currentMenu = eMenuIndex.Title;
                break;
        }
    }

    public override void ExecuteSelectedMenu() {
        base.ExecuteSelectedMenu();
        // 決定音再生
        if (_audioSource != null && _seDecide != null) {
            _audioSource.PlayOneShot(_seDecide);
        }
        switch (_currentMenu) {
            case eMenuIndex.ReturnGame: // ゲームに戻る
                OnCloseMenu();
                break;
            case eMenuIndex.Retry: // やり直し（ポーズからのリトライなので必殺チャージなし）
                GameSceneManager.Instance.StageRestart(false, false);
                OnCloseMenu();
                break;
            case eMenuIndex.Title: // タイトルに戻る
                GameSceneManager.Instance.GameRestart();
                OnCloseMenu();
                break;
        }
    }
}
