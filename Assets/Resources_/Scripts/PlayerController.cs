using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Character_Base character;

    // コントローラー
    private InputActions _inputActions;

    // 方向入力
    private Vector2 _moveInputValue;
    private bool _isJumpPressed = false;
    private bool _isJumpHeld = false;

    private CharacterInputData input;

    private void Awake()
    {
        _inputActions = new InputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Move.performed += _OnMove;
        _inputActions.Player.Move.canceled += _OnMove;
        _inputActions.Player.Jump.performed += OnJump;
        _inputActions.Player.Jump.canceled += OnJumpRelease;    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= _OnMove;
        _inputActions.Player.Move.canceled -= _OnMove;
        _inputActions.Player.Jump.performed -= OnJump;
        _inputActions.Player.Jump.canceled -= OnJumpRelease;
        _inputActions.Player.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        // 入力取得
        input.move = _moveInputValue;
        input.jumpPressed = _isJumpPressed;
        input.jumpHeld = _isJumpHeld;

        character.UpdateMotor(input);
        character.Warp(input);

        _isJumpPressed = false;
    }

    private void _OnMove(InputAction.CallbackContext context)
    {
        _moveInputValue = context.ReadValue<Vector2>();
        Debug.Log(_moveInputValue);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = true;
        _isJumpHeld = true;
    }

    private void OnJumpRelease(InputAction.CallbackContext context)
    {
        _isJumpPressed = false;
        _isJumpHeld = false;
    }
}
