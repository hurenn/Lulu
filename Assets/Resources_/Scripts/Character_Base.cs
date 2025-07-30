using UnityEngine;

public class Character_Base : MonoBehaviour
{
    [SerializeField] private CommonParameter _param;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;

    [SerializeField] private Rigidbody2D _rb;
    private bool _isGrounded;
    private bool _isJumping;

    private float _currentJumpTime = 0;

    private void Start()
    {
        _rb.gravityScale = 0;
    }

    private void FixedUpdate()
    {
        _ApplyGravity();
    }

    private void _ApplyGravity()
    {
        Vector2 velocity = _rb.linearVelocity;

        if (!_isGrounded)
        {
            float gravity_effect = _param.gravity;
            if(velocity.y < 0)
            {
                gravity_effect *= _param.fallMultiplier;
            }

            velocity.y += gravity_effect * Time.fixedDeltaTime;
            
            if(velocity.y < _param.maxFallSpeed)
            {
                velocity.y = _param.maxFallSpeed;
            }

            _rb.linearVelocity = velocity;
        }
    }

    public void UpdateMotor(CharacterInputData input)
    {
        _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, 0.1f, _groundLayer);

        // 横移動
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = input.move.x * _param.moveSpeed;

        // ジャンプ
        if(input.jumpPressed && _isGrounded)
        {
            velocity.y = _param.jumpForce;
            _currentJumpTime = _param.maxJumpHoldTime;
            _isJumping = true;
        }

        // ジャンプリリース
        if ((!input.jumpHeld && _isJumping) || _currentJumpTime <= 0)
        {
            _isJumping = false;
        }

        // 長押しジャンプ
        if (input.jumpHeld && _isJumping)
        {
            velocity.y = _param.jumpForce;
            _currentJumpTime -= Time.deltaTime;
        }

        _rb.linearVelocity = velocity;
    }

}
