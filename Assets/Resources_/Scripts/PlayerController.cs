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
    [SerializeField] private bool _inspectMoveLeft = false;
    [SerializeField] private bool _inspectMoveRight = false;
    [SerializeField] private bool _inspectMoveUp = false;
    [SerializeField] private bool _inspectMoveDown = false;
    [Space]
    private bool _inspectJumpPressed = false;
    [SerializeField] private bool _inspectJumpHeld = false;
    private bool _inspectJumpReleased = true;
    [Space]
    [SerializeField] private bool _inspectMessageNext = false;

    private CharacterInputData input;
    public CharacterInputData Input => input;

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

        if (!isEnabledCharacterInput) {
            input.Clear();
        }
        // Inspector用入力の処理
        _ProcessInspectorInputs();

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
        _inspectJumpHeld = true;
    }

    private void OnJumpRelease(InputAction.CallbackContext context) {
        _isMessageNextPressed = false;
        _isJumpPressed = false;
        _isJumpHeld = false;
        _inspectJumpHeld = false;
    }

    private void OnAbilityY(InputAction.CallbackContext context) {
        _isAbilityYPressed = true;
        _isAbilityYHeld = true;
    }

    private void OnAbilityYRelease(InputAction.CallbackContext context) {
        _isAbilityYPressed = false;
        _isAbilityYHeld = false;
    }

    private void OnAbilityX(InputAction.CallbackContext context) {
        _isAbilityXPressed = true;
        _isAbilityXHeld = true;
    }
    private void OnAbilityXRelease(InputAction.CallbackContext context) {
        _isAbilityXPressed = false;
        _isAbilityXHeld = false;
    }

    private void OnAbilityA(InputAction.CallbackContext context) {
        _isAbilityAPressed = true;
        _isAbilityAHeld = true;
    }
    private void OnAbilityARelease(InputAction.CallbackContext context) {
        _isAbilityAPressed = false;
        _isAbilityAHeld = false;
    }

    bool _inspectMoveMode = false;
    /// <summary>
    /// Inspector用入力の処理
    /// </summary>
    private void _ProcessInspectorInputs() {
        // 移動入力
        Vector2 inspectMove = Vector2.zero;
        if (_inspectMoveLeft) inspectMove.x = -1f;
        if (_inspectMoveRight) inspectMove.x = 1f;
        if (_inspectMoveUp) inspectMove.y = 1f;
        if (_inspectMoveDown) inspectMove.y = -1f;

        if (inspectMove.magnitude > 0.5f || _inspectMoveMode) {
            _moveInputValue = inspectMove;
            input.move = _moveInputValue;
        }
        _inspectMoveMode = inspectMove.magnitude > 0.5f;

        // ジャンプ入力
        if (_inspectJumpHeld && !_isJumpHeld) {
            _isJumpPressed = true;
            _isJumpHeld = true;
            input.jumpPressed = _isJumpPressed;
            input.jumpHeld = _isJumpHeld;
        }
        if (!_inspectJumpHeld && _isJumpHeld) {
            _isJumpPressed = false;
            _isJumpHeld = false;
            input.jumpPressed = _isJumpPressed;
            input.jumpHeld = _isJumpHeld;
        }

        // メッセージ送り入力
        if (_inspectMessageNext) {
            _isMessageNextPressed = true;
            _inspectMessageNext = false;
        }
    }

    #region Inspector Control Methods
    /// <summary>
    /// Inspector用：全ての入力をリセット
    /// </summary>
    [ContextMenu("Reset All Inspect Inputs")]
    public void ResetAllInspectInputs() {
        _inspectMoveLeft = false;
        _inspectMoveRight = false;
        _inspectMoveUp = false;
        _inspectMoveDown = false;
        _inspectJumpPressed = false;
        _inspectJumpHeld = false;
        _inspectMessageNext = false;

        // 実際の入力状態もリセット
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
    }

    /// <summary>
    /// Inspector用：ジャンプボタンを1フレームだけ押す
    /// </summary>
    [ContextMenu("Inspect Jump Press")]
    public void InspectJumpPress() {
        _inspectJumpPressed = true;
    }

    /// <summary>
    /// Inspector用：メッセージ送りボタンを1フレームだけ押す
    /// </summary>
    [ContextMenu("Inspect Message Next")]
    public void InspectMessageNext() {
        _inspectMessageNext = true;
    }
    #endregion
}
