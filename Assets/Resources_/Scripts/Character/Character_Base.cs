using UnityEngine;

public class Character_Base : MonoBehaviour
{
    // 共通パラメータ
    [SerializeField] protected CommonParameter _param;
    // キャラクターパラメータ
    [SerializeField] protected CharacterParameter _charaParam;
    public bool isInvincible => _charaParam != null && _charaParam.isInvincible;

    // 地面チェッカー
    [SerializeField] protected LayerMask _groundLayer;
    // 壁チェッカー
    [SerializeField] protected LayerMask _wallLayer;
    // 障害物チェッカー
    [SerializeField] protected LayerMask _obstacleLayer;

    [SerializeField] protected Collider2D _col;
    [SerializeField] protected Rigidbody2D _rb;
    [SerializeField] protected SpriteRenderer _sprite;

    // 能力スロット
    [SerializeField] protected Ability_Base _abilityY;
    [SerializeField] protected Ability_Base _abilityX;
    [SerializeField] protected Ability_Base _abilityA;

    // チェッカーパラメータ
    protected Vector3 _groundCheckLocalPos = default;
    protected Vector3 _groundCheckScale = default;
    protected Vector3 _wallCheckLeftLocalPos = default;
    protected Vector3 _wallCheckRightLocalPos = default;
    protected Vector3 _wallCheckScale = default;

    // キャラクター状態フラグ
    protected bool _isRight = true; // 右向きかどうか
    protected bool _isWalking;
    protected bool _isDashing;
    protected bool _isWarpDelay;
    protected bool _isWarpDashing;
    protected bool _isSliding;      // スライディング中かどうか
    protected bool _isSlidingCanceling; // スライディングキャンセル中かどうか
    protected bool _isGroundSticking; // 地面に張り付いている状態
    protected bool _isWallSliding;  // 壁に沿って滑っている状態
    protected bool _isGrounded;
    protected bool _isJumping;
    protected bool _isTouchingLeft;
    protected bool _isTouchingRight;

    // 通常移動可能かどうか
    protected bool _CanMove => !_isWarpDashing && !_isSlidingCanceling;
    // 重力を適用するかどうか
    protected bool _EnableGravity => !_isWarpDashing && !_isWallSliding && !_isWarpDelay;
    // ジャンプ力を取得
    protected float _jumpForce => _isDashing ? _param.dashJumpForce :
            _isSliding ? _param.slideJumpForce : _param.jumpForce;

    // 現在のジャンプ時間計測
    protected float _currentJumpTime = 0;
    // 移動入力を止めてから経過した時間計測
    protected float _currentStopMoveInputTime = 0;
    // 直前まで進んでいた方向
    protected Vector2 _lastWalkDirection = Vector2.zero;

    // 行動不能時間計測
    protected float _intervalTimer = 0;
    // ダメージリアクション時間計測
    protected float _damageReactionTimer = 0;

    private void Start()
    {
        _Setup();
    }

    /// <summary>
    /// Startで実行されるセットアップ
    /// </summary>
    protected virtual void _Setup() {
        _rb.gravityScale = 0;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 地面チェックの初期化
        _groundCheckLocalPos = Vector3.up * (-GetCharacterSize().y / 2 - _param.groundCheckHeight);
        _groundCheckScale = new Vector3(GetCharacterSize().x - _param.checkerBuffer, _param.groundCheckHeight, 1);

        // 壁チェックの初期化
        var chara_size = GetCharacterSize();
        _wallCheckLeftLocalPos = Vector3.right * (-chara_size.x / 2 - _param.wallCheckWidth);
        _wallCheckRightLocalPos = Vector3.right * (chara_size.x / 2 + _param.wallCheckWidth);
        _wallCheckScale = new Vector3(_param.wallCheckWidth, chara_size.y - _param.checkerBuffer, 1);
    }

    private void FixedUpdate()
    {
        _CheckTerrain();
        _ApplyGravity();
        _UpdateSpecials();

        if(_intervalTimer > 0) {
            _intervalTimer -= Time.deltaTime; 
        }
        if(_damageReactionTimer > 0) {
            _damageReactionTimer -= Time.deltaTime;
        }

        // 向きの更新
        if (_sprite != null && _isRight != _sprite.flipX) {
            _sprite.flipX = _isRight;
            _abilityX?.SetIsRight(_isRight);
            _abilityY?.SetIsRight(_isRight);
            _abilityA?.SetIsRight(_isRight);
        }
    }

    /// <summary>
    /// コントローラ入力
    /// </summary>
    public virtual void UpdateControl(CharacterInputData input) {
        _UpdateMotor(input);

        _UpdateAbility(_abilityY, input.move, input.abilityYPressed, input.abilityYHeld);
        _UpdateAbility(_abilityX, input.move, input.abilityXPressed, input.abilityXHeld);
        _UpdateAbility(_abilityA, input.move, input.abilityAPressed, input.abilityAHeld);
    }

