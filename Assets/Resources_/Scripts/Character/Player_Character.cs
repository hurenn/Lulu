using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーキャラクター（ルル）
/// </summary>
public class Player_Character : Character_Base {
    private const float _SPECIAL_GAGE_DAMAGE_RATE = 0.1f; // ダメージによる必殺技ゲージ増加率

    private CharacterParameter_Player _player_charaParam => _charaParam as CharacterParameter_Player;
    public CharacterParameter_Player PlayerCharaParam => _player_charaParam;

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
    // ダッシュ終了直後の逆方向ダッシュ受付タイマー
    private float _dashEndTimer = 0f;
    // 上下ワープダッシュフラグ（横ズレ許可判定用）
    private bool _isVerticalWarpDash = false;
    // 下方向ワープフラグ（時間終了無効化用）
    private bool _isDownwardWarpDash = false;

    private bool _isAvoid = false;
    private float _isAvoidTimer = 0.01f;
    private float _currentAvoidTime = 0f;

    // 能力スロット（要素の並びはeAbilitySlot(Y,X,A,B)の順に対応）
    [SerializeField] protected Ability_Base[] _abilities = new Ability_Base[4];
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

    // 起動時に固定で設定する能力（デフォルトはBスロットのワープ）
    [SerializeField] private eAbilityType _lockedSlotAbility = eAbilityType.Warp;
    [SerializeField] private eAbilitySlot _lockedSlot = eAbilitySlot.B;

    protected override void _Setup() {
        base._Setup();
        if (_warpControl != null) {
            _warpControl.Setup(_OnPreWarpCommon, _OnWarpEndCommon);
        }
        _playerParam = PlayerParameter.Instance;
        ApplyPlayerParameter();
        _SetupHadAbility();

        // 固定スロットに常に指定の能力を設定
        SetAbilitySlot(_lockedSlotAbility, _lockedSlot, false);
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

        // 自動発光設定
        _SetAutoLight(_isWarpDashing);

        // 氷無敵時間計測
        if (_isIceInvincible) {
            // コインワープ連鎖などのワープ判定中は消費せず、ダッシュ開始時まで温存する
            if (!_isWarpChecking) {
                // 無敵保障時間計測
                _currentIceInvincibleTime -= Time.fixedDeltaTime;
            }
            if (_currentIceInvincibleTime <= 0) {
                if (!_isWarpDashing) {
                    // 無敵終了
                    _isIceInvincible = false;
                }
            }
        }

        foreach (var ability in _abilities) {
            ability?.UpdateParameter(_isRight, transform, _param, _player_charaParam, _warpControl, _motorStates);
        }

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
            _SetWarpDashing(false);
            _OnExecuteIceAutoAttack(target_enemy);
            return;
        }

        // ワープダッシュの最大時間を超えた場合は終了（下方向ワープは時間終了なし）
        if (!_isDownwardWarpDash && _currentWarpDashTime > _param.maxWarpDashTime) {
            _SetWarpDashing(false);
            return;
        }
        _currentWarpDashTime += Time.deltaTime;

        // 方向入力による慣性制御
        if (_inputData.move != Vector2.zero && _warpDashDirection.sqrMagnitude > 0f) {
            Vector2 dashDir = _warpDashDirection.normalized;
            Vector2 input = _inputData.move;

            // 横方向ワープ中のみ加速・減速
            float speed = _warpDashDirection.magnitude;
            if (!_isVerticalWarpDash) {
                float axialInput = Vector2.Dot(input, dashDir);
                float speedDelta = axialInput > 0f
                    ? axialInput * _param.warpDashControlAccel
                    : axialInput * _param.warpDashControlDecel;
                speed = Mathf.Max(speed + speedDelta * Time.deltaTime, 0f);
            }
            _warpDashDirection = dashDir * speed;

            // 上下ワープ中のみ左右入力で横方向ズレ
            if (_isVerticalWarpDash) {
                _warpDashDirection.x += input.x * _param.warpDashControlSteer * Time.deltaTime;
            }
        }

