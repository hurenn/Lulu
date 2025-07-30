using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Character_Base character;

    // コントローラー
    private Controls _inputActions;

    // 方向入力
    private Vector2 _moveInputValue;
    private bool _isJumpPressed = false;
    private bool _isJumpHeld = false;

    private CharacterInputData input;

    private void Awake()
    {
        _inputActions = new Controls();
    }

    private void OnEnable()
    {
        _inputActions.Character.Enable();
        _inputActions.Character.Move.started += _OnMove;
        _inputActions.Character.Move.canceled += _OnMove;
        _inputActions.Character.Jump.performed += OnJump;
        _inputActions.Character.Jump.canceled += OnJumpRelease;    }

    private void OnDisable()
    {
        _inputActions.Character.Move.performed -= _OnMove;
        _inputActions.Character.Move.canceled -= _OnMove;
        _inputActions.Character.Jump.performed -= OnJump;
        _inputActions.Character.Jump.canceled -= OnJumpRelease;
        _inputActions.Character.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        // 入力取得
        input.move = _moveInputValue;
        input.jumpPressed = _isJumpPressed;
        input.jumpHeld = _isJumpHeld;

        character.UpdateMotor(input);

        _isJumpPressed = false;
    }

    private void _OnMove(InputAction.CallbackContext context)
    {
        _moveInputValue = context.ReadValue<Vector2>();
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
