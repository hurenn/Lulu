using UnityEngine;

public class Character_Base : MonoBehaviour
{
    // 歩行中
    private bool _isWalking;
    // ダッシュ中
    private bool _isDashing;

    [SerializeField] private CommonParameter _param;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;

    [SerializeField] private Rigidbody2D _rb;
    private bool _isGrounded;
    private bool _isJumping;

    // 現在のジャンプ時間
    private float _currentJumpTime = 0;

    // ダッシュ入力猶予
    private readonly float _dashThreshold = 0.2f;
    // 移動入力を止めてから経過した時間
    private float _currentStopMoveInputTime = 0;
    // 直前まで進んでいた方向
    private Vector2 _lastWalkDirection = Vector2.zero;

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
        // 移動入力
        if (input.move.x != 0)
        {
            // 直前まで入力なし
            if (!_isWalking)
            {
                // 同じ方向にすぐ再入力でダッシュ
                if (_currentStopMoveInputTime < _dashThreshold && (
                    (Mathf.Sign(input.move.x) == Mathf.Sign(_lastWalkDirection.x) && !_isDashing) ||
                    (Mathf.Sign(input.move.x) != Mathf.Sign(_lastWalkDirection.x) && _isDashing)))
                {
                    _isDashing = true;
                }
                else
                {
                    _isDashing = false;
                }
                _isWalking = true;
            }

            // 移動中は常にフラグリセット
            _lastWalkDirection = input.move;
            _currentStopMoveInputTime = 0;
        }
        else // 入力停止
        {
            if (_isWalking)
            {
                // 歩行から停止
                _isWalking = false;
                _currentStopMoveInputTime = 0;
            }
            else
            {
                // 停止中はタイマー更新
                _currentStopMoveInputTime += Time.deltaTime;
                if (_currentStopMoveInputTime > _dashThreshold)
                {
                    _isDashing = false;
                }
            }
        }

        _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, 0.1f, _groundLayer);

        // 横移動
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = input.move.x * (_isDashing ? _param.dashSpeed : _param.moveSpeed);

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
