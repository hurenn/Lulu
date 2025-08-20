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

    [SerializeField] private Collider2D _col;
    [SerializeField] private Rigidbody2D _rb;

    // ワープ管理
    [SerializeField] private WarpControl _warpControl;

    // チェッカーパラメータ
    private Vector3 _groundCheckLocalPos = default;
    private Vector3 _groundCheckScale = default;
    private Vector3 _wallCheckLeftLocalPos = default;
    private Vector3 _wallCheckRightLocalPos = default;
    private Vector3 _wallCheckScale = default;

    // キャラクター状態フラグ
    private bool _isWalking;
    private bool _isDashing;
    private bool _isWarpDelay;
    private bool _isWarpDashing;
    private bool _isSliding;      // スライディング中かどうか
    private bool _isSlidingCanceling; // スライディングキャンセル中かどうか
    private bool _isGroundSticking; // 地面に張り付いている状態
    private bool _isWallSliding;  // 壁に沿って滑っている状態
    private bool _isGrounded;
    private bool _isJumping;
    private bool _isTouchingLeft;
    private bool _isTouchingRight;

    // 通常移動可能かどうか
    private bool _CanMove => !_isWarpDashing && !_isSlidingCanceling;
    // 重力を適用するかどうか
    private bool _EnableGravity => !_isWarpDashing && !_isWallSliding && !_isWarpDelay;
    // ジャンプ力を取得
    private float _jumpForce => _isDashing ? _param.dashJumpForce :
            _isSliding ? _param.slideJumpForce : _param.jumpForce;

    // 現在のジャンプ時間計測
    private float _currentJumpTime = 0;
    // 移動入力を止めてから経過した時間計測
    private float _currentStopMoveInputTime = 0;
    // 直前まで進んでいた方向
    private Vector2 _lastWalkDirection = Vector2.zero;
    // ワープのクールタイム計測
    private float _currentWarpCoolTime = 0;
    // ワープダッシュ時間計測
    private float _currentWarpDashTime = 0;
    // ワープダッシュの方向
    private Vector2 _warpDashDirection = Vector2.zero;
    // スライディング時間計測
    private float _currentSlideTime = 0;
     // 着地ダッシュ時間計測
    private float _currentLandingDashTime = 0;
    // 壁に沿って滑る速度
    private float _currentWallSlideTime = 0;

    private void Start()
    {
        _rb.gravityScale = 0;

        // 地面チェックの初期化
        _groundCheckLocalPos = Vector3.up * (-GetCharacterSize().y / 2 - _param.groundCheckHeight);
        _groundCheckScale = new Vector3(GetCharacterSize().x - _param.checkerBuffer, _param.groundCheckHeight, 1);

        // 壁チェックの初期化
        var chara_size = GetCharacterSize();
        _wallCheckLeftLocalPos = Vector3.right * (-chara_size.x / 2 - _param.wallCheckWidth);
        _wallCheckRightLocalPos = Vector3.right * (chara_size.x / 2 + _param.wallCheckWidth);
        _wallCheckScale = new Vector3(_param.wallCheckWidth, chara_size.y - _param.checkerBuffer, 1);

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

        // 地面張り付き状態計測
        if(_currentLandingDashTime < _param.maxLandingDashTime && _isGroundSticking) {
            _currentLandingDashTime += Time.fixedDeltaTime;
            if (_currentLandingDashTime >= _param.maxLandingDashTime)
            {
                _isGroundSticking = false; // 張り付き状態を解除
            }
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
        if (_currentWarpDashTime > _param.maxWarpDashTime)
        {
            _isWarpDashing = false; // ワープダッシュ終了
            return; // ワープダッシュのクールタイム中は何もしない
        }
        _currentWarpDashTime += Time.deltaTime;

        // ワープダッシュ移動
        var dash_velocity = _warpDashDirection;
        _rb.linearVelocity = dash_velocity;
        // ワープダッシュ力を減衰させる
        _warpDashDirection *= _param.warpDashDamping;
        if(_warpDashDirection.magnitude < 0.2f)
        {
            _warpDashDirection = Vector2.zero; // ダッシュ力が小さくなったらリセット
        }

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
                _isGroundSticking = true;
                _currentLandingDashTime = 0;
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

        if (_currentWallSlideTime >= _param.maxSlideTime)
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
            velocity.y = -_param.wallSlideSpeed;
        }
        else
        {
            // 壁に沿って上方向に滑る
            velocity.y = _param.wallSlideSpeed;
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

    /// <summary>
    /// スライディング実行
    /// </summary>
    private void _ExecuteSlide()
    {
        _isDashing = true;
        _isSliding = true;
        _currentSlideTime = 0;
    }

    /// <summary>
    /// スライディング処理
    /// </summary>
    private void _UpdateSliding()
    {
        if (_isSliding)
        {
            // スライドダッシュ中はキャラクターの位置を更新
            Vector2 velocity = _rb.linearVelocity;
            velocity.x = _warpDashDirection.x;
            _rb.linearVelocity = velocity.normalized * _param.slideSpeed;

            _currentSlideTime += Time.deltaTime;
            if (_currentSlideTime >= _param.maxSlideTime)
            {
                _isSliding = false; // スライディング終了
                _currentSlideTime = 0;
            }
        } 
        if (_isSlidingCanceling) {
            _isSliding = false;

            Vector2 velocity = _rb.linearVelocity;
            velocity.x *= _param.slideCancelDamping;
            
            // 一定以下になったら完全停止
            if (Mathf.Abs(velocity.x) < 0.1f) {
                velocity.x = 0;
                _isSlidingCanceling = false;
            }
            _rb.linearVelocity = velocity;
        }
    }

    public void UpdateMotor(CharacterInputData input) {

        Vector2 velocity = _rb.linearVelocity;

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

        // スライディング中に逆方向入力でキャンセル
        if (_isSliding && input.move.x != 0 && Mathf.Sign(input.move.x) != Mathf.Sign(_warpDashDirection.x)) {
            _isSlidingCanceling = true; // スライディングキャンセル中フラグを立てる
            _currentSlideTime = 0;
            return;
        }

        // スライディングキャンセル中にジャンプでキャンセル
        if (_isSlidingCanceling && input.jumpPressed) {
            _isSlidingCanceling = false; // スライディングキャンセル終了
        }

        // 地面張り付き状態の入力
        if (_isGroundSticking) {
            if(input.move.x != 0)
            {
                // 張り付き状態で移動入力があれば張り付き状態を解除
                _isGroundSticking = false;
                _warpDashDirection = input.move.x > 0 ? _param.warpDashDownRight : _param.warpDashDownLeft;
                _ExecuteSlide(); // スライディング実行
            }
            else if (input.jumpPressed)
            {
                // 張り付き状態でジャンプ入力があればジャンプ
                _isGroundSticking = false;
            }
        }

        if (!_CanMove)
        {
            return;
        }

        // ジャンプ
        if (input.jumpPressed && _isGrounded) {
            // スライディングジャンプ
            if (_isSliding) {
                _isSliding = false;

                // y方向の加速を無視
                _warpDashDirection.y = 0;

                // スライディング時間リセット
                _currentSlideTime = 0;
            }

            velocity.y = _jumpForce;
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
            velocity.y = _jumpForce;
            _currentJumpTime -= Time.deltaTime;
        }

        // 移動入力
        if (input.move.x != 0)
        {
            // 直前まで入力なし
            if (!_isWalking)
            {
                // 同じ方向にすぐ再入力でダッシュ
                if (_currentStopMoveInputTime < _param.dashInputThreshold && (
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
                if (_currentStopMoveInputTime > _param.dashInputThreshold)
                {
                    _isDashing = false;
                }
            }
        }

        velocity.x = input.move.x * (_isDashing ? _param.dashSpeed : 
            _isSliding ? _param.slideSpeed : _param.moveSpeed);

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
            _warpDashDirection = direction switch {
                WarpControl.eWarpDirection.Up => _param.warpDashUp,
                WarpControl.eWarpDirection.UpRight => _param.warpDashUpRight,
                WarpControl.eWarpDirection.Right => _param.warpDashRight,
                WarpControl.eWarpDirection.DownRight => _param.warpDashDownRight,
                WarpControl.eWarpDirection.Down => _param.warpDashDown,
                WarpControl.eWarpDirection.DownLeft => _param.warpDashDownLeft,
                WarpControl.eWarpDirection.Left => _param.warpDashLeft,
                WarpControl.eWarpDirection.UpLeft => _param.warpDashUpLeft,
                _ => Vector2.zero
            };

            // 一瞬待機
            yield return new WaitForSeconds(_param.warpWaitTime);

            // ワープ実行
            _warpControl.Warp(direction);

            yield return null;

            // ワープダッシュ実行
            _isDashing = true;
            _isWarpDashing = true;
            _isWarpDelay = false;
            _currentWarpDashTime = 0;

            // ワープダッシュのクールタイムをリセット
            _currentWarpCoolTime = _param.warpCoolTime;
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