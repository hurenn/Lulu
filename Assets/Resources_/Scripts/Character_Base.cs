using System.Collections;
using UnityEngine;

public class Character_Base : MonoBehaviour
{
    // 共通パラメータ
    [SerializeField] private CommonParameter _param;

    // 地面チェッカー
    [SerializeField] private LayerMask _groundLayer;
    // 壁チェッカー
    [SerializeField] private LayerMask _wallLayer;
    // 障害物チェッカー
    [SerializeField] private LayerMask _obstacleLayer;

    // チェッカーパラメータ
    private Vector3 _groundCheckLocalPos = default;
    private Vector3 _groundCheckScale = default;
    private Vector3 _wallCheckLeftLocalPos = default;
    private Vector3 _wallCheckRightLocalPos = default;
    private Vector3 _wallCheckScale = default;
    private const float _groundCheckHeight = 0.05f;
    private const float _wallCheckWidth = 0.1f;
    private const float _checkerBuffer = 0.1f;

    [SerializeField] private Collider2D _col;
    [SerializeField] private Rigidbody2D _rb;

    // キャラクター状態フラグ
    private bool _isWalking;
    private bool _isDashing;
    private bool _isWarpDashing;
    private bool _isSliding;
    private bool _isGrounded;
    private bool _isJumping;
    private bool _isTouchingLeft;
    private bool _isTouchingRight;

    // ワープ管理
    [SerializeField] private WarpControl _warpControl;

    // 現在のジャンプ時間
    private float _currentJumpTime = 0;

    // ダッシュ入力猶予
    private readonly float _dashThreshold = 0.2f;
    // 移動入力を止めてから経過した時間
    private float _currentStopMoveInputTime = 0;
    // 直前まで進んでいた方向
    private Vector2 _lastWalkDirection = Vector2.zero;

    // ワープ待機時間
    [SerializeField]
    private float _warpWaitTime = 0.1f;
    // ワープのクールタイム
    [SerializeField]
    private float _maxWarpCoolTime = 0.1f;
    private float _currentWarpCoolTime = 0;

    // ワープダッシュの最大時間
    [SerializeField]
    private float _maxWarpDashTime = 0.5f;
    // ワープダッシュの方向
    private Vector2 _warpDashDirection = Vector2.zero;
    // ワープダッシュの速度
    [SerializeField]
    private Vector2 _warpDashSpeed = new Vector2(20f, 6f);

    // スライディング時間計測
    private float _currentSlideTime = 0;
    // スライディングの最大時間
    private const float _maxSlideTime = 0.5f;
    // スライディング方向
    private WarpControl.eWarpDirection _slideDirection = WarpControl.eWarpDirection.Up;

    private void Start()
    {
        _rb.gravityScale = 0;

        // 地面チェックの初期化
        _groundCheckLocalPos = Vector3.up * (-GetCharacterSize().y / 2 - _groundCheckHeight);
        _groundCheckScale = new Vector3(GetCharacterSize().x - _checkerBuffer, _groundCheckHeight, 1);

        // 壁チェックの初期化
        var chara_size = GetCharacterSize();
        _wallCheckLeftLocalPos = Vector3.right * (-chara_size.x / 2 - _wallCheckWidth);
        _wallCheckRightLocalPos = Vector3.right * (chara_size.x / 2 + _wallCheckWidth);
        _wallCheckScale = new Vector3(_wallCheckWidth, chara_size.y - _checkerBuffer, 1);

        _warpControl.Setup(chara_size, _obstacleLayer);
    }

    private void FixedUpdate()
    {
        _CheckTerrain();
        _ApplyGravity();

        if(_currentWarpCoolTime > 0)
        {
            _currentWarpCoolTime -= Time.fixedDeltaTime;
        }
    }

    private void _CheckTerrain()
    {
        _isTouchingLeft = Physics2D.OverlapBox(transform.position + _wallCheckLeftLocalPos, _wallCheckScale, 0, _wallLayer);
        _isTouchingRight = Physics2D.OverlapBox(transform.position + _wallCheckRightLocalPos, _wallCheckScale, 0, _wallLayer);

        _isGrounded = Physics2D.OverlapBox(transform.position + _groundCheckLocalPos, _groundCheckScale, 0, _groundLayer);
    }

    private void _ApplyGravity()
    {
        if (_isWarpDashing)
        {
            return; // ワープダッシュ中は重力を適用しない
        }

        Vector2 velocity = _rb.linearVelocity;

        if (!_isGrounded)
        {
            float gravity_effect = _param.gravity;
            if (velocity.y < 0)
            {
                gravity_effect *= _param.fallMultiplier;
            }

            velocity.y += gravity_effect * Time.fixedDeltaTime;

            if (velocity.y < _param.maxFallSpeed)
            {
                velocity.y = _param.maxFallSpeed;
            }
            if (velocity.y > _param.maxJumpSpeed)
            {
                velocity.y = _param.maxJumpSpeed;
            }

            _rb.linearVelocity = velocity;
        }
    }

