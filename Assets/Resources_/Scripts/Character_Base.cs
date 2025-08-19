using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
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
    private const float _checkerBuffer = 0.05f;

    [SerializeField] private Collider2D _col;
    [SerializeField] private Rigidbody2D _rb;

    // キャラクター状態フラグ
    private bool _isWalking;
    private bool _isDashing;
    private bool _isWarpDelay;
    private bool _isWarpDashing;
    private bool _isSliding;      // スライディング中かどうか
    private bool _isSlideJumping; // スライディング中にジャンプしたかどうか
    private bool _isGroundSticking; // 地面に張り付いている状態
    private bool _isWallSliding;  // 壁に沿って滑っている状態
    private bool _isGrounded;
    private bool _isJumping;
    private bool _isTouchingLeft;
    private bool _isTouchingRight;

    // 通常移動可能かどうか
    private bool _CanMove => !_isWarpDashing && !_isSliding &&
        !_isSlideJumping && !_isGroundSticking && !_isWallSliding;
    // 重力を適用するかどうか
    private bool _EnableGravity => !_isWarpDashing && !_isWallSliding && !_isWarpDelay;

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
    private float _currentWarpDashTime = 0;
    // ワープダッシュの方向
    private Vector2 _warpDashDirection = Vector2.zero;
    // ワープダッシュの速度
    [SerializeField]
    private Vector2 _warpDashSpeed = new Vector2(20f, 5f);

    // スライディング時間計測
    private float _currentSlideTime = 0;
    // スライディングの最大時間
    [SerializeField]
    private float _maxSlideTime = 1f;

    // 壁に沿って滑る速度
    [SerializeField]
    private float _wallSlideSpeed = 2.0f;
    private float _currentWallSlideTime = 0;

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
        _UpdateWarpDash();
        _UpdateSliding();
        _UpdateWallSlideMove();

        if (_currentWarpCoolTime > 0)
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
        if (!_EnableGravity)
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

    /// <summary>
    /// ワープダッシュの更新処理
    /// </summary>
    private void _UpdateWarpDash()
    {
        if (!_isWarpDashing)
        {
            return; // ワープダッシュ中でない場合は何もしない
        }

        // ワープダッシュの最大時間を超えた場合は終了
        if (_currentWarpDashTime > _maxWarpDashTime)
        {
            _isWarpDashing = false; // ワープダッシュ終了
            return; // ワープダッシュのクールタイム中は何もしない
        }
        _currentWarpDashTime += Time.deltaTime;

        // ワープダッシュ移動
        var dash_velocity = _warpDashDirection * _warpDashSpeed;
        _rb.linearVelocity = dash_velocity;

        // 地面に接触しているかチェック
        if (_isGrounded)
        {
            if (_warpDashDirection.x != 0)
            {
                // 地面に対して斜めに移動している場合はスライディングを実行
                _ExecuteSlide(); // スライディング実行
            }
            else
            {
                // 地面に対して垂直に移動している場合は張り付き状態に移行
                //_isGroundSticking = true;
            }
            _isWarpDashing = false; // ワープダッシュ終了
            return;
        }
        // 壁に接触しているかチェック
        if ((_isTouchingLeft && _warpDashDirection.x < 0) || (_isTouchingRight && _warpDashDirection.x > 0))
        {
            // 壁に接触している場合は壁に沿って滑る
            _isWallSliding = true;
            _currentWallSlideTime = 0;

            _isWarpDashing = false; // ワープダッシュ終了
            return;
        }
    }

    /// <summary>
    /// 壁滑りの更新処理
    /// </summary>
    private void _UpdateWallSlideMove()
    {
        // 壁滑り中でない場合は何もしない
        if (!_isWallSliding)
        {
            return;
        }

        if (_currentWallSlideTime >= _maxSlideTime)
        {
            _isWallSliding = false; // 壁滑り終了
            return;
        }
        _currentWallSlideTime += Time.deltaTime;

        // 壁に沿って滑る処理
        Vector2 velocity = Vector2.zero;
        if (_warpDashDirection.y < 0)
        {
            // 壁に沿って下方向に滑る
            velocity.y = -_wallSlideSpeed;
        }
        else
        {
            // 壁に沿って上方向に滑る
            velocity.y = _wallSlideSpeed;
        }
        _rb.linearVelocity = velocity;

        // 壁との接触が無くなった場合は壁滑りを終了してジャンプする
        if (!_isTouchingLeft && !_isTouchingRight)
        {
            _isWallSliding = false; // 壁滑り終了
            _isJumping = true;

            velocity.y = _param.jumpForce;
            _rb.linearVelocity = velocity; // ジャンプ力を適用
        }

        // 着地した場合は壁滑りを終了
        if (_isGrounded)
        {
            _isWallSliding = false; // 壁滑り終了
        }
    }

    private void _SlideDashMove()
    {
        // スライドダッシュ中はキャラクターの位置を更新
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = _warpDashDirection.x * _warpDashSpeed.x;
        _rb.linearVelocity = velocity;
    }

    /// <summary>
    /// スライディング実行
    /// </summary>
    private void _ExecuteSlide()
    {
        _isSliding = true;
        _currentSlideTime = 0;
    }

    /// <summary>
    /// スライディング処理
    /// </summary>
    private void _UpdateSliding()
    {
        if (_isSlideJumping && _isGrounded && _currentSlideTime > 0.1f)
        {
            _isSlideJumping = false;
        }

        if (_isSliding || _isSlideJumping)
        {
            _SlideDashMove();

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
        // 壁滑り中の入力
        if (_isWallSliding)
        {
            // 壁と反対方向に移動しようとする入力があれば壁滑りを終了
            if (input.move.x != 0 && Mathf.Sign(input.move.x) != Mathf.Sign(_warpDashDirection.x))
            {
                _isWallSliding = false; // 壁滑り終了
                return;
            }
        }

        if (!_CanMove)
        {
            return;
        }

        // 横移動
        Vector2 velocity = _rb.linearVelocity;

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

        // スライディングジャンプ
        if (_isSliding && _isJumping)
        {
            _isSlideJumping = true;
            _isSliding = false;

            // y方向の加速を無視
            _warpDashDirection.y = 0;

            // スライディング時間リセット
            _currentSlideTime = 0;

            // ジャンプ力を適用
            _rb.linearVelocity = velocity;
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

        velocity.x = input.move.x * (_isDashing ? _param.dashSpeed : _param.moveSpeed);

        // 壁に接触している場合は横移動を0にする
        if ((_isTouchingLeft && input.move.x < 0) || (_isTouchingRight && input.move.x > 0))
        {
            velocity.x = 0;
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
            // スライディングリセット
            _isSliding = false;
            // 重力を無効化
            _isWarpDelay = true;
            // 速度をリセット
            _rb.linearVelocity = Vector2.zero;

            // ワープダッシュの方向を設定
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

            // 一瞬待機
            yield return new WaitForSeconds(_warpWaitTime);

            // ワープ実行
            _warpControl.Warp(direction);

            yield return null;

            // ワープダッシュ実行
            _isWarpDashing = true;
            _isWarpDelay = false;
            _currentWarpDashTime = 0;

            // ワープダッシュのクールタイムをリセット
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
    private void OnDrawGizmos()
    {
        // 地面チェック位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + _groundCheckLocalPos, _groundCheckScale);

        // 壁チェック位置・サイズ
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + _wallCheckLeftLocalPos, _wallCheckScale);
        Gizmos.DrawWireCube(transform.position + _wallCheckRightLocalPos, _wallCheckScale);
    }
    #endregion
}