using UnityEngine;

public class Pause_AbilityMenu : Pause_MenuBase {
    [SerializeField] private RectTransform _buttonY;
    [SerializeField] private RectTransform _buttonX;
    [SerializeField] private RectTransform _buttonB;
    [SerializeField] private RectTransform _buttonA;

    private enum eButtonIndex {
        Y = 0,
        X = 1,
        B = 2,
        A = 3,
    }
    private eButtonIndex _currentButton = eButtonIndex.B;

    private enum eInputDirection {
        None = 0,
        Up = 1,
        Down = 2,
        Right = 3,
        Left = 4,
    }

    public override void OnInputVertical(int dir) {
        eInputDirection dir_enum = dir > 0 ? eInputDirection.Up : eInputDirection.Down;
        SelectButton(dir_enum);
    }

    public override void OnInputHorizontal(int dir) {
        eInputDirection dir_enum = dir > 0 ? eInputDirection.Right : eInputDirection.Left;
        SelectButton(dir_enum);
    }

    // PlayerController‚©‚çŒÄ‚Î‚ê‚é•ûŒü“ü—Íˆ—
    private void SelectButton(eInputDirection dir) {

        switch (dir) {
            case eInputDirection.Up:
                _currentButton = eButtonIndex.X;
                break;
            case eInputDirection.Down:
                _currentButton = eButtonIndex.B;
                break;
            case eInputDirection.Left:
                if (_currentButton == eButtonIndex.Y) {
                    OnSwitchMenu(-1);
                } else {
                    _currentButton = eButtonIndex.Y;
                }
                break;
            case eInputDirection.Right:
                if (_currentButton == eButtonIndex.A) {
                    OnSwitchMenu(1);
                } else {
                    _currentButton = eButtonIndex.A;
                }
                break;
        }

        // ‘I‘ğ‰¹‚ğÄ¶
        if (_audioSource != null && _seSelect != null) {
            _audioSource.PlayOneShot(_seSelect);
        }
    }

    public override void ExecuteSelectedMenu() {
        base.ExecuteSelectedMenu();
        // Œˆ’è‰¹‚ğÄ¶
        if (_audioSource != null && _seDecide != null) {
            _audioSource.PlayOneShot(_seDecide);
        }
    }
}
