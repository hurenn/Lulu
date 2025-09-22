using System.Collections;
using UnityEngine;

public class Character_Base : MonoBehaviour
{
    // 共通パラメータ
    [SerializeField] protected CommonParameter _param;
    // キャラクターパラメータ
    [SerializeField] protected CharacterParameter _charaParam;
    public bool isInvincible => _charaParam != null && _charaParam.isInvincible;

    // 入力データ
    protected CharacterInputData _inputData = new CharacterInputData();

    // 地面チェッカー
    [SerializeField] protected LayerMask _groundLayer;
    // 壁チェッカー
    [SerializeField] protected LayerMask _wallLayer;
    // 障害物チェッカー
    [SerializeField] protected LayerMask _obstacleLayer;

    [SerializeField] protected Collider2D _col;
    [SerializeField] protected Rigidbody2D _rb;
    [SerializeField] protected SpriteRenderer _sprite;
    [SerializeField] protected Animator _anim;

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
    protected bool _isSlidingJump;  // スライディングジャンプ中かどうか
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

    // 死亡フラグ
    protected bool _isDie = false;
    public bool isDead => _isDie;

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
        if (_sprite != null) {
            _sprite.flipX = _isRight;
            _abilityX?.SetCharacterTransform(_isRight, transform, _param, _charaParam);
            _abilityY?.SetCharacterTransform(_isRight, transform, _param, _charaParam);
            _abilityA?.SetCharacterTransform(_isRight, transform, _param, _charaParam);
        }
    }

    /// <summary>
    /// コントローラ入力
    /// </summary>
    public virtual void UpdateControl(CharacterInputData input) {
        if( _isDie ) {
            input.move = Vector2.zero;
            input.jumpHeld = false;
            input.jumpPressed = false;
            input.abilityXHeld = false;
            input.abilityXPressed = false;
            input.abilityYHeld = false;
            input.abilityYPressed = false;
            input.abilityAHeld = false;
            input.abilityAPressed = false;
        }
        // 入力データ保存
        _inputData = input;

        _UpdateMotor();

        _UpdateAbility(_abilityY, input.move, input.abilityYPressed, input.abilityYHeld);
        _UpdateAbility(_abilityX, input.move, input.abilityXPressed, input.abilityXHeld);
        _UpdateAbility(_abilityA, input.move, input.abilityAPressed, input.abilityAHeld);
    }

    public void SetAbilitySlot(eAbilityType ability_type, eAbilitySlot ability_slot) {
        var ability = AbilityFactory.CreateAbility(ability_type, transform, ability_slot);
        if (ability == null) {
            Debug.LogError("能力生成失敗: " + ability_type);
            return;
        }

        // スロットにセット
        switch (ability_slot) {
            case eAbilitySlot.Y:
                _abilityY = ability;
                break;
            case eAbilitySlot.X:
                _abilityX = ability;
                break;
            case eAbilitySlot.A:
                _abilityA = ability;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 能力の更新処理
    /// </summary>
    private void _UpdateAbility(Ability_Base ability, Vector2 dir_input, bool button_pressed, bool button_held) {
        if(ability == null) {
            return;
        }

        // 単押し使用
        if (button_pressed) {
            _AbilityResult(ability.ExecuteSimple(), dir_input);
        }
        // 長押し使用
        if (button_held) {
            _AbilityResult(ability.ExecuteLong(), dir_input);
        }
        // ボタンを離したときの処理
        if (!button_held && !button_pressed) {
            ability.ExecuteRelease();
        }
    }

    private void _AbilityResult(eAbilityResult result, Vector2 dir_input) {
        switch (result) {
            case eAbilityResult.None:
                break;
            case eAbilityResult.IceSlash1:
            case eAbilityResult.IceSlash2:
            case eAbilityResult.IceSlash3:
            case eAbilityResult.IceSeparate:
                // 斬撃隙
                _intervalTimer = _param.iceSlashInterval;
                _rb.linearVelocity = Vector2.zero;
                Vector2 slash_bounce_move = Vector2.right * dir_input.x * _param.slashMoveForce;
                if (!_isGrounded) {
                    slash_bounce_move.y = _param.slashRebound;
                    _currentJumpTime = 0;
                }
                _rb.linearVelocity = slash_bounce_move;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// キャラクターごとに移動処理を実装
    /// </summary>
    protected virtual void _UpdateMotor() {
        /*
        Vector2 velocity = _rb.linearVelocity;

        if (!_CanMove || _damageReactionTimer > 0 || _intervalTimer > 0) {
            return;
        }

        // ジャンプ
        if (input.jumpPressed && _isGrounded) {
            velocity.y = _jumpForce;
            _currentJumpTime = _param.maxJumpHoldTime;
            _isJumping = true;
            _anim?.SetBool("Jump", true);
            _anim?.Play("Jump");
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

        //状態に応じたフラグ管理
        _anim?.SetBool("Jump", _isJumping);
        _anim?.SetBool("Fall", !_isGrounded);

        // 移動入力
        if (input.move.x != 0) {
            // 直前まで入力なし
            if (!_isWalking) {
                // 同じ方向にすぐ再入力でダッシュ
                if (_currentStopMoveInputTime < _param.dashInputThreshold && (
                    (Mathf.Sign(input.move.x) == Mathf.Sign(_lastWalkDirection.x) && !_isDashing) ||
                    (Mathf.Sign(input.move.x) != Mathf.Sign(_lastWalkDirection.x) && _isDashing))) {
                    _isDashing = true;
                    _anim?.SetBool("Dash", true);
                }
                _isWalking = true;
                _anim?.SetBool("Walk", true);
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

                //移動アニメーション停止
                _anim?.SetBool("Walk", false);
                _anim?.SetBool("Dash", false);
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
        */
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
        // AnimatorControllerがセットされている場合のみ実行
        if (_anim != null && _anim.runtimeAnimatorController != null) {
            _anim.SetBool("IsGround", _isGrounded); //接地フラグ
        }
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
    /// レベルアップ実行
    /// </summary>
    /// <param name="level_type"></param>
    public void Levelup(PlayerParameter.eLevelType level_type) {
        // 対応するレベルを上げる
        var player_param = PlayerParameter.Instance;
        if (player_param != null) {
            switch (level_type) {
                case PlayerParameter.eLevelType.HP:
                    player_param.levelParameter.hpLevel++;
                    break;
                case PlayerParameter.eLevelType.MP:
                    player_param.levelParameter.mpLevel++;
                    break;
                case PlayerParameter.eLevelType.Attack:
                    player_param.levelParameter.attackLevel++;
                    break;
                default:
                    break;
            }
        }
        Debug.Log($"Levelup:{level_type.ToString()}");

        // レベルに応じたパラメータを適用
        ApplyPlayerParameter();
    }

    /// <summary>
    /// レベルに応じたパラメータを適用
    /// </summary>
    public void ApplyPlayerParameter() {
        var player_param = PlayerParameter.Instance;
        if (player_param != null && _charaParam != null) {
            _charaParam.SetMaxHP(_charaParam.defaultMaxHP + player_param.levelParameter.hpLevel);
            _charaParam.SetMaxMP(_charaParam.defaultMaxMP + player_param.levelParameter.mpLevel * 10.0f);
            _charaParam.attackPower = _charaParam.attackPower + player_param.levelParameter.attackLevel;
        }
    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    /// <param name="damage">ダメージ数</param>
    /// <param name="blow_power_right">右向きで吹っ飛ぶ力</param>
    /// <param name="invincible_time">無敵時間</param>
    /// <param name="damage_reaction_time">動けない時間</param>
    public virtual void Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time) {
        if (isInvincible || _isDie) {
            return;
        }

        if (_charaParam != null) {
            // ダメージ実行
            _charaParam.ExecuteDamage(damage, invincible_time, ref _isDie);

            if (_isDie) {
                StartCoroutine(Die());
                return;
            }

            // アニメーション
            _anim.Play("Damage");

            // ダメージリアクション
            _damageReactionTimer = damage_reaction_time;

            // 吹っ飛び
            _rb.linearVelocity = blow_power_right;
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    /// <param name="blow_power_right"></param>
    protected virtual IEnumerator Die() {
        // 死亡処理
        _anim.Play("Die");

        // アニメーションの長さを取得してから削除
        float destroy_time = 0;
        var clip_info = _anim.GetCurrentAnimatorClipInfo(0);
        if (clip_info.Length > 0) {
            destroy_time = clip_info[0].clip.length;
        }

        Destroy(gameObject, destroy_time);
        yield break;
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

    /// <summary>
    /// MP回復
    /// </summary>
    /// <param name="amount">回復値</param>
    /// <param name="force">強制回復</param>
    public void RecoverMP(float amount, bool force) {
        if (_charaParam != null) {
            _charaParam.RecoverMP(amount, force);
        }
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