using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    [SerializeField] private Character_Base character;
    public Character_Base Character => character;

    // コントローラー
    private InputActions _inputActions;

    // 方向入力
    private Vector2 _moveInputValue;

    // Abilityボタンの状態（スロットごと）
    private AbilityButtonState[] _abilityButtons = new AbilityButtonState[4];
    // スロットと物理ボタン(InputAction)の対応表（ボタンコンフィグの差し替え対象）
    private InputAction[] _abilitySlotActions = new InputAction[4];

    // メッセージ送り入力
    private bool _isMessageNextPressed = false;
    private bool _wasMessageNextPressed = false; // 前フレームのボタン状態

    // キャラクター操作入力受付フラグ
    public bool isEnabledCharacterInput { get; set; } = true;

    // --- Inspector用コントローラー ---
    [Header("Character Controls")]
    public bool insertMoveLeft = false;
    public bool insertMoveRight = false;
    public bool insertMoveUp = false;
    public bool insertMoveDown = false;
    [Space]
    private bool _insertJumpButtonPressed = false;
    public bool insertJumpHeld = false;
    private bool _insertJumpReleased = true;
    [Space]
    public bool insertMessageNext = false;

    private CharacterInputData input;
    public CharacterInputData Input => input;
    public CharacterInputData virtualInput { get; set; }

    // 特定入力完了コールバック
    System.Action _inputCompletedCallback = null;
    // 特定入力記憶
    CharacterInputData _specificInput = new CharacterInputData();

    public void SetSpecificInput(CharacterInputData specific_input, System.Action input_completed) {
        //_ResetInput();
        _specificInput = specific_input;
        _inputCompletedCallback = input_completed;
    }

    private Pause_UI _pauseUIInstance;
    private float _pauseMenuInputCooldown = 0f;
    private float _pauseDecideInputCooldown = 0f; // 決定入力のクールタイム
    private const float PAUSE_MENU_INPUT_COOLDOWN = 0.5f;
    private const float PAUSE_DECIDE_INPUT_COOLDOWN = 0.3f; // 決定入力のクールタイム

    // ボタン割り当ての保存キー
    private const string BINDING_OVERRIDES_PREF_KEY = "InputBindingOverrides";

    // Pause_UIインスタンスをキャッシュして取得
    private Pause_UI GetPauseUI()
    {
        if (_pauseUIInstance == null)
        {
            _pauseUIInstance = FindAnyObjectByType<Pause_UI>();
        }
        return _pauseUIInstance;
    }

    private void Awake() {
        _inputActions = new InputActions();

        // スロットと物理ボタンの対応表を構築
        _abilitySlotActions[(int)eAbilitySlot.Y] = _inputActions.Player.AbilityY;
        _abilitySlotActions[(int)eAbilitySlot.X] = _inputActions.Player.AbilityX;
        _abilitySlotActions[(int)eAbilitySlot.A] = _inputActions.Player.AbilityA;
        _abilitySlotActions[(int)eAbilitySlot.B] = _inputActions.Player.AbilityB;

        // 保存済みのボタン割り当てを適用
        _LoadBindingOverrides();
    }

    private void Start() {
        // Pause_UIインスタンス取得
        var pauseUI = FindAnyObjectByType<Pause_UI>();
        if (pauseUI != null) {
            _pauseUIInstance = pauseUI;
        }
    }

    private void OnEnable() {
        _inputActions.Player.Enable();
        _inputActions.Player.Move.performed += _OnMove;
        _inputActions.Player.Move.canceled += _OnMove;

        foreach (var action in _abilitySlotActions) {
            action.performed += _OnAbilityPerformed;
            action.canceled += _OnAbilityCanceled;
        }

        // Pauseアクション購読
        _inputActions.Player.Pause.performed += OnPause;
    }

    private void OnDisable() {
        _inputActions.Player.Move.performed -= _OnMove;
        _inputActions.Player.Move.canceled -= _OnMove;

        foreach (var action in _abilitySlotActions) {
            action.performed -= _OnAbilityPerformed;
            action.canceled -= _OnAbilityCanceled;
        }

        // Pauseアクション解除
        _inputActions.Player.Pause.performed -= OnPause;
        _inputActions.Player.Disable();
    }

    // Pauseアクションのコールバック
    private void OnPause(InputAction.CallbackContext context) {
        var pauseUI = GetPauseUI();
        if (pauseUI != null) {
            pauseUI.UIViewSwitch();
        }
    }

    private bool _wasPauseOpen = false;

    // Update is called once per frame
    void Update() {
        // ポーズ画面を開いている間はキャラクター操作入力を無効化
        if (Pause_UI.IsOpen) {
            _wasPauseOpen = true;
            // Pause_UIの上下入力イベントを発火
            var pauseUI = GetPauseUI();
            if (pauseUI != null)
            {
                if (_pauseMenuInputCooldown > 0f) _pauseMenuInputCooldown -= Time.unscaledDeltaTime;
                if (_pauseDecideInputCooldown > 0f) _pauseDecideInputCooldown -= Time.unscaledDeltaTime;

                float y = _moveInputValue.y;
                float x = _moveInputValue.x;
                // 入力無しの場合はクールタイムリセット
                if (Mathf.Abs(y) < 0.5f && Mathf.Abs(x) < 0.5f) {
                    _pauseMenuInputCooldown = 0f;
                }

                int dir = 0;
                if (y > 0.5f) dir = 1; // 上
                else if (y < -0.5f) dir = -1; // 下
                if (dir != 0 && _pauseMenuInputCooldown <= 0f)
                {
                    // メニュー内移動入力
                    pauseUI.InputVerticalDir(dir);
                    _pauseMenuInputCooldown = PAUSE_MENU_INPUT_COOLDOWN;
                    input.move.y = 0; // 入力を1フレームでリセット
                }

                dir = 0;
                if (x > 0.5f) dir = 1; // 右
                else if (x < -0.5f) dir = -1; // 左
                if (dir != 0 && _pauseMenuInputCooldown <= 0f)
                {
                    // パネル切り替え入力
                    pauseUI.InputHorizonDir(dir);
                    _pauseMenuInputCooldown = PAUSE_MENU_INPUT_COOLDOWN;
                    input.move.x = 0; // 入力を1フレームでリセット
                }

                if (_isMessageNextPressed && _pauseDecideInputCooldown <= 0f)
                {
                    // メニュー決定入力
                    pauseUI.InputDecide();
                    _pauseDecideInputCooldown = PAUSE_DECIDE_INPUT_COOLDOWN;
                }
            }
            // ポーズ画面中は_isMessageNextPressedを必ずfalseにリセット
            _isMessageNextPressed = false;
            input.Clear();
            character.UpdateControl(input);
            return;
        }

        // 入力取得
        Vector2 move_input = Vector2.zero;
        if(_moveInputValue.x > 0.5f) move_input.x = 1f;
        if(_moveInputValue.x < -0.5f) move_input.x = -1f;
        if (_moveInputValue.y > 0.5f) move_input.y = 1f;
        if (_moveInputValue.y < -0.5f) move_input.y = -1f;
        input.move = move_input;

        for (int i = 0; i < _abilityButtons.Length; i++) {
            input.SetAbilityButton((eAbilitySlot)i, _abilityButtons[i]);
        }
        input.isJumpPressed = false;
        input.isJumpReleased = false;
        virtualInput = input;

        if (_wasPauseOpen) {
            // ポーズ画面を閉じた直後は入力をリセット
            _wasPauseOpen = false;
            input.abilityB.pressed = false;
        }

        // 特定入力のチェック
        if (_inputCompletedCallback != null) {
            GetSpecificInput(input, _specificInput);
        }

        // キャラクター操作入力が無効な場合、入力をクリア
        if (!isEnabledCharacterInput) {
            input.Clear();
            _ProcessInspectorInputs();
        }

        // メッセージ送り入力
        input.messageNextPressed = _isMessageNextPressed;
        _isMessageNextPressed = false;

        character.IsEventInvincible = !isEnabledCharacterInput;
        character.UpdateControl(input);

        for (int i = 0; i < _abilityButtons.Length; i++) {
            _abilityButtons[i].pressed = false;
            _abilityButtons[i].released = false;
        }
    }

    private void _OnMove(InputAction.CallbackContext context) {
        _moveInputValue = context.ReadValue<Vector2>();
    }

    private void _OnAbilityPerformed(InputAction.CallbackContext context) {
        TriggerAbilityInput(_GetSlotForAction(context.action));
    }

    private void _OnAbilityCanceled(InputAction.CallbackContext context) {
        ReleaseAbilityInput(_GetSlotForAction(context.action));
    }

    /// <summary>
    /// 物理ボタン(InputAction)からスロットを逆引き
    /// </summary>
    private eAbilitySlot _GetSlotForAction(InputAction action) {
        for (int i = 0; i < _abilitySlotActions.Length; i++) {
            if (_abilitySlotActions[i] == action) {
                return (eAbilitySlot)i;
            }
        }
        return default;
    }

    /// <summary>
    /// 指定スロットの能力ボタンが押された扱いにする（チュートリアル等からの疑似入力にも使用）
    /// </summary>
    public void TriggerAbilityInput(eAbilitySlot slot) {
        if (!_wasMessageNextPressed) {
            _isMessageNextPressed = true;
            _wasMessageNextPressed = true; // ここでtrueに設定
        }
        _abilityButtons[(int)slot].pressed = true;
        _abilityButtons[(int)slot].held = true;

        // UIフラッシュ演出
        _FlashAbilityUI(slot);
    }

    /// <summary>
    /// 指定スロットの能力ボタンが離された扱いにする
    /// </summary>
    public void ReleaseAbilityInput(eAbilitySlot slot) {
        _isMessageNextPressed = false;
        _abilityButtons[(int)slot].pressed = false;
        _abilityButtons[(int)slot].held = false;
        _abilityButtons[(int)slot].released = true;
        _wasMessageNextPressed = false; // Release時にfalseに戻す
    }

    /// <summary>
    /// 能力UIを光らせる
    /// </summary>
    private void _FlashAbilityUI(eAbilitySlot slot) {
        // ポーズ中やキャラクター操作無効時は演出をスキップ
        if (Pause_UI.IsOpen || !isEnabledCharacterInput) {
            return;
        }

        var abilityUIManager = FindAnyObjectByType<AbilityUIManager>();
        if (abilityUIManager != null) {
            abilityUIManager.FlashAbilityUI(slot);
        }
    }

    bool _insertMoveMode = false;
    /// <summary>
    /// Insert用入力の処理
    /// </summary>
    private void _ProcessInspectorInputs() {
        // 移動入力
        Vector2 insertMove = Vector2.zero;
        if (insertMoveLeft) insertMove.x = -1f;
        if (insertMoveRight) insertMove.x = 1f;
        if (insertMoveUp) insertMove.y = 1f;
        if (insertMoveDown) insertMove.y = -1f;

        if (insertMove.magnitude > 0.5f || _insertMoveMode) {
            _moveInputValue = insertMove;
            input.move = _moveInputValue;
        }
        _insertMoveMode = insertMove.magnitude > 0.5f;

        // ジャンプ入力（Bスロット扱い）
        if (insertJumpHeld && !_abilityButtons[(int)eAbilitySlot.B].held) {
            _abilityButtons[(int)eAbilitySlot.B].pressed = true;
            _abilityButtons[(int)eAbilitySlot.B].held = true;
            input.abilityB.pressed = true;
            input.abilityB.held = true;
        }
        if (!insertJumpHeld && _abilityButtons[(int)eAbilitySlot.B].held) {
            _abilityButtons[(int)eAbilitySlot.B].pressed = false;
            _abilityButtons[(int)eAbilitySlot.B].held = false;
            input.abilityB.pressed = false;
            input.abilityB.held = false;
        }

        // メッセージ送り入力
        if (insertMessageNext) {
            _isMessageNextPressed = true;
            insertMessageNext = false;
        }
    }

    /// <summary>
    /// 特定の入力だけ受け付ける
    /// </summary>
    /// <param name="dir_input"></param>
    /// <param name="jump_input"></param>
    /// <returns></returns>
    public void GetSpecificInput(CharacterInputData input, CharacterInputData specific_input) {
        bool isInputReceived = true;
        // 方向入力のチェック
        if (specific_input.move.magnitude > 0.1f) {
            if (!(Vector2.Dot(input.move, specific_input.move.normalized) > 0.8f)) {
                isInputReceived = false;
            }
        } else {
            isInputReceived = true; // 方向入力が無い場合は常にtrue
        }
        // ジャンプ入力のチェック
        if (specific_input.abilityB.pressed && isInputReceived == true) {
            if (input.abilityB.pressed) {
                isInputReceived = true;
            } else {
                isInputReceived = false;
            }
        }
        // Xボタン入力のチェック
        if (specific_input.abilityX.pressed && isInputReceived == true) {
            if (input.abilityX.pressed) {
                isInputReceived = true;
            } else {
                isInputReceived = false;
            }
        }

        // 入力完了コールバックの呼び出し
        if (isInputReceived && _inputCompletedCallback != null) {
            _inputCompletedCallback.Invoke();
            _inputCompletedCallback = null;
            _specificInput = new CharacterInputData();
        }
    }

    public void SetInput(CharacterInputData input) {
        this.input = input;
    }

    #region ボタンコンフィグ（リバインド）
    /// <summary>
    /// 保存済みのボタン割り当てを読み込んで適用
    /// </summary>
    private void _LoadBindingOverrides() {
        if (PlayerPrefs.HasKey(BINDING_OVERRIDES_PREF_KEY)) {
            string json = PlayerPrefs.GetString(BINDING_OVERRIDES_PREF_KEY);
            _inputActions.LoadBindingOverridesFromJson(json);
        }
    }

    /// <summary>
    /// 現在のボタン割り当てを保存
    /// </summary>
    public void SaveBindingOverrides() {
        string json = _inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BINDING_OVERRIDES_PREF_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 指定スロットのボタンを対話式に再割り当てする
    /// </summary>
    /// <param name="slot">再割り当て対象のスロット</param>
    /// <param name="onComplete">完了時のコールバック</param>
    public void RebindAbilitySlot(eAbilitySlot slot, System.Action onComplete = null) {
        var action = _abilitySlotActions[(int)slot];
        action.Disable();
        action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => {
                operation.Dispose();
                action.Enable();
                SaveBindingOverrides();
                onComplete?.Invoke();
            })
            .OnCancel(operation => {
                operation.Dispose();
                action.Enable();
            })
            .Start();
    }

    /// <summary>
    /// 指定スロットのボタン割り当てをデフォルトに戻す
    /// </summary>
    public void ResetAbilitySlotBinding(eAbilitySlot slot) {
        _abilitySlotActions[(int)slot].RemoveAllBindingOverrides();
        SaveBindingOverrides();
    }
    #endregion

    #region Inspector Control Methods
    /// <summary>
    /// Inspector用：全ての入力をリセット
    /// </summary>
    [ContextMenu("Reset All Inspect Inputs")]
    public void ResetAllInspectInputs() {
        insertMoveLeft = false;
        insertMoveRight = false;
        insertMoveUp = false;
        insertMoveDown = false;
        _insertJumpButtonPressed = false;
        insertJumpHeld = false;
        insertMessageNext = false;
    }

    /// <summary>
    /// 入力状態をリセット
    /// </summary>
    private void _ResetInput() {
        _moveInputValue = Vector2.zero;
        for (int i = 0; i < _abilityButtons.Length; i++) {
            _abilityButtons[i].Clear();
        }
        _isMessageNextPressed = false;
        virtualInput.Clear();
    }

    /// <summary>
    /// Inspector用：ジャンプボタンを1フレームだけ押す
    /// </summary>
    [ContextMenu("Inspect Jump Press")]
    public void InspectJumpPress() {
        _insertJumpButtonPressed = true;
    }

    /// <summary>
    /// Inspector用：メッセージ送りボタンを1フレームだけ押す
    /// </summary>
    [ContextMenu("Inspect Message Next")]
    public void InspectMessageNext() {
        insertMessageNext = true;
    }
    #endregion
}
