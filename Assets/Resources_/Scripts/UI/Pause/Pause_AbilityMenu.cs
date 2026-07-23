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

    // ボタンとアイコンの割り当て管理
    private Dictionary<eButtonIndex, RectTransform> _buttonToIcon = new();
    // アイコンと説明文の対応
    private Dictionary<RectTransform, GameObject> _iconToExplain = new();
    // アイコンと能力タイプの対応
    private Dictionary<RectTransform, eAbilityType> _iconToAbilityType = new();
    // 掴んでいるアイコン
    private RectTransform _grabbedIcon = null;
    private eButtonIndex _grabbedFromButton = eButtonIndex.None;

    public override void Open(Action<int> onSwitchMenu, Action onCloseMenu, AudioSource audio_source, AudioClip se_select, AudioClip se_decide) {
        base.Open(onSwitchMenu, onCloseMenu, audio_source, se_select, se_decide);

        // アイコンと説明文の対応を初期化
        _iconToExplain[_WarpIcon] = _WarpExplain;
        _iconToExplain[_IceIcon] = _IceExplain;
        _iconToExplain[_LightIcon] = _LightExplain;
        _iconToExplain[_FireIcon] = _FireExplain;
        _iconToExplain[_WarpTriggerIcon] = _WarpExplain;
        _iconToExplain[_IceTriggerIcon] = _IceExplain;
        _iconToExplain[_LightTriggerIcon] = _LightExplain;
        _iconToExplain[_FireTriggerIcon] = _FireExplain;

        // アイコンと能力タイプの対応を初期化
        _iconToAbilityType[_WarpIcon] = eAbilityType.Warp;
        _iconToAbilityType[_IceIcon] = eAbilityType.Ice;
        _iconToAbilityType[_LightIcon] = eAbilityType.Light;
        _iconToAbilityType[_FireIcon] = eAbilityType.Fire;
        _iconToAbilityType[_WarpTriggerIcon] = eAbilityType.Warp;
        _iconToAbilityType[_IceTriggerIcon] = eAbilityType.Ice;
        _iconToAbilityType[_LightTriggerIcon] = eAbilityType.Light;
        _iconToAbilityType[_FireTriggerIcon] = eAbilityType.Fire;

        // 初期割り当て（デフォルト）
        _buttonToIcon[eButtonIndex.B] = _WarpIcon;
        _buttonToIcon[eButtonIndex.Y] = _IceIcon;
        _buttonToIcon[eButtonIndex.X] = _LightIcon;
        _buttonToIcon[eButtonIndex.A] = _FireIcon;
        _buttonToIcon[eButtonIndex.SR] = _WarpTriggerIcon;
        _buttonToIcon[eButtonIndex.ZL] = _IceTriggerIcon;
        _buttonToIcon[eButtonIndex.ZR] = _LightTriggerIcon;
        _buttonToIcon[eButtonIndex.SL] = _FireTriggerIcon;

        // 現在の能力割り当てにメインボタン(B,Y,X,A)のアイコン配置を同期
        _SyncButtonIconsWithCurrentAbilities();

        // アイコンを各ボタンに配置
        foreach (var pair in _buttonToIcon) {
            _PlaceIconOnButton(pair.Value, _GetButtonRect(pair.Key));
        }

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
                        eButtonIndex upTarget = _isMoveToSR ? eButtonIndex.SR : eButtonIndex.SL;
                        if (_CanMoveTo(upTarget)) {
                            _currentButton = upTarget;
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
                        //_currentButton = eButtonIndex.System;
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
                        //_currentButton = eButtonIndex.System;
                        break;
                    case eButtonIndex.ZL:
                        _currentButton = eButtonIndex.SL;
                        break;
                    case eButtonIndex.ZR:
                        _currentButton = eButtonIndex.SR;
                        break;
                    case eButtonIndex.SR:
                        if (_CanMoveTo(eButtonIndex.X)) {
                            _isMoveToSR = true;
                            _currentButton = eButtonIndex.X;
                        }
                        break;
                    case eButtonIndex.SL:
                        if (_CanMoveTo(eButtonIndex.X)) {
                            _isMoveToSR = false;
                            _currentButton = eButtonIndex.X;
                        }
                        break;
                    case eButtonIndex.System:
                        //_currentButton = eButtonIndex.Reset;
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
                    //case eButtonIndex.System:
                    //case eButtonIndex.Reset:
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
                    //case eButtonIndex.System:
                    //case eButtonIndex.Reset:
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
            _currentButton = eButtonIndex.Reset;
            _UpdateExplain();
            MoveFrameToSelected(_MenuButtonReset, true);
            return;
        }

        // B,Y,X,A,SL,SR,ZL,ZRボタン以外は何もしない
        if (_currentButton != eButtonIndex.B && _currentButton != eButtonIndex.Y &&
            _currentButton != eButtonIndex.X && _currentButton != eButtonIndex.A &&
            _currentButton != eButtonIndex.SL && _currentButton != eButtonIndex.SR &&
            _currentButton != eButtonIndex.ZL && _currentButton != eButtonIndex.ZR) {
            return;
        }

        // ボタンコンフィグ機能：アイコンの入れ替え
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

            // _selectFrameの右側にローカル座標で配置
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

        // 異なるグループ間の入れ替えは禁止（ABXY / SLSRZLZR）
        if (!_IsSameGroup(_grabbedFromButton, targetButton)) {
            return;
        }

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

        // PlayerCharacterの能力も入れ替える
        _SwapPlayerAbility(_grabbedFromButton, targetButton);

        // 掴んでいた状態を解除
        _grabbedIcon = null;
        _grabbedFromButton = eButtonIndex.None;

        // 説明文を更新
        _UpdateExplain();
    }

    /// <summary>
    /// PlayerCharacterの能力を入れ替える
    /// </summary>
    private void _SwapPlayerAbility(eButtonIndex fromButton, eButtonIndex toButton) {
        // B,Y,X,Aボタンのみ対応（SL,SR,ZL,ZRは除外）
        if (!_IsMainButton(fromButton) || !_IsMainButton(toButton)) {
            return;
        }

        var player = FindAnyObjectByType<Player_Character>();
        if (player == null) {
            Debug.LogWarning("Player_Characterが見つかりません");
            return;
        }

        // ボタンIndexからeAbilitySlotに変換
        eAbilitySlot fromSlot = _ButtonIndexToAbilitySlot(fromButton);
        eAbilitySlot toSlot = _ButtonIndexToAbilitySlot(toButton);

        // スロットの能力を入れ替え
        player.SwapAbilitySlot(fromSlot, toSlot);

        // 能力側の割り当て（PlayerParameter.Instance.Abilities）を即座に更新
        player.SaveAbilitySlot();

        // UIも入れ替え
        _SwapAbilityUI(fromSlot, toSlot);
    }

    /// <summary>
    /// AbilityUIを現在の能力割り当てに合わせて再配置する
    /// （2スロット間の相対的な入れ替えではなく、現在の実データから毎回組み直す）
    /// </summary>
    private void _SwapAbilityUI(eAbilitySlot slotA, eAbilitySlot slotB) {
        var abilityUIManager = FindAnyObjectByType<AbilityUIManager>();
        if (abilityUIManager == null) {
            Debug.LogWarning("AbilityUIManagerが見つかりません");
            return;
        }

        abilityUIManager.SyncAbilityUIToCurrentSlots(PlayerParameter.Instance.Abilities);
    }

    /// <summary>
    /// メインボタン（B,Y,X,A）かどうかを判定
    /// </summary>
    private bool _IsMainButton(eButtonIndex button) {
        return button == eButtonIndex.B || button == eButtonIndex.Y ||
               button == eButtonIndex.X || button == eButtonIndex.A;
    }

    /// <summary>
    /// トリガーボタン（SL,ZL,SR,ZR）かどうかを判定
    /// </summary>
    private bool _IsTriggerButton(eButtonIndex button) {
        return button == eButtonIndex.SL || button == eButtonIndex.ZL ||
               button == eButtonIndex.SR || button == eButtonIndex.ZR;
    }

    /// <summary>
    /// 2つのボタンが同じグループ（ABXY / SLSRZLZR）に属するかどうかを判定
    /// </summary>
    private bool _IsSameGroup(eButtonIndex a, eButtonIndex b) {
        return (_IsMainButton(a) && _IsMainButton(b)) || (_IsTriggerButton(a) && _IsTriggerButton(b));
    }

    /// <summary>
    /// アイコンを掴んでいる間、別グループのボタンへカーソル移動できないようにする判定
    /// </summary>
    private bool _CanMoveTo(eButtonIndex target) {
        return _grabbedIcon == null || _IsSameGroup(_grabbedFromButton, target);
    }

    /// <summary>
    /// ボタンIndexをeAbilitySlotに変換
    /// </summary>
    private eAbilitySlot _ButtonIndexToAbilitySlot(eButtonIndex button) {
        return button switch {
            eButtonIndex.B => eAbilitySlot.B,
            eButtonIndex.Y => eAbilitySlot.Y,
            eButtonIndex.X => eAbilitySlot.X,
            eButtonIndex.A => eAbilitySlot.A,
            _ => eAbilitySlot.B // デフォルト
        };
    }

    /// <summary>
    /// eAbilitySlotをボタンIndexに変換
    /// </summary>
    private eButtonIndex _AbilitySlotToButtonIndex(eAbilitySlot slot) {
        return slot switch {
            eAbilitySlot.B => eButtonIndex.B,
            eAbilitySlot.Y => eButtonIndex.Y,
            eAbilitySlot.X => eButtonIndex.X,
            eAbilitySlot.A => eButtonIndex.A,
            _ => eButtonIndex.B // デフォルト
        };
    }

    /// <summary>
    /// 能力タイプに対応するアイコンを取得
    /// </summary>
    private RectTransform _GetIconForAbilityType(eAbilityType type) {
        return type switch {
            eAbilityType.Warp => _WarpIcon,
            eAbilityType.Ice => _IceIcon,
            eAbilityType.Light => _LightIcon,
            eAbilityType.Fire => _FireIcon,
            _ => null
        };
    }

    /// <summary>
    /// 現在のプレイヤーの能力割り当てにメインボタン(B,Y,X,A)のアイコン配置を同期する
    /// （SL,SR,ZL,ZRのトリガーボタンにはゲームプレイ上の対応スロットが無いため対象外）
    /// </summary>
    private void _SyncButtonIconsWithCurrentAbilities() {
        var abilities = PlayerParameter.Instance?.Abilities;
        if (abilities == null) return;

        foreach (var pair in abilities) {
            var icon = _GetIconForAbilityType(pair.Key);
            if (icon == null) continue;
            _buttonToIcon[_AbilitySlotToButtonIndex(pair.Value)] = icon;
        }
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
            //eButtonIndex.System => _MenuButtonSystem,
            //eButtonIndex.Reset => _MenuButtonReset,
            _ => null
        };
    }

    /// <summary>
    /// 選択中のボタンに対応した説明文を表示
    /// </summary>
    private void _UpdateExplain() {
        // 全ての説明文を非表示
        _WarpExplain?.SetActive(false);
        _IceExplain?.SetActive(false);
        _LightExplain?.SetActive(false);
        _FireExplain?.SetActive(false);

        // 現在選択中のボタンに対応するアイコンを取得
        if (_buttonToIcon.TryGetValue(_currentButton, out var currentIcon)) {
            // そのアイコンに対応した説明文を表示
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
                //eButtonIndex.System => _MenuButtonSystem,
                //eButtonIndex.Reset => _MenuButtonReset,
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
