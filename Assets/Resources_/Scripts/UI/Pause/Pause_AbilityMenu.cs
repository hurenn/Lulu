using System;
using System.Collections.Generic;
using UnityEngine;

public class Pause_AbilityMenu : Pause_MenuBase {
    // メニューボタン
    [SerializeField] private RectTransform _MenuButtonB;
    [SerializeField] private RectTransform _MenuButtonY;
    [SerializeField] private RectTransform _MenuButtonX;
    [SerializeField] private RectTransform _MenuButtonA;
    [SerializeField] private RectTransform _MenuButtonSL;
    [SerializeField] private RectTransform _MenuButtonZL;
    [SerializeField] private RectTransform _MenuButtonSR;
    [SerializeField] private RectTransform _MenuButtonZR;
    [SerializeField] private RectTransform _MenuButtonSystem;
    [SerializeField] private RectTransform _MenuButtonReset;

    // 能力アイコン
    [SerializeField] private RectTransform _IconParent;
    [SerializeField] private RectTransform _WarpIcon;
    [SerializeField] private RectTransform _IceIcon;
    [SerializeField] private RectTransform _LightIcon;
    [SerializeField] private RectTransform _FireIcon;
    [SerializeField] private RectTransform _WarpTriggerIcon;
    [SerializeField] private RectTransform _IceTriggerIcon;
    [SerializeField] private RectTransform _LightTriggerIcon;
    [SerializeField] private RectTransform _FireTriggerIcon;

    // 対応ボタン
    private eButtonIndex _WarpButton = eButtonIndex.B;
    private eButtonIndex _IceButton = eButtonIndex.Y;
    private eButtonIndex _LightButton = eButtonIndex.X;
    private eButtonIndex _FireButton = eButtonIndex.A;
    //private eButtonIndex _WarpTriggerButton = eButtonIndex.SR;
    //private eButtonIndex _IceTriggerButton = eButtonIndex.ZL;
    //private eButtonIndex _LightTriggerButton = eButtonIndex.ZR;
    //private eButtonIndex _FireTriggerButton = eButtonIndex.SL;

    // 能力説明文
    [SerializeField] private GameObject _WarpExplain;
    [SerializeField] private GameObject _IceExplain;
    [SerializeField] private GameObject _LightExplain;
    [SerializeField] private GameObject _FireExplain;

    // システムテキスト
    [SerializeField] private GameObject _SystemText1;
    [SerializeField] private GameObject _SystemText2;
    private bool _isViewSystemText1 = true;

    private bool _isMoveToSR = false;

    private enum eButtonIndex {
        None = -1,

        B = 0,
        Y = 1,
        X = 2,
        A = 3,

        SL = 4,
        ZL = 5,
        SR = 6,
        ZR = 7,

        System = 8,
        Reset = 9,
    }
    private eButtonIndex _currentButton = eButtonIndex.B;

    private enum eInputDirection {
        None = 0,
        Up = 1,
        Down = 2,
        Right = 3,
        Left = 4,
    }

    // ボタン→アイコンの割り当て管理
    private Dictionary<eButtonIndex, RectTransform> _buttonToIcon = new();
    // アイコン→説明文の対応
    private Dictionary<RectTransform, GameObject> _iconToExplain = new();
    // 掴んでいるアイコン
    private RectTransform _grabbedIcon = null;
    private eButtonIndex _grabbedFromButton = eButtonIndex.None;

