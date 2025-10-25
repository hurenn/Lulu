using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    [SerializeField] private Character_Base character;
    public Character_Base Character => character;

    // コントローラー
    private InputActions _inputActions;

    // 方向入力
    private Vector2 _moveInputValue;
    private bool _isJumpPressed = false;
    private bool _isJumpHeld = false;

    // Abilityボタンの状態
    private bool _isAbilityYPressed = false;
    private bool _isAbilityYHeld = false;
    private bool _isAbilityXPressed = false;
    private bool _isAbilityXHeld = false;
    private bool _isAbilityAPressed = false;
    private bool _isAbilityAHeld = false;

    // メッセージ送り入力
    private bool _isMessageNextPressed = false;

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
        _ResetInput();
        _specificInput = specific_input;
        _inputCompletedCallback = input_completed;
    }

    private void Awake() {
        _inputActions = new InputActions();
    }

    private void OnEnable() {
        _inputActions.Player.Enable();
        _inputActions.Player.Move.performed += _OnMove;
        _inputActions.Player.Move.canceled += _OnMove;
        _inputActions.Player.Jump.performed += OnJump;
        _inputActions.Player.Jump.canceled += OnJumpRelease;

        _inputActions.Player.AbilityY.performed += OnAbilityY;
        _inputActions.Player.AbilityY.canceled += OnAbilityYRelease;
        _inputActions.Player.AbilityX.performed += OnAbilityX;
        _inputActions.Player.AbilityX.canceled += OnAbilityXRelease;
        _inputActions.Player.AbilityA.performed += OnAbilityA;
        _inputActions.Player.AbilityA.canceled += OnAbilityARelease;
    }

    private void OnDisable() {
        _inputActions.Player.Move.performed -= _OnMove;
        _inputActions.Player.Move.canceled -= _OnMove;
        _inputActions.Player.Jump.performed -= OnJump;
        _inputActions.Player.Jump.canceled -= OnJumpRelease;
        _inputActions.Player.AbilityY.performed -= OnAbilityY;
        _inputActions.Player.AbilityY.canceled -= OnAbilityYRelease;
        _inputActions.Player.AbilityX.performed -= OnAbilityX;
        _inputActions.Player.AbilityX.canceled -= OnAbilityXRelease;
        _inputActions.Player.AbilityA.performed -= OnAbilityA;
        _inputActions.Player.AbilityA.canceled -= OnAbilityARelease;
        _inputActions.Player.Disable();
    }

    // Update is called once per frame
    void Update() {
        // 入力取得
        input.move = _moveInputValue;
        input.jumpPressed = _isJumpPressed;
        input.jumpHeld = _isJumpHeld;
        input.abilityYPressed = _isAbilityYPressed;
        input.abilityYHeld = _isAbilityYHeld;
        input.abilityXPressed = _isAbilityXPressed;
        input.abilityXHeld = _isAbilityXHeld;
        input.abilityAPressed = _isAbilityAPressed;
        input.abilityAHeld = _isAbilityAHeld;
        virtualInput = input;

        // 特定入力のチェック
        if (_inputCompletedCallback != null) {
            GetSpecificInput(input, _specificInput);
        }

        // キャラクター操作入力が無効な場合、入力をクリア
        if (!isEnabledCharacterInput) {
            input.Clear();

            // Insert用入力の処理
            _ProcessInspectorInputs();
        }

        // メッセージ送り入力
        input.messageNextPressed = _isMessageNextPressed;
        _isMessageNextPressed = false;

        character.UpdateControl(input);

        _isJumpPressed = false;
        _isAbilityYPressed = false;
        _isAbilityXPressed = false;
        _isAbilityAPressed = false;
    }

    private void _OnMove(InputAction.CallbackContext context) {
        _moveInputValue = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context) {
        _isMessageNextPressed = true;
        _isJumpPressed = true;
        _isJumpHeld = true;
    }

    private void OnJumpRelease(InputAction.CallbackContext context) {
        _isMessageNextPressed = false;
        _isJumpPressed = false;
        _isJumpHeld = false;
    }

    private void OnAbilityY(InputAction.CallbackContext context) {
        _isMessageNextPressed = true;
        _isAbilityYPressed = true;
        _isAbilityYHeld = true;
    }

    private void OnAbilityYRelease(InputAction.CallbackContext context) {
        _isMessageNextPressed = false;
        _isAbilityYPressed = false;
        _isAbilityYHeld = false;
    }

    private void OnAbilityX(InputAction.CallbackContext context) {
        _isMessageNextPressed = true;
        _isAbilityXPressed = true;
        _isAbilityXHeld = true;
    }
    private void OnAbilityXRelease(InputAction.CallbackContext context) {
        _isMessageNextPressed = false;
        _isAbilityXPressed = false;
        _isAbilityXHeld = false;
    }

    private void OnAbilityA(InputAction.CallbackContext context) {
        _isMessageNextPressed = true;
        _isAbilityAPressed = true;
        _isAbilityAHeld = true;
    }
    private void OnAbilityARelease(InputAction.CallbackContext context) {
        _isMessageNextPressed = false;
        _isAbilityAPressed = false;
        _isAbilityAHeld = false;
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

        // ジャンプ入力
        if (insertJumpHeld && !_isJumpHeld) {
            _isJumpPressed = true;
            _isJumpHeld = true;
            input.jumpPressed = _isJumpPressed;
            input.jumpHeld = _isJumpHeld;
        }
        if (!insertJumpHeld && _isJumpHeld) {
            _isJumpPressed = false;
            _isJumpHeld = false;
            input.jumpPressed = _isJumpPressed;
            input.jumpHeld = _isJumpHeld;
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
        if (specific_input.jumpPressed && isInputReceived == true) {
            if (input.jumpPressed) {
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
        _isJumpPressed = false;
        _isJumpHeld = false;
        _isAbilityYPressed = false;
        _isAbilityYHeld = false;
        _isAbilityXPressed = false;
        _isAbilityXHeld = false;
        _isAbilityAPressed = false;
        _isAbilityAHeld = false;
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
