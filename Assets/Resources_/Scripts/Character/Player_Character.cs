using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーキャラクター（ルル）
/// </summary>
public class Player_Character : Character_Base {
    private CharacterParameter_Player _player_charaParam => _charaParam as CharacterParameter_Player;

    // ワープ管理
    [SerializeField] private WarpControl _warpControl;
    // ワープエフェクト
    [SerializeField] private GameObject _warpEffectPrefab;
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
    // スライドジャンプ時間計測
    private float _currentSlideJumpTime = 0;
    // 壁に沿って滑る速度
    private float _currentWallSlideTime = 0;

    private bool _isAvoid = false;
    private float _isAvoidTimer = 0.01f;
    private float _currentAvoidTime = 0f;

    // 能力スロット
    [SerializeField] protected Ability_Base _abilityY;
    [SerializeField] protected Ability_Base _abilityX;
    [SerializeField] protected Ability_Base _abilityA;
    // 能力スロット一時保存
    private Dictionary<eAbilityType, eAbilitySlot> _tmpAbilitySlot = new Dictionary<eAbilityType, eAbilitySlot>();

    // レベルアップオブジェクト
    [SerializeField] private Animator _levelUpAnimator;

    // オート氷攻撃チェッカー
    [SerializeField] private AutoIceAttackChecker _autoAttackChecker;

    private bool _isIceInvincible = false;
    private float _iceInvincibleTime = 0.1f;
    private float _currentIceInvincibleTime = 0f;

    public void SaveAbilitySlot() {
        foreach (var ability in _tmpAbilitySlot) {
            if (ability.Key != eAbilityType.None) {
                _playerParam.AddAbility(ability.Key, ability.Value);
            } else {
                _playerParam.RemoveAbility(ability.Value);
            }
        }
    }

    // プレイヤー用パラメーター
    private PlayerParameter _playerParam;

    protected override void _Setup() {
        base._Setup();
        if (_warpControl != null) {
            _warpControl.Setup(_OnPreWarpCommon, _OnWarpEndCommon);
        }
        _playerParam = PlayerParameter.Instance;
        ApplyPlayerParameter();
        _SetupHadAbility();
    }

    /// <summary>
    /// 取得済み能力のセットアップ
    /// </summary>
    private void _SetupHadAbility() {
        var had_ability = _playerParam.Abilities;
        foreach (var ability in had_ability) {
            SetAbilitySlot(ability.Key, ability.Value, false);
        }
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        _UpdateWarpDash();
        _UpdateSliding();
        _UpdateWallSlideMove();
        _UpdateSlideJump();

        // 氷無敵時間計測
        if (_isIceInvincible) {
            // 無敵保障時間計測
            _currentIceInvincibleTime -= Time.fixedDeltaTime;
            if (_currentIceInvincibleTime <= 0) {
                if (!_isWarpDashing) {
                    // 無敵終了
                    _isIceInvincible = false;
                }
            }
        }

        _abilityX?.UpdateParameter(_isRight, transform, _param, _player_charaParam, _warpControl, _motorStates);
        _abilityY?.UpdateParameter(_isRight, transform, _param, _player_charaParam, _warpControl, _motorStates);
        _abilityA?.UpdateParameter(_isRight, transform, _param, _player_charaParam, _warpControl, _motorStates);

        if (_currentWarpCoolTime > 0) {
            _currentWarpCoolTime -= Time.fixedDeltaTime;
        }
        if (_isAvoid) {
            _currentAvoidTime -= Time.fixedDeltaTime;
            if (_currentAvoidTime <= 0) {
                _isAvoid = false;
                _anim.SetBool("Avoid", false);
            }
        }

        // 着地時MP回復
        if (_isGrounded && !_player_charaParam.isMaxMP) {
            _player_charaParam.RecoverMP();
        }

        // 地面張り付き状態計測
        if (_currentLandingDashTime < _param.maxLandingDashTime && _isGroundSticking) {
            _currentLandingDashTime += Time.fixedDeltaTime;
            if (_currentLandingDashTime >= _param.maxLandingDashTime) {
                _isGroundSticking = false; // 張り付き状態を解除
            }
        }
    }