    public override void Open(Action<int> onSwitchMenu, Action onCloseMenu, AudioSource audio_source, AudioClip se_select, AudioClip se_decide) {
        base.Open(onSwitchMenu, onCloseMenu, audio_source, se_select, se_decide);

        // アイコン→説明文の対応を初期化
        _iconToExplain[_WarpIcon] = _WarpExplain;
        _iconToExplain[_IceIcon] = _IceExplain;
        _iconToExplain[_LightIcon] = _LightExplain;
        _iconToExplain[_FireIcon] = _FireExplain;
        _iconToExplain[_WarpTriggerIcon] = _WarpExplain;
        _iconToExplain[_IceTriggerIcon] = _IceExplain;
        _iconToExplain[_LightTriggerIcon] = _LightExplain;
        _iconToExplain[_FireTriggerIcon] = _FireExplain;

        // 初期割り当て
        _buttonToIcon[eButtonIndex.B] = _WarpIcon;
        _buttonToIcon[eButtonIndex.Y] = _IceIcon;
        _buttonToIcon[eButtonIndex.X] = _LightIcon;
        _buttonToIcon[eButtonIndex.A] = _FireIcon;
        _buttonToIcon[eButtonIndex.SR] = _WarpTriggerIcon;
        _buttonToIcon[eButtonIndex.ZL] = _IceTriggerIcon;
        _buttonToIcon[eButtonIndex.ZR] = _LightTriggerIcon;
        _buttonToIcon[eButtonIndex.SL] = _FireTriggerIcon;

        // アイコンを各ボタンに配置
        _PlaceIconOnButton(_WarpIcon, _MenuButtonB);
        _PlaceIconOnButton(_IceIcon, _MenuButtonY);
        _PlaceIconOnButton(_LightIcon, _MenuButtonX);
        _PlaceIconOnButton(_FireIcon, _MenuButtonA);
        _PlaceIconOnButton(_WarpTriggerIcon, _MenuButtonSR);
        _PlaceIconOnButton(_IceTriggerIcon, _MenuButtonZL);
        _PlaceIconOnButton(_LightTriggerIcon, _MenuButtonZR);
        _PlaceIconOnButton(_FireTriggerIcon, _MenuButtonSL);

        // 最初の選択肢に枠を移動
        MoveFrameToSelected(_MenuButtonB, true);
        _currentButton = eButtonIndex.B;
        _grabbedIcon = null;
        _grabbedFromButton = eButtonIndex.None;

        // 説明文を更新
        _UpdateExplain();

        // システムテキスト表示更新
        _UpdateSystemText();
    }

    public override void OnInputVertical(int dir) {
        eInputDirection dir_enum = dir > 0 ? eInputDirection.Up : eInputDirection.Down;
        SelectButton(dir_enum);
    }

    public override void OnInputHorizontal(int dir) {
        eInputDirection dir_enum = dir > 0 ? eInputDirection.Right : eInputDirection.Left;
        SelectButton(dir_enum);
    }