    private void _SetupWarpDash(WarpControl.eWarpDirection direction)
    {
        _warpDashDirection = direction switch
        {
            WarpControl.eWarpDirection.Up => Vector2.up,
            WarpControl.eWarpDirection.UpRight => new Vector2(1, 1).normalized,
            WarpControl.eWarpDirection.Right => Vector2.right,
            WarpControl.eWarpDirection.DownRight => new Vector2(1, -1).normalized,
            WarpControl.eWarpDirection.Down => Vector2.down,
            WarpControl.eWarpDirection.DownLeft => new Vector2(-1, -1).normalized,
            WarpControl.eWarpDirection.Left => Vector2.left,
            WarpControl.eWarpDirection.UpLeft => new Vector2(-1, 1).normalized,
            _ => Vector2.zero
        };
        _isWarpDashing = true;
        _rb.linearVelocity = Vector2.zero; // ワープダッシュ開始時に速度をリセット
    }

    /// <summary>
    /// ワープダッシュの更新処理
    /// </summary>
    private IEnumerator _UpdateWarpDash()
    {
        float currentWarpDashTime = 0;
        while (currentWarpDashTime < _maxWarpDashTime)
        {
            currentWarpDashTime += Time.deltaTime;

            // ワープダッシュ中はキャラクターの位置を更新
            Vector2 velocity = _rb.linearVelocity;
            velocity = _warpDashDirection * _warpDashSpeed;
            _rb.linearVelocity = velocity;

            yield return null; // 次のフレームまで待機
        }

        _isWarpDashing = false; // ワープダッシュ終了
    }

    /// <summary>
    /// スライディング処理
    /// </summary>
    private void _UpdateSliding()
    {
        if (_isSliding)
        {
            _currentSlideTime += Time.deltaTime;
            if (_currentSlideTime >= _maxSlideTime)
            {
                _isSliding = false; // スライディング終了
                _currentSlideTime = 0;
            }
        }
    }

    public void UpdateMotor(CharacterInputData input)
    {
        if (_isWarpDashing)
        {
            return;
        }

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

        // 横移動
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = input.move.x * (_isDashing ? _param.dashSpeed : _param.moveSpeed);

        // 壁に接触している場合は横移動を0にする
        if ((_isTouchingLeft && input.move.x < 0) || (_isTouchingRight && input.move.x > 0))
        {
            velocity.x = 0;
        }

        // ジャンプ
        if (input.jumpPressed && _isGrounded)
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

    public void Warp(CharacterInputData input)
    {
        if (_warpControl == null || _currentWarpCoolTime > 0)
        {
            return;
        }

        // ワープ入力
        if (input.move.magnitude != 0 && !_isGrounded && input.jumpPressed)
        {
            WarpControl.eWarpDirection direction = WarpControl.eWarpDirection.Up;

            direction = input.move switch
            {
                { x: > 0, y: > 0 } => WarpControl.eWarpDirection.UpRight,
                { x: > 0, y: < 0 } => WarpControl.eWarpDirection.DownRight,
                { x: < 0, y: > 0 } => WarpControl.eWarpDirection.UpLeft,
                { x: < 0, y: < 0 } => WarpControl.eWarpDirection.DownLeft,
                { x: 0, y: > 0 } => WarpControl.eWarpDirection.Up,
                { x: 0, y: < 0 } => WarpControl.eWarpDirection.Down,
                { x: > 0, y: 0 } => WarpControl.eWarpDirection.Right,
                { x: < 0, y: 0 } => WarpControl.eWarpDirection.Left,
                _ => direction
            };
            StartCoroutine(WarpCoroutine(direction));
        }

        IEnumerator WarpCoroutine(WarpControl.eWarpDirection direction)
        {
            // ワープダッシュ実行準備
            _SetupWarpDash(direction);

            // 一瞬待機
            yield return new WaitForSeconds(_warpWaitTime);

            // ワープ実行
            _warpControl.Warp(direction);

            // ワープダッシュ実行
            StartCoroutine(_UpdateWarpDash());

            _currentWarpCoolTime = _maxWarpCoolTime;
        }
    }

    /// <summary>
    /// キャラクターサイズを取得
    /// </summary>
    public Vector2 GetCharacterSize()
    {
        if (_col != null)
        {
            return _col.bounds.size; // キャラクターのコライダーサイズを返す
        }
        return new Vector2(0.5f, 1f); // デフォルトのキャラクターサイズ
    }

    #region デバッグ用
    //private void OnDrawGizmos()
    //{
    //    // 地面チェック位置
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireCube(transform.position + _groundCheckLocalPos, _groundCheckScale);

    //    // 壁チェック位置・サイズ
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireCube(transform.position + _wallCheckLeftLocalPos, _wallCheckScale);
    //    Gizmos.DrawWireCube(transform.position + _wallCheckRightLocalPos, _wallCheckScale);
    //}
    #endregion
}