    /// <summary>
    /// MP回復
    /// </summary>
    /// <param name="amount">回復値</param>
    /// <param name="force">強制回復</param>
    public void RecoverMP(float amount, bool force) {
        if (_player_charaParam == null) {
            return;
        }
        _player_charaParam.RecoverMP(amount, force);
    }

    /// <summary>
    /// ワープダッシュの更新処理
    /// </summary>
    private void _UpdateWarpDash() {
        if (!_isWarpDashing) {
            return; // ワープダッシュ中でない場合は何もしない
        }

        // オート攻撃対象がいる場合はオート攻撃を実行
        var target_enemy = _autoAttackChecker.PopTargetEnemy();
        if (target_enemy != null && _HasAbility<Ability_Ice>()) {
            _isWarpDashing = false;
            _OnExecuteIceAutoAttack(target_enemy);
            return;
        }

        // ワープダッシュの最大時間を超えた場合は終了
        if (_currentWarpDashTime > _param.maxWarpDashTime) {
            _isWarpDashing = false; // ワープダッシュ終了
            return; // ワープダッシュのクールタイム中は何もしない
        }
        _currentWarpDashTime += Time.deltaTime;

        // ワープダッシュ移動
        var dash_velocity = _warpDashDirection;
        _rb.linearVelocity = dash_velocity;
        // ワープダッシュ力を減衰させる
        _warpDashDirection *= _param.warpDashDamping;
        if (_warpDashDirection.magnitude < 0.2f) {
            _warpDashDirection = Vector2.zero; // ダッシュ力が小さくなったらリセット
        }

        // 地面に接触しているかチェック
        if (_isGrounded) {
            if (_warpDashDirection.x != 0) {
                // 地面に対して斜めに移動している場合はスライディングを実行
                _ExecuteSlide(); // スライディング実行
            } else {
                // 地面に対して垂直に移動している場合は張り付き状態に移行
                _isGroundSticking = true;
                _currentLandingDashTime = 0;
            }
            _player_charaParam.OnRecoverOverheat(); // オーバーヒート回復
            _isWarpDashing = false; // ワープダッシュ終了
            return;
        }
        // 壁に接触しているかチェック
        if ((_isTouchingLeft && _warpDashDirection.x < 0) || (_isTouchingRight && _warpDashDirection.x > 0)) {
            _ExecuteWallSlide(); // 壁滑り実行
            return;
        }
    }

    /// <summary>
    /// 壁滑りの更新処理
    /// </summary>
    private void _UpdateWallSlideMove() {
        // 壁滑り中でない場合は何もしない
        if (!_isWallDash) {
            return;
        }

        if (_currentWallSlideTime >= _param.maxWallSlideTime) {
            _SetWallDash(false);
            return;
        }
        _currentWallSlideTime += Time.deltaTime;

        // 壁に沿って滑る処理
        Vector2 velocity = Vector2.zero;
        if (_warpDashDirection.y < 0) {
            // 壁に沿って下方向に滑る
            velocity.y = -_param.wallSlideSpeed;
        } else {
            // 壁に沿って上方向に滑る
            velocity.y = _param.wallSlideSpeed;
        }
        _SetWallDash(_isWallDash, velocity.y >= 0);
        _rb.linearVelocity = velocity;

        // 壁との接触が無くなった場合は壁滑りを終了してジャンプする
        if (!_isTouchingLeft && !_isTouchingRight) {
            _SetWallDash(false);
            _isJumping = true;
            if (_seJump != null) {
                _audioSource?.PlayOneShot(_seJump);
            }

            velocity.y = _param.jumpForce;
            _rb.linearVelocity = velocity; // ジャンプ力を適用
        }

        // 着地した場合は壁滑りを終了
        if (_isGrounded) {
            _SetWallDash(false);
        }
    }

    /// <summary>
    /// スライディング実行
    /// </summary>
    private void _ExecuteSlide() {
        _isDashing = true;
        _SetSliding(true);
        _currentSlideTime = 0;
    }
    /// <summary>
    /// ダッシュ実行
    /// </summary>
    private void _ExecuteDash() {
        _isDashing = true;
        _anim?.SetBool("Dash", true);
    }