    // PlayerControllerから呼ばれる方向入力処理
    private void SelectButton(eInputDirection dir) {
        switch (dir) {
            case eInputDirection.Up:
                switch (_currentButton) {
                    case eButtonIndex.B:
                    case eButtonIndex.Y:
                    case eButtonIndex.A:
                        _currentButton = eButtonIndex.X;
                        break;
                    case eButtonIndex.X:
                        if (_isMoveToSR) {
                            _currentButton = eButtonIndex.SR;
                        } else {
                            _currentButton = eButtonIndex.SL;
                        }
                        break;
                    case eButtonIndex.SL:
                        _currentButton = eButtonIndex.ZL;
                        break;
                    case eButtonIndex.SR:
                        _currentButton = eButtonIndex.ZR;
                        break;
                    case eButtonIndex.System:
                        _currentButton = eButtonIndex.B;
                        break;
                    case eButtonIndex.Reset:
                        _currentButton = eButtonIndex.System;
                        break;
                }
                break;
            case eInputDirection.Down:
                switch (_currentButton) {
                    case eButtonIndex.X:
                    case eButtonIndex.Y:
                    case eButtonIndex.A:
                        _currentButton = eButtonIndex.B;
                        break;
                    case eButtonIndex.B:
                        _currentButton = eButtonIndex.System;
                        break;
                    case eButtonIndex.ZL:
                        _currentButton = eButtonIndex.SL;
                        break;
                    case eButtonIndex.ZR:
                        _currentButton = eButtonIndex.SR;
                        break;
                    case eButtonIndex.SR:
                        _isMoveToSR = true;
                        _currentButton = eButtonIndex.X;
                        break;
                    case eButtonIndex.SL:
                        _isMoveToSR = false;
                        _currentButton = eButtonIndex.X;
                        break;
                    case eButtonIndex.System:
                        _currentButton = eButtonIndex.Reset;
                        break;
                }
                break;
            case eInputDirection.Left:
                switch (_currentButton) {
                    case eButtonIndex.ZR:
                    case eButtonIndex.SL:
                        _currentButton = eButtonIndex.ZL;
                        break;
                    case eButtonIndex.SR:
                        _currentButton = eButtonIndex.SL;
                        break;
                    case eButtonIndex.X:
                    case eButtonIndex.A:
                    case eButtonIndex.B:
                        _currentButton = eButtonIndex.Y;
                        break;
                    case eButtonIndex.Y:
                    case eButtonIndex.ZL:
                    case eButtonIndex.System:
                    case eButtonIndex.Reset:
                        OnSwitchMenu(-1);
                        break;
                }
                break;
            case eInputDirection.Right:
                switch (_currentButton) {
                    case eButtonIndex.SL:
                        _currentButton = eButtonIndex.SR;
                        break;
                    case eButtonIndex.ZL:
                    case eButtonIndex.SR:
                        _currentButton = eButtonIndex.ZR;
                        break;
                    case eButtonIndex.Y:
                    case eButtonIndex.B:
                    case eButtonIndex.X:
                        _currentButton = eButtonIndex.A;
                        break;
                    case eButtonIndex.A:
                    case eButtonIndex.ZR:
                    case eButtonIndex.System:
                    case eButtonIndex.Reset:
                        OnSwitchMenu(1);
                        break;
                }
                break;
        }
        _UpdateFrame();
        // 説明文を更新
        _UpdateExplain();
    }

    public override void ExecuteSelectedMenu() {
        base.ExecuteSelectedMenu();
        // 決定音を再生
        if (_audioSource != null && _seDecide != null) {
            _audioSource.PlayOneShot(_seDecide);
        }

        // システムテキスト切り替え
        if (_currentButton == eButtonIndex.System) {
            _isViewSystemText1 = !_isViewSystemText1;
            _UpdateSystemText();
            return;
        }

        if(_currentButton == eButtonIndex.Reset) {
            // リセット：全ての割り当てを初期状態に戻す
            Open(OnSwitchMenu, OnCloseMenu, _audioSource, _seSelect, _seDecide);
            return;
        }

        // B,Y,X,A,SL,SR,ZL,ZRボタン以外は何もしない
        if (_currentButton != eButtonIndex.B && _currentButton != eButtonIndex.Y &&
            _currentButton != eButtonIndex.X && _currentButton != eButtonIndex.A &&
            _currentButton != eButtonIndex.SL && _currentButton != eButtonIndex.SR &&
            _currentButton != eButtonIndex.ZL && _currentButton != eButtonIndex.ZR) {
            return;
        }

        // 1回目の決定：アイコンを掴む
        if (_grabbedIcon == null) {
            _GrabIcon(_currentButton);
        }
        // 2回目の決定：アイコンを入れ替える
        else {
            _SwapIcon(_currentButton);
        }
    }

    /// <summary>
    /// アイコンを掴む
    /// </summary>
    private void _GrabIcon(eButtonIndex button) {
        if (!_buttonToIcon.TryGetValue(button, out var icon) || icon == null) return;

        _grabbedIcon = icon;
        _grabbedFromButton = button;

        // アイコンを_selectFrameの子に設定
        if (_selectFrame != null) {
            _grabbedIcon.SetParent(_selectFrame.rectTransform, false);
            
            // _selectFrameの右上にローカル座標で配置
            var iconRect = _grabbedIcon.GetComponent<RectTransform>();
            if (iconRect != null) {
                var frameRect = _selectFrame.rectTransform;
                Vector2 localPos = new Vector2(
                    frameRect.rect.width * 0.5f + iconRect.rect.width * 0.5f + 10f,
                    frameRect.rect.height * 0.5f
                );
                iconRect.anchoredPosition = localPos;
            }
        }
    }