        // ワープダッシュ移動
        var dash_velocity = _warpDashDirection;
        _rb.linearVelocity = dash_velocity;
        // ワープダッシュ力を減衰させる（下方向ワープは減衰なし）
        if (!_isDownwardWarpDash) {
            _warpDashDirection *= _param.warpDashDamping;
            if (_warpDashDirection.magnitude < 0.2f) {
                _warpDashDirection = Vector2.zero;
            }
        }

        // 地面に接触しているかチェック
        if (_isGrounded) {
            if (!_isVerticalWarpDash) {
                // 斜めワープの場合はスライディングを実行
                _ExecuteSlide(); // スライディング実行
            } else {
                // 上下ワープの場合は張り付き状態に移行
                _isGroundSticking = true;
                _currentLandingDashTime = 0;
            }
            _player_charaParam.OnRecoverOverheat(); // オーバーヒート回復
            _SetWarpDashing(false);
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
        _SetDash(_isRight, is_effect: false);
        _SetSliding(true);
        _currentSlideTime = 0;
    }
    /// <summary>
    /// ダッシュ実行
    /// </summary>
    private void _SetDash(bool is_right, bool enable = true, bool is_effect = true) {
        _isDashing = enable;
        _anim?.SetBool("Dash", enable);
        if (is_effect && enable && _isGrounded && _dashEffect != null) {
            var footPos = transform.position + Vector3.down * (GetCharacterSize().y / 2f);
            EffectPool.Instance.Spawn(_dashEffect, footPos, !is_right);
        }
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
        _SetSlidingJump(false); // スライディングジャンプ終了
        _SetWarpDashing(false);
    }