    /// <summary>
    /// 壁ダッシュ実行
    /// </summary>
    private void _ExecuteWallSlide() {
        // 壁方向への入力が無ければキャンセル
        if (!((_isTouchingLeft && _inputData.move.x < 0) || (_isTouchingRight && _inputData.move.x > 0))) {
            return;
        }

        _SetWallDash(true, _rb.linearVelocity.y >= 0);
        _currentWallSlideTime = 0;
        _isSlidingJump = false;
        _isWarpDashing = false;
    }

    /// <summary>
    /// ワープダッシュ実行
    /// </summary>
    private void _ExecuteWarpDash() {
        _isDashing = true;
        _isWarpDashing = true;
        _currentWarpDashTime = 0;
    }

    /// <summary>
    /// スライディング処理
    /// </summary>
    private void _UpdateSliding() {
        if (_isSliding) {
            // オート攻撃対象がいる場合はオート攻撃を実行
            var target_enemy = _autoAttackChecker.PopTargetEnemy();
            if (target_enemy != null && _HasAbility<Ability_Ice>()) {
                _SetSliding(false); // スライディング終了
                _currentSlideTime = 0;
                _OnExecuteIceAutoAttack(target_enemy);
                return;
            }

            // スライドダッシュ中はキャラクターの位置を更新
            Vector2 velocity = _rb.linearVelocity;
            var dash_dir = _warpDashDirection.normalized;
            velocity.x = dash_dir.x * _param.slideSpeed;
            _rb.linearVelocity = velocity;

            _currentSlideTime += Time.deltaTime;
            if (_currentSlideTime >= _param.maxSlideTime) {
                _SetSliding(false); // スライディング終了
                _currentSlideTime = 0;
            }
            // 壁に接触している場合はスライディング終了
            if ((_isTouchingLeft && velocity.x < 0) || (_isTouchingRight && velocity.x > 0)) {
                _SetSliding(false);
                _currentSlideTime = 0;
            }
        }
        if (_isSlidingCanceling) {
            _SetSliding(false);

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

    private void _UpdateSlideJump() {
        // スライドジャンプ時間計測
        if (!_isSlidingJump) {
            return;
        }

        // 着地した場合はスライドジャンプ終了
        if (_isGrounded && _currentSlideJumpTime > 0.1f) {
            _isSlidingJump = false;
            return;
        }
        _currentSlideJumpTime += Time.fixedDeltaTime;

        // 壁に当たった場合は壁ダッシュに移行
        if ((_isTouchingLeft && _warpDashDirection.x < 0) || (_isTouchingRight && _warpDashDirection.x > 0)) {
            _ExecuteWallSlide();
            return;
        }
    }

    public override void UpdateControl(CharacterInputData input) {
        if (_isDead) {
            input.abilityXHeld = false;
            input.abilityXPressed = false;
            input.abilityYHeld = false;
            input.abilityYPressed = false;
            input.abilityAHeld = false;
            input.abilityAPressed = false;
        }
        base.UpdateControl(input);

        _UpdateAbility(_abilityY, input.move, input.abilityYPressed, input.abilityYHeld, input.abilityYReleased, input);
        _UpdateAbility(_abilityX, input.move, input.abilityXPressed, input.abilityXHeld, input.abilityXReleased, input);
        _UpdateAbility(_abilityA, input.move, input.abilityAPressed, input.abilityAHeld, input.abilityAReleased, input);

        _Warp();
    }

    /// <summary>
    /// 能力スロットにセット
    /// </summary>
    public void SetAbilitySlot(eAbilityType ability_type, eAbilitySlot ability_slot, bool is_effect = true) {
        var ability = AbilityFactory.CreateAbility(ability_type, ability_slot,
            () => _AbilityResult(eAbilityResult.SpecialEnd, _inputData.move), is_effect);

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

        if (!_tmpAbilitySlot.ContainsKey(ability_type)) {
            _tmpAbilitySlot.Add(ability_type, ability_slot);
        }
        if (ability == null) {
            _playerParam.RemoveAbility(ability_slot);
        }
    }

    /// <summary>
    /// 能力の更新処理
    /// </summary>
    private void _UpdateAbility(Ability_Base ability, Vector2 dir_input, bool button_pressed, bool button_held, bool button_released, CharacterInputData input) {
        if (ability == null) {
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
        if (button_released) {
            ability.ExecuteRelease();
        }
    }

    /// <summary>
    /// 能力の実行結果処理
    /// </summary>
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
                _isWarpDashing = false; // ワープダッシュ終了
                Vector2 slash_bounce_move = Vector2.right * dir_input.x * _param.slashMoveForce;
                if (!_isGrounded) {
                    slash_bounce_move.y = _param.slashRebound;
                    _currentJumpTime = 0;
                }
                _rb.linearVelocity = slash_bounce_move;
                break;
            case eAbilityResult.IceLockonSlash:
                // 斬撃隙
                _intervalTimer = _param.iceSlashInterval;
                _rb.linearVelocity = Vector2.zero;

                // ロックオン対象の方向を向く
                var lockon = LockonManager.Instance;
                Vector3 to_target = lockon.targetTransform.position - transform.position;
                to_target.y = 0; // 水平成分のみ
                _isRight = to_target.x > 0;

                LockonManager.Instance.ClearTarget(); // ロックオン解除
                break;
            case eAbilityResult.FireSpecial:
            case eAbilityResult.IceSpecial:
            case eAbilityResult.LightSpecial:
                // 操作不能にする
                _specialUsing = true;
                // 慣性をリセット
                _rb.linearVelocity = Vector2.zero;
                break;
            case eAbilityResult.SpecialEnd:
                // 操作可能にする
                _specialUsing = false;
                break;
            default:
                break;
        }

        if (result != eAbilityResult.LightParry && result != eAbilityResult.LightDome) {
            _OnReleaseLightDome();
        }
        if (result != eAbilityResult.LightDome) {
            _OnResetFireInterval();
        }
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time) {
        if (isInvincible || _isDead) {
            return false;
        }

        // 氷の能力で無敵（仮対応）
        if ( _isIceInvincible) {
            return false;
        }

        // 光の能力で無敵回避
        bool is_light_avoid = _player_charaParam.isLightInvincible || _player_charaParam.isAutoLightInvincible;
        if (is_light_avoid && !_player_charaParam.isOverheat) {
            // MP消費
            _player_charaParam.ConsumeMP(_player_charaParam.isLightInvincible ? eAbilityType.LightAvoid : eAbilityType.LightAutoAvoid);

            // 自動発光回避
            if (_player_charaParam.isAutoLightInvincible && !_player_charaParam.isLightInvincible) {
                _OnAvoidAutoLight();
            }

            _anim.Play("Warp_Enter");       // ワープアニメ再生
            _anim.SetBool("Avoid", true);   // ワープアニメフラグ
            _isAvoid = true;
            _currentAvoidTime = _isAvoidTimer;

            StartCoroutine(_warpControl.AvoidWarp(
                () => {
                    Instantiate(_warpEffectPrefab, transform.position + transform.up, Quaternion.identity);
                }));
            return false;
        }

        if (_isSliding || _isEventInvincible) {
            return false; // ダメージ無効
        }

        _isWarpDashing = false; // ワープダッシュ終了
        _SetSliding(false);
        _isSlidingJump = false; // スライディングジャンプ終了
        _SetWallDash(false);
        _isGroundSticking = false; // 地面張り付き状態終了
        _isJumping = false; // ジャンプ終了

        return base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time);
    }