    /// <summary>
    /// アイコンを入れ替える
    /// </summary>
    private void _SwapIcon(eButtonIndex targetButton) {
        if (_grabbedIcon == null || _grabbedFromButton == eButtonIndex.None) return;

        // 入れ替え先のアイコンを取得
        _buttonToIcon.TryGetValue(targetButton, out var targetIcon);

        // 入れ替え
        _buttonToIcon[targetButton] = _grabbedIcon;
        if (targetIcon != null) {
            _buttonToIcon[_grabbedFromButton] = targetIcon;
            _PlaceIconOnButton(targetIcon, _GetButtonRect(_grabbedFromButton));
        } else {
            _buttonToIcon.Remove(_grabbedFromButton);
        }

        // 掴んでいたアイコンを配置
        _PlaceIconOnButton(_grabbedIcon, _GetButtonRect(targetButton));

        // 掴んでいる状態を解除
        _grabbedIcon = null;
        _grabbedFromButton = eButtonIndex.None;

        // 説明文を更新
        _UpdateExplain();
    }

    /// <summary>
    /// アイコンをボタンに配置
    /// </summary>
    private void _PlaceIconOnButton(RectTransform icon, RectTransform button) {
        if (icon == null || button == null) return;
        
        // アイコンを_IconParentの子に戻す
        if (_IconParent != null) {
            icon.SetParent(_IconParent, true);
        }
        
        // ワールド座標でボタン位置に配置
        icon.position = button.position;
    }

    // ボタンのRectTransform取得
    private RectTransform _GetButtonRect(eButtonIndex btn) {
        return btn switch {
            eButtonIndex.B => _MenuButtonB,
            eButtonIndex.Y => _MenuButtonY,
            eButtonIndex.X => _MenuButtonX,
            eButtonIndex.A => _MenuButtonA,
            eButtonIndex.SL => _MenuButtonSL,
            eButtonIndex.ZL => _MenuButtonZL,
            eButtonIndex.SR => _MenuButtonSR,
            eButtonIndex.ZR => _MenuButtonZR,
            eButtonIndex.System => _MenuButtonSystem,
            eButtonIndex.Reset => _MenuButtonReset,
            _ => null
        };
    }

    /// <summary>
    /// 選択中のボタンに対応する説明文を表示
    /// </summary>
    private void _UpdateExplain() {
        // 全ての説明文を非表示
        _WarpExplain?.SetActive(false);
        _IceExplain?.SetActive(false);
        _LightExplain?.SetActive(false);
        _FireExplain?.SetActive(false);

        // 現在選択中のボタンに対応するアイコンを取得
        if (_buttonToIcon.TryGetValue(_currentButton, out var currentIcon)) {
            // そのアイコンに対応する説明文を表示
            if (_iconToExplain.TryGetValue(currentIcon, out var explain)) {
                explain?.SetActive(true);
            }
        }
    }

    // 選択枠の位置更新
    private void _UpdateFrame() {
        MoveFrameToSelected(
            _currentButton switch {
                eButtonIndex.B => _MenuButtonB,
                eButtonIndex.Y => _MenuButtonY,
                eButtonIndex.X => _MenuButtonX,
                eButtonIndex.A => _MenuButtonA,
                eButtonIndex.SL => _MenuButtonSL,
                eButtonIndex.ZL => _MenuButtonZL,
                eButtonIndex.SR => _MenuButtonSR,
                eButtonIndex.ZR => _MenuButtonZR,
                eButtonIndex.System => _MenuButtonSystem,
                eButtonIndex.Reset => _MenuButtonReset,
                _ => _MenuButtonB
            });
    }

    private void _UpdateSystemText() {
        if (_SystemText1 != null && _SystemText2 != null) {
            _SystemText1.SetActive(_isViewSystemText1);
            _SystemText2.SetActive(!_isViewSystemText1);
        }
    }
}