    /// <summary>
    /// ワープダッシュ実行
    /// </summary>
    private void _ExecuteWarpDash() {
        _isVerticalWarpDash = Mathf.Approximately(_warpDashDirection.x, 0f);
        _isDownwardWarpDash = _isVerticalWarpDash && _warpDashDirection.y < 0f;
        _SetDash(_isRight, is_effect: false);
        _SetWarpDashing(true);
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
            // 横移動速度が歩行速度以下になった場合はスライディング終了
            if (Mathf.Abs(velocity.x) <= _param.moveSpeed) {
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
            _SetSlidingJump(false); // スライディングジャンプ終了
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
        if (_isDead || _specialUsing) {
            input.abilityX.held = false;
            input.abilityX.pressed = false;
            input.abilityY.held = false;
            input.abilityY.pressed = false;
            input.abilityA.held = false;
            input.abilityA.pressed = false;
            input.abilityB.held = false;
            input.abilityB.pressed = false;
        }
        _inputData = input;
        for (int i = 0; i < _abilities.Length; i++) {
            eAbilitySlot slot = (eAbilitySlot)i;
            var button = input.GetAbilityButton(slot);
            _UpdateAbility(_abilities[i], input.move, button.pressed, button.held, button.released, input);
        }
        input = _inputData;

        base.UpdateControl(input);

        _Warp();
    }

    /// <summary>
    /// 能力スロットにセット
    /// </summary>
    public void SetAbilitySlot(eAbilityType ability_type, eAbilitySlot ability_slot, bool is_effect = true) {
        var ability = AbilityFactory.CreateAbility(ability_type, ability_slot,
            (special_anim) => PlayAnim(special_anim),
            () => _AbilityResult(eAbilityResult.SpecialEnd, _inputData.move), is_effect);

        // スロットにセット
        _abilities[(int)ability_slot] = ability;

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
            _AbilityResult(ability.ExecuteRelease(), dir_input);
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
                _SetWarpDashing(false);
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
                _SetEndSpecialTrigger();
                break;
            case eAbilityResult.Jump:
                // ジャンプボタンフラグを立てる
                _inputData.isJumpPressed = true;
                _inputData.isJumpHeld = true;
                break;
            case eAbilityResult.JumpHeld:
                // ジャンプボタンフラグを立てる
                _inputData.isJumpHeld = true;
                break;
            case eAbilityResult.JumpRelease:
                // ジャンプボタンフラグを下ろす
                _inputData.isJumpReleased = true;
                break;
            default:
                break;
        }

        // 炎自動攻撃間隔リセット
        if (result != eAbilityResult.LightDome &&
            result != eAbilityResult.JumpHeld &&
            result != eAbilityResult.JumpRelease &&
            result != eAbilityResult.Jump) {
            _OnResetFireInterval();
        } else if (result == eAbilityResult.Jump && !_isGrounded) {
            // ワープ時も呼び出す
            _OnResetFireInterval();
        }
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false) {
        if (isInvincible || _isDead || _specialUsing) {
            return false;
        }

        // 氷の能力で無敵（仮対応）。トラップなどのダメージは貫通させる
        if (_isIceInvincible && !is_trap_damage) {
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
                }, _inputData.move));
            return false;
        }

        if (_isSliding || _isEventInvincible) {
            return false; // ダメージ無効
        }

        _SetWarpDashing(false);
        _SetSliding(false);
        _SetSlidingJump(false); // スライディングジャンプ終了
        _SetWallDash(false);
        _isGroundSticking = false; // 地面張り付き状態終了
        _isJumping = false; // ジャンプ終了

        if (damage > 0) {
            // 必殺技チャージ
            foreach (var ability in _abilities) {
                ability?.AddSpecialCharge(damage * _SPECIAL_GAGE_DAMAGE_RATE);
            }
        }

        bool isDamaged = base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time, is_trap_damage);
        if (isDamaged) {
            ScreenFlash.Instance?.Flash(0.4f, new Color(1f, 0f, 0f, 0.2f));
        }
        return isDamaged;
    }

    protected override IEnumerator Die() {
        yield return base.Die();

        yield return new WaitForSeconds(2.0f);

        Instantiate(_warpEffectPrefab, transform.position, Quaternion.identity);
        _sprite.enabled = false;

        yield return new WaitForSeconds(1.0f);

        // ステージリトライ（死亡によるリトライなので必殺チャージ50%付与）
        GameSceneManager.Instance.StageRestart(false, true);
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
            // 下入力で即座に壁上りを終了
            if (_inputData.move.y < -0.5f) {
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
        if (_isSlidingCanceling && _inputData.isJumpPressed) {
            _isSlidingCanceling = false; // スライディングキャンセル終了
        }

        // 地面張り付き状態の入力
        if (_isGroundSticking) {
            if (_inputData.move.x != 0) {
                // 張り付き状態で移動入力があれば張り付き状態を解除
                _isGroundSticking = false;
                _warpDashDirection = _inputData.move.x > 0 ? _param.warpDashDownRight : _param.warpDashDownLeft;
                _SetDash(_inputData.move.x > 0); // ダッシュ実行
            } else if (_inputData.isJumpPressed) {
                // 張り付き状態でジャンプ入力があればジャンプ
                _isGroundSticking = false;
            }
        }

        if (!_CanMove) {
            return;
        }

        // ジャンプ
        if (_inputData.isJumpPressed && _isGrounded && !(_inputData.move.y < -0.5f && _inputData.move.x == 0)) {
            // スライディングジャンプ
            if (_isSliding) {
                _SetSliding(false);
                _SetSlidingJump(true);

                // y方向の加速を無視
                _warpDashDirection.y = 0;

                // スライディング時間リセット
                _currentSlideTime = 0;
                _currentSlideJumpTime = 0;
            }
            // ダッシュジャンプをスライディングジャンプと統合（壁ダッシュ判定を共有）
            else if (_isDashing) {
                _SetSlidingJump(true);
                _warpDashDirection = new Vector2(_isRight ? 1f : -1f, 0f);
                _currentSlideJumpTime = 0;
            }

            velocity.y = _jumpForce;
            _currentJumpTime = _param.maxJumpHoldTime;
            _isJumping = true;
            _anim?.SetBool("Jump", true);
            _anim?.Play("Jump");
            // ジャンプエフェクト生成
            if (_jumpEffect != null) {
                var footPos = transform.position + Vector3.down * (GetCharacterSize().y / 2f);
                EffectPool.Instance.Spawn(_jumpEffect, footPos);
            }
            if (_seJump != null) {
                _audioSource?.PlayOneShot(_seJump);
            }
        }
        // ジャンプリリース
        if ((!_inputData.isJumpHeld && _isJumping) || _currentJumpTime <= 0) {
            _isJumping = false;
        }
        // 長押しジャンプ
        if (_inputData.isJumpHeld && _isJumping) {
            velocity.y = _jumpForce;
            _currentJumpTime -= Time.deltaTime;
        }

        //状態に応じたフラグ管理
        if (_anim != null && _anim.isActiveAndEnabled) {
            _anim.SetBool("Jump", _isJumping);
            _anim.SetBool("Fall", !_isGrounded);
        }

        // 移動入力
        if (_inputData.move.x != 0) {
            // 直前まで入力なし
            if (!_isWalking) {
                // 同じ方向にすぐ再入力でダッシュ
                bool isOppositeDir = _lastWalkDirection.x != 0 &&
                    Mathf.Sign(_inputData.move.x) != Mathf.Sign(_lastWalkDirection.x);
                if ((_currentStopMoveInputTime < _param.dashInputThreshold && (
                        (Mathf.Sign(_inputData.move.x) == Mathf.Sign(_lastWalkDirection.x) && !_isDashing) ||
                        (isOppositeDir && _isDashing))) ||
                    (isOppositeDir && _dashEndTimer > 0)) {
                    _SetDash(_inputData.move.x > 0); // ダッシュ実行
                }
                _isWalking = true;
                _anim?.SetBool("Walk", true);
            }

            // 移動中は常にフラグリセット
            _lastWalkDirection = _inputData.move;
            _currentStopMoveInputTime = 0;
            _dashEndTimer = 0;
        } else // 入力停止
          {
            if (_isWalking) {
                // 歩行から停止
                _isWalking = false;
                _currentStopMoveInputTime = 0;
                //移動アニメーション停止
                _anim?.SetBool("Walk", false);
                if (_isDashing) _dashEndTimer = _param.dashInputThreshold;
                _SetDash(_isRight, false);
            } else {
                // 停止中はタイマー更新
                _currentStopMoveInputTime += Time.deltaTime;
                if (_dashEndTimer > 0) _dashEndTimer -= Time.deltaTime;
                if (_currentStopMoveInputTime > _param.dashInputThreshold) {
                    _SetDash(_isRight, false);
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
        if ((!_isGrounded && _inputData.isJumpPressed) ||
            (_isGrounded && _inputData.move.y < -0.5f && _inputData.move.x == 0 && _inputData.isJumpPressed)) {
            // エフェクト生成
            Instantiate(_warpEffectPrefab, transform.position, Quaternion.identity);

            if (_inputData.move.magnitude == 0 && !_warpControl.GetCoinWarpCheck().HasValue) {
                // 入力が無く、コインワープもできない場合はワープしない
                return;
            }

            // ワープ処理開始
            StartCoroutine(WarpStart());
        }

        IEnumerator WarpStart() {
            // MP消費
            var is_success = _player_charaParam.ConsumeMP(eAbilityType.WarpExecute);

            if (!is_success) {
                yield break; // 失敗
            }
            if(_GetAbility<Ability_Ice>() != null) {
                // 氷無敵付与
                _isIceInvincible = true;
            }
            _currentIceInvincibleTime = _iceInvincibleTime;

            WarpControl.eWarpDirection dash_direction = _warpDirection;

            _isWarpChecking = true;
            if (_inputData.move.magnitude != 0) {
                // 入力方向にワープ
                yield return _warpControl.DirectionWarp(_warpDirection, _OnExecuteIceAutoAttack);
            } else if (_warpControl.GetCoinWarpCheck().HasValue) {
                // コインワープ(方向入力なしでコインワープ可能な場合)
                yield return _warpControl.CoinWarp();
            }
            _isWarpChecking = false;

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
        _SetSlidingJump(false);
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
    /// オート発光開始
    /// </summary>
    private void _SetAutoLight(bool enable) {
        var ability = _GetAbility<Ability_Light>();
        if (ability != null) {
            ability.SetAutoLight(enable);
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
        foreach (var ability in _abilities) {
            if (ability is T typed) return typed;
        }
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

    /// <summary>
    /// スロットの参照をクリア
    /// </summary>
    /// <param name="slot">クリアするスロット</param>
    public void ClearAbilitySlotReference(eAbilitySlot slot) {
        // スロットの参照をnullに設定
        _abilities[(int)slot] = null;

        // 一時保存用Dictionaryからも削除
        if (_tmpAbilitySlot != null) {
            eAbilityType targetType = eAbilityType.None;
            foreach (var kvp in _tmpAbilitySlot) {
                if (kvp.Value == slot) {
                    targetType = kvp.Key;
                    break;
                }
            }
            if (targetType != eAbilityType.None) {
                _tmpAbilitySlot.Remove(targetType);
            }
        }
    }

    /// <summary>
    /// 二つのスロットの能力を入れ替える
    /// </summary>
    /// <param name="slotA">スロットA</param>
    /// <param name="slotB">スロットB</param>
    public void SwapAbilitySlot(eAbilitySlot slotA, eAbilitySlot slotB) {
        // 同じスロットを指定した場合は何もしない
        if (slotA == slotB) {
            Debug.LogWarning("同じスロットを指定しています");
            return;
        }

        // スロットAとスロットBの能力を取得
        Ability_Base abilityA = _abilities[(int)slotA];
        Ability_Base abilityB = _abilities[(int)slotB];

        // スロットAとスロットBの能力タイプを取得
        eAbilityType abilityTypeA = eAbilityType.None;
        eAbilityType abilityTypeB = eAbilityType.None;

        foreach (var kvp in _tmpAbilitySlot) {
            if (kvp.Value == slotA) {
                abilityTypeA = kvp.Key;
            }
            if (kvp.Value == slotB) {
                abilityTypeB = kvp.Key;
            }
        }

        // 一時保存用Dictionaryを更新
        if (abilityTypeA != eAbilityType.None) {
            _tmpAbilitySlot[abilityTypeA] = slotB;
        }
        if (abilityTypeB != eAbilityType.None) {
            _tmpAbilitySlot[abilityTypeB] = slotA;
        }

        // スロットの参照を入れ替え
        _abilities[(int)slotA] = abilityB;
        _abilities[(int)slotB] = abilityA;

        Debug.Log($"スロット{slotA}({abilityTypeA})とスロット{slotB}({abilityTypeB})を入れ替えました");
    }

    /// <summary>
    /// 能力を削除する（互換性のために残す）
    /// </summary>
    /// <param name="slot">外すスロット</param>
    public void RemoveAbility(eAbilitySlot slot) {
        var ability = _abilities[(int)slot];

        AbilityFactory.DestroyAbility(ability, slot);

        ClearAbilitySlotReference(slot);
    }

    // プレイヤーパラメータのスナップショットを取得
    public PlayerParameterSnapshot GetParameterSnapshot() {
        var snapshot = new PlayerParameterSnapshot();
        if (_player_charaParam != null) {
            snapshot.Lv = _playerParam.levelParameter.hpLevel; // レベルを保存
        }
        return snapshot;
    }

    // プレイヤーパラメータをスナップショットから復元
    public void RestoreParameter(PlayerParameterSnapshot snapshot) {
        if (_playerParam != null && snapshot != null) {
            // レベルを復元（SetLevel等がなければ直接代入やAddExpで調整）
            _playerParam.levelParameter.hpLevel = snapshot.Lv;
            _playerParam.levelParameter.mpLevel = snapshot.Lv;
            _playerParam.levelParameter.attackLevel = snapshot.Lv;
            ApplyPlayerParameter();
        }
    }

    // パラメータとアビリティスロットをまとめて保存する
    public void SavePlayerState()
    {
        var snapshot = GetParameterSnapshot();
        var checkpointManager = CheckpointManager.Instance;
        if (checkpointManager != null && snapshot != null)
        {
            // 空のリストを渡す（Checkpoint以外から呼ばれる場合）
            checkpointManager.SaveCheckpoint(transform.position, snapshot, new List<string>(), new List<string>());
        }
        else
        {
            Debug.LogError("CheckpointManager or PlayerParameterSnapshot is null.");
        }
        SaveAbilitySlot();
    }

    protected override void _SetWarpDashing(bool is_warp_dashing) {
        base._SetWarpDashing(is_warp_dashing);
    }
}