    protected override IEnumerator Die() {
        yield return base.Die();

        yield return new WaitForSeconds(1.0f);

        Instantiate(_warpEffectPrefab, transform.position, Quaternion.identity);
        _sprite.enabled = false;

        yield return new WaitForSeconds(1.0f);

        // シーン再読み込み
        ChangeScene.LoadScene(false);
    }

    /// <summary>
    /// 移動入力
    /// </summary>
    protected override void _UpdateMotor() {
        Vector2 velocity = _rb.linearVelocity;

        if (_damageReactionTimer > 0 || _intervalTimer > 0 || _specialUsing) {
            return;
        }

        // 壁滑り中の入力
        if (_isWallDash) {
            // 壁と反対方向に移動しようとする入力があれば壁滑りを終了
            if (_inputData.move.x != 0 && Mathf.Sign(_inputData.move.x) != Mathf.Sign(_warpDashDirection.x)) {
                _SetWallDash(false);
                return;
            }
        }

        // スライディング中に逆方向入力でキャンセル
        if (_isSliding && _inputData.move.x != 0 && Mathf.Sign(_inputData.move.x) != Mathf.Sign(_warpDashDirection.x)) {
            _isSlidingCanceling = true; // スライディングキャンセル中フラグを立てる
            _currentSlideTime = 0;
            return;
        }

        // スライディングキャンセル中にジャンプでキャンセル
        if (_isSlidingCanceling && _inputData.jumpPressed) {
            _isSlidingCanceling = false; // スライディングキャンセル終了
        }

        // 地面張り付き状態の入力
        if (_isGroundSticking) {
            if (_inputData.move.x != 0) {
                // 張り付き状態で移動入力があれば張り付き状態を解除
                _isGroundSticking = false;
                _warpDashDirection = _inputData.move.x > 0 ? _param.warpDashDownRight : _param.warpDashDownLeft;
                _ExecuteDash(); // ダッシュ実行
            } else if (_inputData.jumpPressed) {
                // 張り付き状態でジャンプ入力があればジャンプ
                _isGroundSticking = false;
            }
        }

        if (!_CanMove) {
            return;
        }

        // ジャンプ
        if (_inputData.jumpPressed && _isGrounded && !(_inputData.move.y < -0.5f && _inputData.move.x == 0)) {
            // スライディングジャンプ
            if (_isSliding) {
                _SetSliding(false);
                _isSlidingJump = true;

                // y方向の加速を無視
                _warpDashDirection.y = 0;

                // スライディング時間リセット
                _currentSlideTime = 0;
                _currentSlideJumpTime = 0;
            }

            velocity.y = _jumpForce;
            _currentJumpTime = _param.maxJumpHoldTime;
            _isJumping = true;
            _anim?.SetBool("Jump", true);
            _anim?.Play("Jump");
            if (_seJump != null) {
                _audioSource?.PlayOneShot(_seJump);
            }
        }
        // ジャンプリリース
        if ((!_inputData.jumpHeld && _isJumping) || _currentJumpTime <= 0) {
            _isJumping = false;
        }
        // 長押しジャンプ
        if (_inputData.jumpHeld && _isJumping) {
            velocity.y = _jumpForce;
            _currentJumpTime -= Time.deltaTime;
        }

        //状態に応じたフラグ管理
        _anim?.SetBool("Jump", _isJumping);
        _anim?.SetBool("Fall", !_isGrounded);

        // 移動入力
        if (_inputData.move.x != 0) {
            // 直前まで入力なし
            if (!_isWalking) {
                // 同じ方向にすぐ再入力でダッシュ
                if (_currentStopMoveInputTime < _param.dashInputThreshold && (
                    (Mathf.Sign(_inputData.move.x) == Mathf.Sign(_lastWalkDirection.x) && !_isDashing) ||
                    (Mathf.Sign(_inputData.move.x) != Mathf.Sign(_lastWalkDirection.x) && _isDashing))) {
                    _ExecuteDash(); // ダッシュ実行
                }
                _isWalking = true;
                _anim?.SetBool("Walk", true);
            }

            // 移動中は常にフラグリセット
            _lastWalkDirection = _inputData.move;
            _currentStopMoveInputTime = 0;
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

        velocity.x = _inputData.move.x * (
            _isSlidingJump ? _param.slideJumpSpeed :
            _isSliding ? _param.slideSpeed :
            _isDashing ? _param.dashSpeed : _param.moveSpeed);

        // 壁に接触している場合は横移動を0にする
        if ((_isTouchingLeft && _inputData.move.x < 0) || (_isTouchingRight && _inputData.move.x > 0)) {
            velocity.x = 0;
        }

        // 向きの更新
        if (_inputData.move.x > 0) {
            _isRight = true;
        } else if (_inputData.move.x < 0) {
            _isRight = false;
        }
        _warpControl.isRight = _isRight;

        // 自動攻撃判定の位置調整
        if (_autoAttackChecker != null) {
            var auto_attack_pos = _autoAttackChecker.transform.localPosition;
            auto_attack_pos.x = Mathf.Abs(auto_attack_pos.x) * (_isRight ? 1 : -1);
            _autoAttackChecker.transform.localPosition = auto_attack_pos;
        }

        _rb.linearVelocity = velocity;
    }

    /// <summary>
    /// ワープ能力
    /// </summary>
    private void _Warp() {
        if (_warpControl == null || _currentWarpCoolTime > 0 || _specialUsing) {
            return;
        }

        // ワープ入力
        if ((!_isGrounded && _inputData.jumpPressed) ||
            (_isGrounded && _inputData.move.y < -0.5f && _inputData.move.x == 0 && _inputData.jumpPressed)) {
            // エフェクト生成
            Instantiate(_warpEffectPrefab, transform.position + transform.up, Quaternion.identity);

            if (_inputData.move.magnitude == 0 && !_warpControl.GetCoinWarpCheck().HasValue) {
                // 入力が無く、コインワープもできない場合はワープしない
                return;
            }

            // ワープ処理開始
            StartCoroutine(WarpStart());

            // ライトドーム自動解除
            _OnReleaseLightDome();
        }

        IEnumerator WarpStart() {
            // MP消費
            var is_success = _player_charaParam.ConsumeMP(eAbilityType.Warp);

            if (!is_success) {
                yield break; // 失敗
            }
            _isIceInvincible = true;
            _currentIceInvincibleTime = _iceInvincibleTime;

            WarpControl.eWarpDirection dash_direction = _warpDirection;

            if (_inputData.move.magnitude != 0) {
                // 入力方向にワープ
                yield return _warpControl.DirectionWarp(_warpDirection);
            } else if (_warpControl.GetCoinWarpCheck().HasValue) {
                // コインワープ
                yield return _warpControl.CoinWarp();
            }

            // ワープダッシュ方向決定
            if (_warpDirection != WarpControl.eWarpDirection.Neutral) {
                dash_direction = _warpDirection;
            } else {
                dash_direction = _warpControl.lastWarpDir; // 直前のワープ方向を使用
            }

            // ワープダッシュの方向を設定
            _warpDashDirection = dash_direction switch {
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

            yield return null;

            // ワープダッシュ実行
            _ExecuteWarpDash();
        }
    }

    private WarpControl.eWarpDirection _warpDirection => _inputData.move switch {
        { x: > 0, y: > 0 } => WarpControl.eWarpDirection.UpRight,
        { x: > 0, y: < 0 } => WarpControl.eWarpDirection.DownRight,
        { x: < 0, y: > 0 } => WarpControl.eWarpDirection.UpLeft,
        { x: < 0, y: < 0 } => WarpControl.eWarpDirection.DownLeft,
        { x: 0, y: > 0 } => WarpControl.eWarpDirection.Up,
        { x: 0, y: < 0 } => WarpControl.eWarpDirection.Down,
        { x: > 0, y: 0 } => WarpControl.eWarpDirection.Right,
        { x: < 0, y: 0 } => WarpControl.eWarpDirection.Left,
        _ => WarpControl.eWarpDirection.Neutral
    };

    void _OnPreWarpCommon() {
        // スライディングリセット
        _SetSliding(false);
        // 重力を無効化
        _isWarpDelay = true;
        // 速度をリセット
        _rb.linearVelocity = Vector2.zero;
        // ワープダッシュの方向リセット
        _warpDashDirection = Vector2.zero;

        //アニメーション管理
        _anim.SetBool("Warp", true);    // ワープアニメフラグ
        _anim.SetBool("Fall", true);    // 空中アニメフラグ
        _anim.Play("Warp_Enter");       // ワープアニメ再生
    }
    void _OnWarpEndCommon() {
        _anim.SetBool("Warp", false);

        // ワープダッシュのクールタイムをリセット
        _currentWarpCoolTime = _param.warpCoolTime;
        // スライドジャンプリセット
        _isSlidingJump = false;
        // 重力を有効化
        _isWarpDelay = false;
    }

    /// <summary>
    /// 経験値追加
    /// </summary>
    public void AddExp(int value) {
        var is_level_up = _playerParam.AddExp(value, ApplyPlayerParameter);
        if(is_level_up) {
            StartCoroutine(_levelUpCoroutine());
        }
    }

    /// <summary>
    /// レベルアップ演出
    /// </summary>
    private IEnumerator _levelUpCoroutine() {
        if( _levelUpAnimator == null) {
            yield break;
        }

        // レベルアップアニメーション再生
        _levelUpAnimator.gameObject.SetActive(true);
        var length = _levelUpAnimator.GetCurrentAnimatorStateInfo(0).length;
        _levelUpAnimator.Play("LevelUp", -1, 0f);
        yield return new WaitForSeconds(length);
        _levelUpAnimator.gameObject.SetActive(false);
    }

    /// <summary>
    /// レベルに応じたパラメータを適用
    /// </summary>
    public void ApplyPlayerParameter() {
        if (_player_charaParam != null) {
            _player_charaParam.SetPlusMaxHp(_playerParam.levelParameter.hpLevel);
            _player_charaParam.SetPlusMaxMp(_playerParam.levelParameter.mpLevel * _param.mpUpPerLevel);
        }
    }

    /// <summary>
    /// オート発光解除
    /// </summary>
    private void _OnReleaseLightDome() {
        var ability = _GetAbility<Ability_Light>();
        if (ability != null) {
            ability.SetAutoLight(false);
        }
    }

    /// <summary>
    /// オート火攻撃間隔リセット
    /// </summary>
    private void _OnResetFireInterval() {
        var ability = _GetAbility<Ability_Fire>();
        if (ability != null) {
            ability.ResetAutoAttackInterval();
        }
    }

    /// <summary>
    /// オート発光回避実行
    /// </summary>
    private void _OnAvoidAutoLight() {
        var ability = _GetAbility<Ability_Light>();
        if (ability != null) {
            ability.AutoAvoid();
        }
    }

    /// <summary>
    /// 氷自動攻撃実行
    /// </summary>
    private void _OnExecuteIceAutoAttack(Enemy_Base target_enemy) {
        var ability = _GetAbility<Ability_Ice>();
        if (ability != null) {

            // 移動場所探索
            WarpControl.eWarpDirection warp_dir =
                _inputData.move.x > 0 ? WarpControl.eWarpDirection.Right : 
                _inputData.move.x < 0 ? WarpControl.eWarpDirection.Left : WarpControl.eWarpDirection.Neutral;
            if(warp_dir == WarpControl.eWarpDirection.Neutral) {
                var to_target = target_enemy.transform.position - transform.position;
                warp_dir = to_target.x > 0 ? WarpControl.eWarpDirection.Right : WarpControl.eWarpDirection.Left;
            }

            var player_warp_target = target_enemy.GetWarpChecker(warp_dir, true);

            // 攻撃場所探索
            WarpControl.eWarpDirection attack_warp_dir = warp_dir == WarpControl.eWarpDirection.Right ? 
                WarpControl.eWarpDirection.Left : WarpControl.eWarpDirection.Right;
            var attack_warp_target = target_enemy.GetWarpChecker(attack_warp_dir, true);

            if (player_warp_target == null) {
                // 自動攻撃失敗
                return;
            }

            // 目標にワープ
            _warpControl.TargetWarp(player_warp_target);
            _ExecuteWarpDash();

            // オート攻撃実行
            if (attack_warp_target != null) {
                ability.ExecuteAutoAttack(attack_warp_target, target_enemy.transform.position);
            }
        }
    }

    /// <summary>
    /// 装備済みの能力を型で取得（ジェネリック）
    /// </summary>
    /// <typeparam name="T">取得したい能力の型</typeparam>
    /// <returns>見つかった能力、なければnull</returns>
    private T _GetAbility<T>() where T : Ability_Base {
        if (_abilityY is T abilityY) return abilityY;
        if (_abilityX is T abilityX) return abilityX;
        if (_abilityA is T abilityA) return abilityA;
        return null;
    }

    /// <summary>
    /// 特定の能力が装備されているか確認
    /// </summary>
    /// <typeparam name="T">確認したい能力の型</typeparam>
    /// <returns>装備されていればtrue</returns>
    private bool _HasAbility<T>() where T : Ability_Base {
        return _GetAbility<T>() != null;
    }
}