    /// <summary>
    /// 能力の更新処理
    /// </summary>
    private void _UpdateAbility(Ability_Base ability, Vector2 dir_input, bool button_pressed, bool button_held) {
        if(ability == null) {
            return;
        }

        ability.DirectionInput(dir_input);
        // 単押し使用
        if (button_pressed) {
            _AbilityResult(ability.ExecuteSimple());
        }
        // 長押し使用
        if (button_held) {
            _AbilityResult(ability.ExecuteLong());
        }
        // ボタンを離したときの処理
        if (!button_held && !button_pressed) {
            ability.ExecuteRelease();
        }
    }

    private void _AbilityResult(eAbilityResult result) {
        switch(result) {
            case eAbilityResult.None:
                break;
            case eAbilityResult.IceSlash1:
            case eAbilityResult.IceSlash2:
            case eAbilityResult.IceSlash3:
            case eAbilityResult.IceSeparate:
                // 斬撃隙
                _intervalTimer = _param.iceSlashInterval;
                _rb.linearVelocity = Vector2.zero;
                if (!_isGrounded) {
                    _rb.linearVelocity = Vector2.up * _param.slashRebound;
                    _currentJumpTime = 0;
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// キャラクターごとに移動処理を実装
    /// </summary>
    protected virtual void _UpdateMotor(CharacterInputData input) {
        Vector2 velocity = _rb.linearVelocity;

        if (!_CanMove || _damageReactionTimer > 0 || _intervalTimer > 0) {
            return;
        }

        // ジャンプ
        if (input.jumpPressed && _isGrounded) {
            velocity.y = _jumpForce;
            _currentJumpTime = _param.maxJumpHoldTime;
            _isJumping = true;
        }
        // ジャンプリリース
        if ((!input.jumpHeld && _isJumping) || _currentJumpTime <= 0) {
            _isJumping = false;
        }
        // 長押しジャンプ
        if (input.jumpHeld && _isJumping) {
            velocity.y = _jumpForce;
            _currentJumpTime -= Time.deltaTime;
        }

        // 移動入力
        if (input.move.x != 0) {
            // 直前まで入力なし
            if (!_isWalking) {
                // 同じ方向にすぐ再入力でダッシュ
                if (_currentStopMoveInputTime < _param.dashInputThreshold && (
                    (Mathf.Sign(input.move.x) == Mathf.Sign(_lastWalkDirection.x) && !_isDashing) ||
                    (Mathf.Sign(input.move.x) != Mathf.Sign(_lastWalkDirection.x) && _isDashing))) {
                    _isDashing = true;
                }

                _isWalking = true;
            }

            // 移動中は常にフラグリセット
            _lastWalkDirection = input.move;
            _currentStopMoveInputTime = 0;

            if( input.move.x > 0 ) {
                _isRight = true;
            } else if( input.move.x < 0 ) {
                _isRight = false;
            }
        } else // 入力停止
          {
            if (_isWalking) {
                // 歩行から停止
                _isWalking = false;
                _currentStopMoveInputTime = 0;
            } else {
                // 停止中はタイマー更新
                _currentStopMoveInputTime += Time.deltaTime;
                if (_currentStopMoveInputTime > _param.dashInputThreshold) {
                    _isDashing = false;
                }
            }
        }

        velocity.x = input.move.x * (_isDashing ? _param.dashSpeed :
            _isSliding ? _param.slideSpeed : _param.moveSpeed);

        // 壁に接触している場合は横移動を0にする
        if ((_isTouchingLeft && input.move.x < 0) || (_isTouchingRight && input.move.x > 0)) {
            velocity.x = 0;
        }

        _rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Updateに追加する処理
    /// </summary>
    protected virtual void _UpdateSpecials() { }

    /// <summary>
    /// 地形チェック
    /// </summary>
    private void _CheckTerrain()
    {
        _isTouchingLeft = Physics2D.OverlapBox(transform.position + _wallCheckLeftLocalPos, _wallCheckScale, 0, _wallLayer);
        _isTouchingRight = Physics2D.OverlapBox(transform.position + _wallCheckRightLocalPos, _wallCheckScale, 0, _wallLayer);

        _isGrounded = Physics2D.OverlapBox(transform.position + _groundCheckLocalPos, _groundCheckScale, 0, _groundLayer);
    }

    /// <summary>
    /// 重力適用
    /// </summary>
    private void _ApplyGravity()
    {
        if (!_EnableGravity || _intervalTimer > 0)
        {
            // 重力適用をスキップ
            return;
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
    /// ダメージ処理
    /// </summary>
    /// <param name="damage">ダメージ数</param>
    /// <param name="reactionTime">ダメージリアクション時間</param>
    public void Damage(float damage, float reactionTime = 0.1f) {
        if (_charaParam != null) {
            _charaParam.ExecuteDamage(damage);

            // ダメージリアクション
            _damageReactionTimer = reactionTime;
            _rb.linearVelocity = Vector2.zero;
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