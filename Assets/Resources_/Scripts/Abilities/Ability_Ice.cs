using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Ability_Ice : Ability_Base {

    private int _attackStep = 0; // 0:未攻撃, 1:1段目, 2:2段目, 3:3段目

    private float _currentComboTime = 0f;
    // コンボ攻撃のクールタイム
    private float _currentComboCoolTime = 0f;

    // 長押し判定時間
    private float _longPressThreshold = 0.3f;
    private float _pressHoldTime = 0f;

    // 長押し実行済みフラグ
    private bool _isHoldExecuted = false;
    // 攻撃方向ロックフラグ
    private bool _isAttackDirectionLocked = false;
    private bool _isEffectRight = true; // 攻撃エフェクトを出す向き(攻撃方向がロックされている間は更新しない)

    // ヒットストップ管理
    [SerializeField] private LocalTimePause _localTimeController;
    
    [SerializeField] private GameObject _slash1;
    [SerializeField] private GameObject _slash2;
    [SerializeField] private GameObject _slash3;
    [SerializeField] private GameObject _lockonSlash;
    private IEnumerator _separateRoutine = null;

    [Header("必殺技:最初の一撃(正面へ飛ばし、敵に当たったら連続攻撃へ移る)")]
    [SerializeField] private GameObject _specialFirstAttackPrefab;
    [SerializeField] private float _specialFirstAttackSpeed = 30.0f;
    [SerializeField] private float _specialFirstAttackTime = 0.5f;   // 飛んでいる時間(この間に敵に当たらなければ失敗)
    [SerializeField] private int _specialFirstAttackDamage = 2;
    [SerializeField] private float _specialFirstAttackHitStopTime = 0.3f; // 当たった時に演出を止める時間
    [SerializeField] private float _specialFirstAttackPassTime = 0.1f;   // ヒットストップの後、敵を通り抜けてから消えるまでの時間
    [SerializeField] private float _specialFirstAttackWait = 0.5f;  // ヒットストップが終わってから連続攻撃を始めるまでの時間
    [SerializeField] private float _specialMissRemainCharge = 0.5f; // 敵に当たらず失敗した時に残す必殺技ゲージの割合

    [Header("必殺技:連続攻撃(プレイヤーと同じ姿のため、序盤は同時に1体しか出さない)")]
    [SerializeField] private GameObject _specialAttackPrefab;
    [SerializeField] private float _specialAttackDuration = 3.0f;      // 連続攻撃を生成し続ける時間
    [SerializeField] private int _specialAttackHitCount = 30;          // 連続攻撃でダメージを与える回数
    [SerializeField] private int _specialAttackDamage = 2;
    [SerializeField] private float _specialAttackIntervalRate = 0.1f;  // 最後の生成間隔÷最初の生成間隔(小さいほど後半の生成が速くなる)
    [SerializeField] private float _specialAttackOverlapTime = 1.5f;   // この時間を超えたら、同時に2体以上表示してよい
    [SerializeField] private float _specialAttackDistance = 9.0f;      // 敵の中心から出現位置までの距離(反対側の同じ距離まで通過する)
    [SerializeField] private float _specialAttackDashTime = 0.12f;     // 出現位置から反対側まで通過する時間
    [SerializeField] private float _specialAttackAngleRange = 60.0f;   // 通過する向きの、水平からの角度のばらつき
    [SerializeField] private float _specialAttackMinAngleChange = 40.0f; // 直前の攻撃と、通過する向きを最低これだけ変える
    [SerializeField] private float _specialAttackDistanceRandom = 0.35f; // 出現位置・消える位置までの距離のばらつき(割合)
    [SerializeField] private float _specialAttackDashTimeRandom = 0.3f;  // 通過する時間のばらつき(割合)
    [SerializeField] private float _specialAttackPassOffset = 0.8f;      // 通過する位置を、敵の中心からずらす最大の距離
    [SerializeField] private float _specialAttackHitStopTime = 0.05f;    // ヒットした時に演出を止める時間
    [SerializeField] private float _specialAttackHitShake = 0.15f;       // ヒットした時のカメラの揺れ
    [SerializeField] private float _specialAttackHitShakeTime = 0.08f;
    [SerializeField] private int _specialAttackGhostCount = 3;           // 通過中に置いていく薄い残像の数
    [SerializeField] private float _specialAttackGhostAlpha = 0.5f;      // 通過中の残像の濃さ(ワープの残像の濃さに掛ける)
    [SerializeField] private float _specialAttackGhostTime = 0.5f;       // 通過中の残像が消えるまでの時間(ワープの残像の時間に掛ける)

    [Header("必殺技:最後の一撃")]
    [SerializeField] private float _specialFinishChargeTime = 2.0f;    // 力を溜める時間
    [SerializeField] private float _specialFinishDistance = 10.0f;     // 敵の中心から力を溜める位置までの距離
    [SerializeField] private float _specialFinishDashTime = 0.15f;
    [SerializeField] private int _specialFinishDamage = 30;
    [SerializeField] private Color _specialFinishChargeColor = new Color(0.55f, 0.85f, 1.0f, 1.0f);
    [SerializeField] private Sprite _specialFinishImage;               // 最後の一撃が当たった瞬間に表示する一枚絵
    [SerializeField] private float _specialFinishImageTime = 2.0f;
    [SerializeField] private float _specialFinishImageFadeTime = 0.1f;   // 一枚絵の表示・非表示の拡縮フェードの時間
    [SerializeField] private float _specialFinishChargeShake = 0.08f;   // 力を溜めている間のカメラの揺れ(小さく揺らし続ける)
    [SerializeField] private float _specialFinishHitShake = 1.5f;       // 最後の一撃が当たった時のカメラの揺れ
    [SerializeField] private float _specialFinishImageShake = 40.0f;    // 最後の一撃が当たった時の一枚絵の揺れ(画面の高さ1080の時のピクセル数)
    [SerializeField] private float _specialFinishHitShakeTime = 0.4f;

    [Header("必殺技:カメラ(映す範囲の高さ。ワールド座標の長さ)")]
    [SerializeField] private float _specialCutInZoomHeight = 7.0f;      // カットイン中、プレイヤーを映す
    [SerializeField] private float _specialRushZoomHeight = 10.0f;      // 連続攻撃中、対象の敵を映す
    [SerializeField] private float _specialFinishZoomMinHeight = 8.0f;  // 最後の一撃中、敵と突撃する姿の両方を映す(最小の高さ)
    [SerializeField] private float _specialFinishZoomMargin = 6.0f;     // 敵と突撃する姿の間の長さに足す余白
    [SerializeField] private float _specialZoomSpeed = 8.0f;            // 映す範囲を変える速さ

    [Header("必殺技:効果音")]
    [SerializeField] private AudioClip _seSpecialHit;                  // 連続攻撃のヒット音
    [SerializeField] private float _seSpecialHitVolume = 1.0f;
    [SerializeField] private AudioClip[] _seSpecialFinishHit;          // 最後の一撃のヒット音(同時に鳴らす)
    [SerializeField] private float _seSpecialFinishHitVolume = 1.0f;
    [SerializeField] private AudioClip _seSpecialCharge;               // 最後の一撃の溜め中に鳴らす音
    [SerializeField] private float _seSpecialChargeVolume = 1.0f;

    [Header("必殺技:BGM(連続攻撃中に徐々に小さくし、操作できるようになったら元に戻す)")]
    [SerializeField] private float _specialBgmVolumeRate = 0.2f;       // 小さくした時の音量(元の音量に掛ける)

    [Header("必殺技:終了後")]
    [SerializeField] private float _specialEndInvincibleTime = 3.0f;   // 終了後にプレイヤーを無敵にする時間

    // 必殺技の演出中に作ったもの(能力が途中で破棄された時に片付ける)
    private Enemy_Base _specialTarget = null;
    private GameObject _specialFinishImageObj = null;
    private bool _isSpecialTimeStopped = false;
    // 時間を止めている間も動かすため、実時間で動くように変えたもの(終わったら元に戻す)
    private readonly System.Collections.Generic.Dictionary<Animator, AnimatorUpdateMode> _unscaledAnimators = new System.Collections.Generic.Dictionary<Animator, AnimatorUpdateMode>();
    private readonly System.Collections.Generic.List<ParticleSystem> _unscaledParticles = new System.Collections.Generic.List<ParticleSystem>();
    private bool _cameraIgnoreTimeScale = false;
    // 必殺技のカメラ(映す中心を毎フレーム取得し、映す範囲の高さを少しずつ目標に近づける)
    private System.Func<Vector2> _specialZoomCenter = null;
    private Transform _specialCameraFocus = null;
    private float _specialZoomHeight = 0f;
    private float _specialZoomTargetHeight = 0f;
    // BGMの音量(必殺技の終了時に元に戻す)
    private AudioSource _specialBgm = null;
    private float _specialBgmVolume = 0f;
    // 溜め中に鳴らしている音
    private AudioSource _specialChargeSe = null;
    private bool _impulseIgnoreTimeScale = false;
    // ヒットストップが終わる時刻(実時間)
    private float _specialHitStopEndTime = 0f;
    private readonly System.Collections.Generic.List<GameObject> _specialObjects = new System.Collections.Generic.List<GameObject>();

    protected override void _Update() {
        // タイマー減少
        if (_attackStep > 0) {
            _currentComboTime -= Time.deltaTime;
            if (_currentComboTime <= 0f) {
                _attackStep = 0; // コンボリセット
            }
        }
        // コンボタイマー
        if (_currentComboCoolTime > 0f) {
            _currentComboCoolTime -= Time.deltaTime;
        }
        // 必殺技のカメラ
        _UpdateSpecialZoom();
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cannotUse) {
            UpdatePartnerTransform(); // 位置更新
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }
        // 切り離し攻撃終了
        _EndSeparate();

        // 必殺技チャージ停止
        _StopSpecialCharge();

        // 必殺技発動
        if (_isSpecialCharged && !_motorStates.isWarpDashing && !_motorStates.isSliding) {
            _UseSpecial();
            return eAbilityResult.IceSpecial;
        }

        // ロックオン攻撃判定
        if (LockonManager.Instance.HasTarget) {
            if (!_charaParam.isOverheat) {
                // ロックオン中の攻撃

                // ロックオン対象の方向を向く
                var lockon = LockonManager.Instance;
                Vector3 to_target = lockon.targetTransform.position - _playerTransform.position;
                to_target.y = 0; // 水平成分のみ

                // ワープチェッカー取得
                WarpChecker warp_checker = null;
                if (to_target.x > 0) {
                    warp_checker = lockon.GetTargetWarpPos(WarpControl.eWarpDirection.Left);
                } else if (to_target.x < 0) {
                    warp_checker = lockon.GetTargetWarpPos(WarpControl.eWarpDirection.Right);
                }

                // ロックオン攻撃実行
                if (warp_checker != null) {
                    _LockonSlash(warp_checker, lockon.targetTransform.position);
                    return eAbilityResult.IceLockonSlash;
                }
            } else {
                // オーバーヒート中は使用不可
                UpdatePartnerTransform(); // 位置更新
                Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            }
        }

        // コンボ攻撃判定
        var attack_result = _ComboSlash();
        // 切り離し攻撃判定
        // if(attack_result == eAbilityResult.None) attack_result = _SeparateSlash();

        // 攻撃実行
        if (attack_result != eAbilityResult.None) {
            _AppearCheck(eAbilityType.Ice);
            return attack_result;
        }

        return eAbilityResult.None;
    }

    /// <summary>
    /// ロックオン攻撃実行
    /// </summary>
    private void _LockonSlash(WarpChecker warp_checker, Vector3 target_pos) {
        if (_lockonSlash == null) {
            Debug.Log("ロックオン攻撃エフェクトが見つかりません");
            return;
        }
        Debug.Log("Lockon Slash");

        IEnumerator attack_routine() {
            yield return _warpControl?.TargetWarp(warp_checker);

            _OnceAttack(target_pos, warp_checker, eAbilityType.LockonSlash);
        }
        StartCoroutine(attack_routine());
    }

    /// <summary>
    /// コンボ攻撃実行
    /// </summary>
    private eAbilityResult _ComboSlash() {

        // クールタイム中は実行不可
        if (_currentComboCoolTime > 0f) {
            return eAbilityResult.None;
        }

        if (_attackStep == 0) {
            // 1段目
            Debug.Log("Slash 1");
            StartCoroutine(_UpdateTransformEasing(_slash1, "Node_Attack1"));
            _SetComboStep(1);
            return eAbilityResult.IceSlash1;            // 実行結果返却
        } else if (_attackStep == 1) {
            // 2段目
            Debug.Log("Slash 2");
            StartCoroutine(_UpdateTransformEasing(_slash2, "Node_Attack2"));
            _SetComboStep(2);
            return eAbilityResult.IceSlash2;            // 実行結果返却
        } else if (_attackStep == 2) {
            // 3段目
            Debug.Log("Slash 3");
            StartCoroutine(_UpdateTransformEasing(_slash3, "Node_Attack3"));
            _SetComboStep(0); // コンボリセット
            return eAbilityResult.IceSlash3;            // 実行結果返却
        }

        return eAbilityResult.None;
    }

    /// <summary>
    /// コンボ段階更新
    /// </summary>
    private void _SetComboStep(int step) {
        _attackStep = step;
        _currentComboTime = _param.comboReceptionTime;    // 次のコンボ受付時間
        _currentComboCoolTime = step == 0 ? _param.comboCoolTime : _param.comboIntervalTime;     // クールタイムセット
    }

    private IEnumerator _UpdateTransformEasing(GameObject effect, string anim_name) {
        if (_isAppearing) {
            // イージングで位置を更新
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = _playerTransform.position + new Vector3(
                _localPosition.x * (_isRight ? -1 : 1),
                _localPosition.y,
                _localPosition.z);
            while (elapsedTime < _param.moveDuration) {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / _param.moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        UpdatePartnerTransform();   // キャラクター位置を更新

        var effect_obj = _SpawnAttackEffect(effect);  // エフェクト生成
        if (effect_obj == null) yield break;
        var damage_zone = effect_obj.GetComponentInChildren<DamageZone>();
        damage_zone?.AddHitStopTarget(_localTimeController); // ヒットストップ対象に追加

        _anim?.Play(anim_name, 0, 0.0f);   // アニメーション再生
    }

    /// <summary>
    /// 切り離し連続攻撃
    /// </summary>
    private IEnumerator _SeparateRoutine() {
        _anim?.SetBool("SeparateEnd", false);
        _anim?.Play("Node_SeparateAttack", 0, 0.0f);   // アニメーション再生
        _isHoldExecuted = true;
        _isAttackDirectionLocked = true; // 攻撃方向ロック
        _canForceReturn = false;      // 強制帰還無効化
        _attackStep = 0; // コンボリセット
        UpdatePartnerTransform();   // キャラクター位置を更新

        float separate_time = 5.5f;
        float current_time = 0f;
        float attack_delay = 0.3f; // 攻撃エフェクト発生までの遅延時間
        float current_delay = 0f;
        _ResetReturnTimer(separate_time); // 帰還タイマーリセット
        while (current_time < separate_time) {
            current_time += Time.deltaTime;
            if (current_delay < attack_delay) {
                current_delay += Time.deltaTime;
            } else {
                // 一定時間ごとに攻撃エフェクト発生
                current_delay = 0f;
                _SpawnAttackEffect(_slash1);  // エフェクト生成
            }
            yield return null;
        }

        yield return new WaitForSeconds(separate_time);
        _anim?.SetBool("SeparateEnd", true);
    }

    // 切り離し攻撃終了
    private void _EndSeparate() {
        // 実行中の連続攻撃コルーチンがあれば停止
        if (_separateRoutine != null) {
            StopCoroutine(_separateRoutine);
            _separateRoutine = null;
        }
        _isAttackDirectionLocked = false;
        _canForceReturn = true;      // 強制帰還有効化

        _anim?.SetBool("SeparateEnd", true);
    }

    public override eAbilityResult ExecuteLong() {
        return eAbilityResult.None;
        // 切り離し
        if (!_isHoldExecuted) {
            _pressHoldTime += Time.deltaTime;
            if (_pressHoldTime >= _longPressThreshold) {
                _EndSeparate();

                Debug.Log("Ice Separate");
                _separateRoutine = _SeparateRoutine();
                StartCoroutine(_separateRoutine);
                return eAbilityResult.IceSeparate;
            }
        }
    }

    public override eAbilityResult ExecuteRelease() {
        _pressHoldTime = 0f;
        _isHoldExecuted = false;
        return eAbilityResult.None;
    }

    /// <summary>
    /// 自動攻撃実行
    /// </summary>
    public void ExecuteAutoAttack(WarpChecker warp_checker, Vector3 enemy_pos) {
        _AutoSlash(warp_checker, enemy_pos);
    }

    /// <summary>
    /// 自動攻撃
    /// </summary>
    private void _AutoSlash(WarpChecker warp_checker, Vector3 enemy_pos) {
        if (_lockonSlash == null) {
            Debug.Log("ロックオン攻撃エフェクトが見つかりません");
            return;
        }
        Debug.Log("Lockon Slash");

        _OnceAttack(enemy_pos, warp_checker, eAbilityType.AutoSlash);
    }

    /// <summary>
    /// 一撃攻撃
    /// </summary>
    /// <param name="target_pos">敵の位置</param>
    private void _OnceAttack(Vector3 target_pos, WarpChecker warp_checker, eAbilityType ability_type) {
        if(_warpControl == null || warp_checker == null) {
            Debug.Log("ワープコントロールまたはワープチェッカーが見つかりません");
            return;
        }

        // フラッシュ
        Color flash_color = Color.white;
        flash_color.a = 0.5f;
        var flash = ScreenFlash.Instance;
        flash?.Flash(color: flash_color);

        // MP消費
        _AppearCheck(ability_type, force_appear: true);

        // キャラクター位置を更新
        UpdatePartnerTransform(warp_checker.transform.position);
        var is_right = target_pos.x > transform.position.x;
        _isRight = is_right;

        // エフェクトの向きを調整して生成
        _UpdateEffectDirection();
        _SpawnAttackEffect(_lockonSlash);
        // アニメーション再生
        _anim?.Play("Node_Attack2", 0, 0.0f);

        // コンボ1段目をスキップ
        _SetComboStep(1);
    }

    public override void UpdateParameter(bool is_right, Transform chara_transform, CommonParameter param, CharacterParameter_Player chara_param, WarpControl warp_control, MotorStates motor_states) {
        base.UpdateParameter(is_right, chara_transform, param, chara_param, warp_control, motor_states);
        // 向きに応じて攻撃エフェクトを出す向きを更新
        _UpdateEffectDirection();
    }

    /// <summary>
    /// 攻撃エフェクトを出す向きを更新する(攻撃方向がロックされている間は前の向きのまま)
    /// </summary>
    private void _UpdateEffectDirection() {
        if (!_isAttackDirectionLocked) {
            _isEffectRight = _isRight;
        }
    }

    /// <summary>
    /// 攻撃エフェクトを生成し、向きを合わせる
    /// (プレハブ本体の向きを書き換えるとプレハブに差分が出るため、生成したものの向きを変える)
    /// </summary>
    private GameObject _SpawnAttackEffect(GameObject effect) {
        if (effect == null) {
            Debug.Log("攻撃エフェクトが登録されていません");
            return null;
        }
        var effect_obj = Instantiate(effect, transform.position, Quaternion.identity);
        var scale = effect_obj.transform.localScale;
        scale.x = _isEffectRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        effect_obj.transform.localScale = scale;
        return effect_obj;
    }

    public override void OnWarp() {
        // 即座に帰還
        if (_isAppearing) {
            _ResetReturnTimer(0.01f);
        }
    }

    protected override void _ForceReturn() {
        base._ForceReturn();
        // 切り離し攻撃終了
        _EndSeparate();
    }

    #region 必殺技

    // 必殺技の間は時間を止め(敵やギミックを止める)、必殺技の演出だけを実時間で動かす。ポーズ中・ヒットストップ中は進めない
    private float _SpecialDeltaTime => Pause_UI.IsOpen || Time.unscaledTime < _specialHitStopEndTime ? 0f : Time.unscaledDeltaTime;
    // カメラのズームはヒットストップ中も進める(当たった瞬間からズームし始めるため)
    private float _SpecialZoomDeltaTime => Pause_UI.IsOpen ? 0f : Time.unscaledDeltaTime;

    protected override void _UseSpecial() {
        base._UseSpecial();
        if (!_isSpecialUsing) return;
        // カットインの間から時間を止める
        _StopWorld();
        // カットイン中はプレイヤーをズームする
        var player_transform = _playerTransform;
        _SetSpecialZoom(() => player_transform != null ? (Vector2)player_transform.position : Vector2.zero, _specialCutInZoomHeight);
        // 出ている仲間は、時間が止まっていてもすぐに帰す
        if (_isAppearing) {
            _ResetReturnTimer(0f);
            _RunUnscaled(gameObject);
            _anim?.Play("ToHide");
        }
    }

    protected override void _OnSpecialCutInFinished(PlayableDirector obj) {
        base._OnSpecialCutInFinished(obj); // ここで時間の流れが戻されるため、止め直す
        Pause_UI.SetTimeScale(0f);
        StartCoroutine(_SpecialAttack());
    }

    /// <summary>
    /// 時間を止める(カメラの揺れは止めずに動かす)
    /// </summary>
    private void _StopWorld() {
        if (!_isSpecialTimeStopped) {
            _isSpecialTimeStopped = true;
            var brain = _GetCameraBrain();
            if (brain != null) {
                _cameraIgnoreTimeScale = brain.IgnoreTimeScale;
                brain.IgnoreTimeScale = true;
            }
            var impulse = Unity.Cinemachine.CinemachineImpulseManager.Instance;
            _impulseIgnoreTimeScale = impulse.IgnoreTimeScale;
            impulse.IgnoreTimeScale = true;
        }
        Pause_UI.SetTimeScale(0f);
    }

    /// <summary>
    /// 止めた時間を戻し、実時間で動くように変えたものを元に戻す
    /// </summary>
    private void _ResumeWorld() {
        if (!_isSpecialTimeStopped) return;
        _isSpecialTimeStopped = false;
        Pause_UI.SetTimeScale(1f);
        var brain = _GetCameraBrain();
        if (brain != null) brain.IgnoreTimeScale = _cameraIgnoreTimeScale;
        Unity.Cinemachine.CinemachineImpulseManager.Instance.IgnoreTimeScale = _impulseIgnoreTimeScale;
        foreach (var pair in _unscaledAnimators) {
            if (pair.Key != null) pair.Key.updateMode = pair.Value;
        }
        _unscaledAnimators.Clear();
        foreach (var particle in _unscaledParticles) {
            if (particle == null) continue;
            var main = particle.main;
            main.useUnscaledTime = false;
        }
        _unscaledParticles.Clear();
    }

    private static Unity.Cinemachine.CinemachineBrain _GetCameraBrain() {
        var cam = Camera.main;
        return cam != null ? cam.GetComponent<Unity.Cinemachine.CinemachineBrain>() : null;
    }

    /// <summary>
    /// 必殺技のカメラで映す中心と、映す範囲の高さを設定する(高さは少しずつ変わる)
    /// </summary>
    private void _SetSpecialZoom(System.Func<Vector2> center, float height) {
        if (_specialCameraFocus == null) {
            var focus = new GameObject("IceSpecialCameraFocus");
            _specialObjects.Add(focus);
            _specialCameraFocus = focus.transform;
        }
        // ズームしていない状態からは、今映っている範囲から少しずつ変える
        if (_specialZoomCenter == null) _specialZoomHeight = _GetCurrentViewHeight();
        _specialZoomCenter = center;
        _specialZoomTargetHeight = height;
        _specialCameraFocus.position = center();
        CinemachineManager.Instance?.ZoomOnTarget(_specialCameraFocus, _specialZoomHeight);
    }

    private void _UpdateSpecialZoom() {
        if (_specialZoomCenter == null || _specialCameraFocus == null) return;
        _specialZoomHeight = Mathf.Lerp(_specialZoomHeight, _specialZoomTargetHeight, 1f - Mathf.Exp(-_specialZoomSpeed * _SpecialZoomDeltaTime));
        _specialCameraFocus.position = _specialZoomCenter();
        CinemachineManager.Instance?.ZoomOnTarget(_specialCameraFocus, _specialZoomHeight);
    }

    private void _EndSpecialZoom() {
        if (_specialZoomCenter == null) return;
        _specialZoomCenter = null;
        CinemachineManager.Instance?.ReturnToPlayer();
    }

    // 今カメラに映っている範囲の高さ(Z=0の平面上)
    private static float _GetCurrentViewHeight() {
        var cam = Camera.main;
        if (cam == null) return 10f;
        if (cam.orthographic) return cam.orthographicSize * 2f;
        return 2f * Mathf.Abs(cam.transform.position.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // 対象の敵の中心を映す
    private System.Func<Vector2> _TargetZoomCenter(Enemy_Base target) {
        Vector2 last = _GetTargetCenter(target);
        return () => {
            if (_IsValidTarget(target)) last = _GetTargetCenter(target);
            return last;
        };
    }

    /// <summary>
    /// BGMを徐々に小さくする(ポーズ中はポーズ画面がBGMの音量を変えるため、触らない)
    /// </summary>
    private IEnumerator _FadeSpecialBgm(float duration) {
        var bgm_obj = GameObject.Find("BGM");
        var bgm = bgm_obj != null ? bgm_obj.GetComponent<AudioSource>() : null;
        if (bgm == null) yield break;
        _specialBgm = bgm;
        _specialBgmVolume = bgm.volume;
        float target_volume = _specialBgmVolume * _specialBgmVolumeRate;
        float time = 0f;
        while (time < duration && _specialBgm == bgm) {
            time += _SpecialDeltaTime;
            if (!Pause_UI.IsOpen && bgm != null) {
                bgm.volume = Mathf.Lerp(_specialBgmVolume, target_volume, Mathf.Clamp01(time / Mathf.Max(duration, 0.001f)));
            }
            yield return null;
        }
    }

    // BGMの音量を元に戻す
    private void _RestoreSpecialBgm() {
        if (_specialBgm == null) return;
        _specialBgm.volume = _specialBgmVolume;
        _specialBgm = null;
    }

    private void _PlaySpecialChargeSe() {
        if (_seSpecialCharge == null) return;
        var obj = new GameObject("IceSpecialChargeSe");
        _specialObjects.Add(obj);
        _specialChargeSe = obj.AddComponent<AudioSource>();
        _specialChargeSe.clip = AudioUtil.GetClipForGain(_seSpecialCharge, _seSpecialChargeVolume);
        _specialChargeSe.volume = Mathf.Min(_seSpecialChargeVolume, 1f);
        _specialChargeSe.spatialBlend = 0f;
        _specialChargeSe.Play();
    }

    // 溜めの音を止める(溜めより長い音の時は、突撃する時に止める)
    private void _StopSpecialChargeSe() {
        if (_specialChargeSe == null) return;
        _DestroySpecialObject(_specialChargeSe.gameObject);
        _specialChargeSe = null;
    }

    private static void _PlaySpecialSe(AudioClip clip, float volume) {
        if (clip == null) return;
        var cam = Camera.main;
        AudioUtil.PlayClipAtPointUnclamped(clip, cam != null ? cam.transform.position : Vector3.zero, volume);
    }

    /// <summary>
    /// 時間を止めている間も動くように、アニメーション・パーティクル・自動削除を実時間で動かす
    /// </summary>
    private void _RunUnscaled(GameObject obj) {
        if (obj == null) return;
        foreach (var animator in obj.GetComponentsInChildren<Animator>(true)) {
            if (!_unscaledAnimators.ContainsKey(animator)) _unscaledAnimators.Add(animator, animator.updateMode);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        foreach (var particle in obj.GetComponentsInChildren<ParticleSystem>(true)) {
            var main = particle.main;
            if (main.useUnscaledTime) continue;
            main.useUnscaledTime = true;
            _unscaledParticles.Add(particle);
        }
        foreach (var auto_destroy in obj.GetComponentsInChildren<AutoDestroy>(true)) {
            auto_destroy.useUnscaledTime = true;
        }
    }

    /// <summary>
    /// 必殺技攻撃
    /// 最初の一撃を正面へ飛ばし、敵に当たったら姿を消して連続攻撃→力を溜めた最後の一撃→敵の横に姿を現す
    /// 演出が終わるまで、敵のHPは1より減らない
    /// </summary>
    private IEnumerator _SpecialAttack() {
        Enemy_Base target = null;
        // 最初の一撃が飛んでいる間は、通常のカメラに戻す
        _EndSpecialZoom();
        yield return _SpecialFirstAttack(hit => target = hit);
        if (target == null) {
            // 敵に当たらなければ失敗してそのまま終了(ゲージは半分だけ消費する)
            ForceCharge(_specialMissRemainCharge);
            _ResumeWorld();
            _onEndSpecialAttack?.Invoke();
            yield break;
        }
        _specialTarget = target;

        // プレイヤーの姿を消す
        var player = _playerTransform != null ? _playerTransform.GetComponent<Player_Character>() : null;
        if (player != null) {
            _SpawnWarpEffect(player.transform.position);
            _warpControl?.SpawnAfterImage(null, 1f, 1f, true);
            player.SetSpecialHidden();
        }
        Vector2 last_center = _GetTargetCenter(target);
        // 終了後にプレイヤーが現れる位置(必殺技を始めた時点で、プレイヤーから遠い方のワープチェッカーの位置)
        Vector2? appear_point = _GetFarWarpPoint(target, _playerTransform != null ? (Vector2)_playerTransform.position : last_center);
        // 最初の一撃が当たった瞬間から、対象の敵をズームする(ヒットストップ中もズームは進める)
        _SetSpecialZoom(_TargetZoomCenter(target), _specialRushZoomHeight);
        // 最後の一撃は、プレイヤーがいた側から突撃する
        bool is_from_left = _playerTransform == null || _playerTransform.position.x <= last_center.x;

        // 最初のヒットから連続攻撃が始まるまで待つ(ヒットストップ中は進まない)
        float wait = 0f;
        while (wait < _specialFirstAttackWait) {
            wait += _SpecialDeltaTime;
            yield return null;
        }

        // 連続攻撃の間に、BGMを徐々に小さくする
        StartCoroutine(_FadeSpecialBgm(_specialAttackDuration));
        yield return _SpecialRushAttack(target);

        if (_IsValidTarget(target)) {
            yield return _SpecialFinishAttack(target, is_from_left);
            last_center = _GetTargetCenter(target);
        }

        // 時間とカメラを戻してから、必殺技を始めた時点で遠い方だったワープチェッカーの位置にプレイヤーを表示
        _EndSpecialZoom();
        _ResumeWorld();
        Vector2 appear_pos = appear_point ?? last_center;
        // 敵から離れる向き(敵と同じ位置に現れた時は、敵がいた側の反対へ)
        bool is_away_right = !Mathf.Approximately(appear_pos.x, last_center.x) ? appear_pos.x > last_center.x : !is_from_left;
        if (player != null) player.AppearFromSpecial(appear_pos, !is_away_right);
        _onEndSpecialAttack?.Invoke();
        // 操作できるようになったので、BGMの音量を元に戻す
        _RestoreSpecialBgm();
        if (player != null) {
            // 敵から離れる向きへ、ワープ後と同じ慣性移動をする(操作できる状態に戻ってから動かす)
            player.StartWarpDash(is_away_right);
            // しばらく無敵にする
            if (player.PlayerCharaParam != null) player.PlayerCharaParam.currentInvincibilityTimer = _specialEndInvincibleTime;
        }

        // 最後の一撃でHPが1だった時は、演出が終わってから倒す
        _specialTarget = null;
        if (target != null) {
            target.SetMinHP(0);
            if (!target.isDead && target.currentHP == 1) {
                target.Damage(1, Vector2.zero, 0f, 0f, false, true);
            }
        }
    }

    /// <summary>
    /// 敵の左右のワープチェッカーのうち、指定した位置から遠い方のワープ先を返す(どちらも無ければnull)
    /// </summary>
    private static Vector2? _GetFarWarpPoint(Enemy_Base target, Vector2 from) {
        Vector2? far_point = null;
        float far_distance = -1f;
        foreach (var side in new[] { WarpControl.eWarpDirection.Left, WarpControl.eWarpDirection.Right }) {
            var checker = target.GetWarpChecker(side);
            var point = checker != null ? checker.GetWarpPoint() : null;
            if (!point.HasValue) continue;
            float distance = (point.Value - from).sqrMagnitude;
            if (distance > far_distance) {
                far_distance = distance;
                far_point = point;
            }
        }
        return far_point;
    }

    /// <summary>
    /// 最初の一撃: プレイヤーから正面へ飛ばし、最初に当たった敵を返す(当たらなければnull)
    /// 当たった時はヒットストップをかけ、ヒットストップが終わったら少し通り抜けてから、ワープエフェクトを出しながら消す
    /// </summary>
    private IEnumerator _SpecialFirstAttack(System.Action<Enemy_Base> on_hit) {
        if (_specialFirstAttackPrefab == null || _playerTransform == null) yield break;

        // 能力側の向きはFixedUpdateで更新されるため、時間を止めている間はプレイヤー本体の向きを使う
        var owner = _playerTransform.GetComponent<Character_Base>();
        bool is_right = owner != null ? owner.isRight : _isRight;
        var obj = Instantiate(_specialFirstAttackPrefab, _playerTransform.position, Quaternion.identity);
        _specialObjects.Add(obj);
        _SetFacing(obj.transform, is_right ? Vector2.right : Vector2.left);

        // 当たり判定はDamageZoneのコライダーの範囲を使い、ダメージはここで与える(敵の被弾無敵に関係なく当てるため)
        var zone = obj.GetComponentInChildren<DamageZone>(true);
        var box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
        Vector3 local_center = Vector3.zero;
        Vector2 hit_size = Vector2.one;
        if (box != null) {
            local_center = obj.transform.InverseTransformPoint(box.transform.TransformPoint(box.offset));
            var scale = box.transform.lossyScale;
            hit_size = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y));
        }
        _DisableDamageZone(obj);

        int enemy_mask = LayerMask.GetMask("Enemy");
        Vector3 velocity = (is_right ? Vector3.right : Vector3.left) * _specialFirstAttackSpeed;
        Enemy_Base hit = null;
        float time = 0f;
        while (time < _specialFirstAttackTime && hit == null) {
            obj.transform.position += velocity * _SpecialDeltaTime;
            Vector2 center = obj.transform.TransformPoint(local_center);
            float nearest = float.MaxValue;
            foreach (var col in Physics2D.OverlapBoxAll(center, hit_size, 0f, enemy_mask)) {
                var enemy = col.GetComponentInParent<Enemy_Base>();
                if (!_IsValidTarget(enemy) || enemy.IsEventInvincible) continue;
                // シールド等で防がれる相手には当たらない
                if (enemy is IDamageSourceBlocker blocker && blocker.IsBlockedDamageSource(null)) continue;
                float distance = Mathf.Abs(enemy.transform.position.x - _playerTransform.position.x);
                if (distance < nearest) {
                    nearest = distance;
                    hit = enemy;
                }
            }
            time += _SpecialDeltaTime;
            yield return null;
        }

        if (hit != null) {
            // 演出が終わるまで倒れないようにしてからダメージを与える
            hit.SetMinHP(1);
            _SpecialDamage(hit, _specialFirstAttackDamage, zone);
            CinemachineManager.Instance?.ShakeCamera(0.3f, 0.1f);
            _specialHitStopEndTime = Mathf.Max(_specialHitStopEndTime, Time.unscaledTime + _specialFirstAttackHitStopTime);
            StartCoroutine(_PassFirstAttackAfterHitStop(obj, velocity));
            on_hit(hit);
            yield break;
        }
        _SpawnWarpEffect(obj.transform.position);
        _DestroySpecialObject(obj);
        on_hit(null);
    }

    /// <summary>
    /// 当たった最初の一撃を、ヒットストップの後に少し通り抜けさせてから、ワープエフェクトを出しながら消す
    /// (ヒットストップ中は_SpecialDeltaTimeが0のため、その場に止まる)
    /// </summary>
    private IEnumerator _PassFirstAttackAfterHitStop(GameObject obj, Vector3 velocity) {
        float time = 0f;
        while (obj != null && time < _specialFirstAttackPassTime) {
            float delta = _SpecialDeltaTime;
            obj.transform.position += velocity * delta;
            time += delta;
            yield return null;
        }
        if (obj == null) yield break;
        _SpawnWarpEffect(obj.transform.position);
        _DestroySpecialObject(obj);
    }

    /// <summary>
    /// 連続攻撃: 敵の周囲から敵を通過する攻撃を生成し続ける(後半になるほど生成が速くなる)
    /// </summary>
    private IEnumerator _SpecialRushAttack(Enemy_Base target) {
        if (_specialAttackPrefab == null || _specialAttackHitCount <= 0) yield break;

        // 生成間隔は等比で短くし、合計が連続攻撃の時間になるようにする
        int count = _specialAttackHitCount;
        float ratio = count > 1 ? Mathf.Pow(Mathf.Max(_specialAttackIntervalRate, 0.001f), 1f / (count - 1)) : 1f;
        float first_interval = Mathf.Approximately(ratio, 1f)
            ? _specialAttackDuration / count
            : _specialAttackDuration * (1f - ratio) / (1f - Mathf.Pow(ratio, count));

        int spawned = 0;
        int alive = 0;
        float time = 0f;
        float next_spawn_time = 0f;
        float interval = first_interval;
        Vector2 last_dir = Vector2.zero;
        while (spawned < count && _IsValidTarget(target)) {
            // 序盤はプレイヤーと同じ姿が2体以上見えないように、前の攻撃が消えるまで待つ
            bool can_overlap = time > _specialAttackOverlapTime;
            if (time >= next_spawn_time && (can_overlap || alive == 0)) {
                // 向き・距離・速さ・通過する位置をばらつかせて、あちこちから通過させる
                var dir = _RandomRushDirection(last_dir);
                last_dir = dir;
                float start_distance = _specialAttackDistance * Random.Range(1f - _specialAttackDistanceRandom, 1f + _specialAttackDistanceRandom);
                float end_distance = _specialAttackDistance * Random.Range(1f - _specialAttackDistanceRandom, 1f + _specialAttackDistanceRandom);
                float dash_time = _specialAttackDashTime * Random.Range(1f - _specialAttackDashTimeRandom, 1f + _specialAttackDashTimeRandom);
                Vector2 pass_offset = new Vector2(-dir.y, dir.x) * Random.Range(-_specialAttackPassOffset, _specialAttackPassOffset);

                alive++;
                StartCoroutine(_SpecialDash(target, dir, start_distance, end_distance, pass_offset, dash_time, () => alive--));
                spawned++;
                // 後半ほど間隔を短くする
                next_spawn_time = Mathf.Max(next_spawn_time, time) + interval;
                interval *= ratio;
            }
            time += _SpecialDeltaTime;
            yield return null;
        }
        // 最後の攻撃が通過し終わるまで待つ
        while (alive > 0) yield return null;
    }

    /// <summary>
    /// 連続攻撃の通過する向きを決める(左右どちらからも、水平から_specialAttackAngleRangeまで傾けて、直前の向きとは十分に変える)
    /// </summary>
    private Vector2 _RandomRushDirection(Vector2 last_dir) {
        Vector2 dir = Vector2.right;
        for (int i = 0; i < 10; i++) {
            float angle = Random.Range(-_specialAttackAngleRange, _specialAttackAngleRange) * Mathf.Deg2Rad;
            dir = new Vector2(Mathf.Cos(angle) * (Random.value < 0.5f ? 1f : -1f), Mathf.Sin(angle));
            if (last_dir == Vector2.zero || Vector2.Angle(dir, last_dir) >= _specialAttackMinAngleChange) break;
        }
        return dir;
    }

    /// <summary>
    /// 敵の近くを通過する攻撃を1回行う(ワープエフェクトと同時に出現し、通過した瞬間にダメージを与え、ワープエフェクトを出しながら消える)
    /// </summary>
    /// <param name="start_distance">通過する位置から出現位置までの距離</param>
    /// <param name="end_distance">通過する位置から消える位置までの距離</param>
    /// <param name="pass_offset">通過する位置の、敵の中心からのずれ</param>
    private IEnumerator _SpecialDash(Enemy_Base target, Vector2 dir, float start_distance, float end_distance, Vector2 pass_offset, float dash_time, System.Action on_end) {
        Vector2 pass_point = _GetTargetCenter(target) + pass_offset;
        Vector2 start = pass_point - dir * start_distance;
        Vector2 end = pass_point + dir * end_distance;
        float hit_rate = start_distance / Mathf.Max(start_distance + end_distance, 0.001f); // 敵の所を通過する瞬間

        var obj = Instantiate(_specialAttackPrefab, start, Quaternion.identity);
        _specialObjects.Add(obj);
        var zone = obj.GetComponentInChildren<DamageZone>(true);
        _DisableDamageZone(obj);
        _SetFacing(obj.transform, dir);
        _RunTrailUnscaled(obj);
        var graphic = _GetGraphic(obj);
        _SpawnWarpEffect(start);
        _SpawnSpecialAfterImage(graphic);

        bool is_hit = false;
        int ghost = 0;
        float time = 0f;
        while (time < dash_time) {
            time += _SpecialDeltaTime;
            float rate = Mathf.Clamp01(time / dash_time);
            obj.transform.position = Vector2.Lerp(start, end, rate);
            // 通過中は薄い残像を等間隔に置いていく
            if (ghost < _specialAttackGhostCount && rate >= (ghost + 1f) / (_specialAttackGhostCount + 1f)) {
                ghost++;
                _SpawnSpecialAfterImage(graphic, _specialAttackGhostAlpha, _specialAttackGhostTime);
            }
            if (!is_hit && rate >= hit_rate) {
                is_hit = true;
                if (_SpecialDamage(target, _specialAttackDamage, zone)) {
                    _PlaySpecialSe(_seSpecialHit, _seSpecialHitVolume);
                    // ヒットストップ(演出だけを止める。カメラの揺れは止めない)
                    _specialHitStopEndTime = Mathf.Max(_specialHitStopEndTime, Time.unscaledTime + _specialAttackHitStopTime);
                    CinemachineManager.Instance?.ShakeCamera(_specialAttackHitShake, _specialAttackHitShakeTime);
                }
            }
            yield return null;
        }

        _SpawnWarpEffect(end);
        _SpawnSpecialAfterImage(graphic);
        StartCoroutine(_DestroyAfterTrail(obj));
        on_end?.Invoke();
    }

    /// <summary>
    /// 最後の一撃: 力を溜めてから突撃し、当たった瞬間に一枚絵を表示する
    /// </summary>
    private IEnumerator _SpecialFinishAttack(Enemy_Base target, bool is_from_left) {
        if (_specialAttackPrefab == null) yield break;

        Vector2 dir = is_from_left ? Vector2.right : Vector2.left;
        Vector2 center = _GetTargetCenter(target);
        Vector2 start = center - dir * _specialFinishDistance;

        var obj = Instantiate(_specialAttackPrefab, start, Quaternion.identity);
        _specialObjects.Add(obj);
        var zone = obj.GetComponentInChildren<DamageZone>(true);
        _DisableDamageZone(obj);
        _SetFacing(obj.transform, dir);
        _RunTrailUnscaled(obj);
        _SpawnWarpEffect(start);

        // 敵と突撃する姿の両方が映るようにズームする
        var attacker = obj.transform;
        var target_center = _TargetZoomCenter(target);
        System.Func<Vector2> finish_center = () => attacker != null ? ((Vector2)attacker.position + target_center()) * 0.5f : target_center();
        _SetSpecialZoom(finish_center, _GetFinishZoomHeight(attacker, target_center()));

        var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        var lights = obj.GetComponentsInChildren<Light2D>();
        var light_intensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) light_intensities[i] = lights[i].intensity;

        // 力を溜める(震えながら色が明滅し、光が強くなっていく)
        _PlaySpecialChargeSe();
        float time = 0f;
        float next_shake_time = 0f;
        while (time < _specialFinishChargeTime) {
            time += _SpecialDeltaTime;
            float rate = Mathf.Clamp01(time / _specialFinishChargeTime);
            obj.transform.position = start + Random.insideUnitCircle * Mathf.Lerp(0.02f, 0.2f, rate);
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * Mathf.Lerp(8f, 40f, rate));
            var color = Color.Lerp(Color.white, _specialFinishChargeColor, pulse * rate);
            foreach (var rend in renderers) rend.color = color;
            for (int i = 0; i < lights.Length; i++) lights[i].intensity = light_intensities[i] * Mathf.Lerp(1f, 3f, rate);
            // 力を溜めている間はカメラを小さく揺らし続ける
            if (time >= next_shake_time) {
                CinemachineManager.Instance?.ShakeCamera(_specialFinishChargeShake, 0.15f);
                next_shake_time = time + 0.15f;
            }
            yield return null;
        }
        foreach (var rend in renderers) rend.color = Color.white;
        _StopSpecialChargeSe();

        // 突撃(溜めていた位置に残像を残す)
        if (_IsValidTarget(target)) center = _GetTargetCenter(target);
        start = obj.transform.position;
        _SpawnSpecialAfterImage(_GetGraphic(obj));
        Vector2 end = center + dir * _specialFinishDistance;
        time = 0f;
        while (time < _specialFinishDashTime) {
            time += _SpecialDeltaTime;
            float rate = Mathf.Clamp01(time / _specialFinishDashTime);
            obj.transform.position = Vector2.Lerp(start, end, rate);
            _specialZoomTargetHeight = _GetFinishZoomHeight(attacker, target_center());
            if (rate >= 0.5f) break; // 敵の中心に届いた瞬間に当たる
            yield return null;
        }

        // 攻撃がヒットする瞬間に一枚絵を表示する(表示している間は時間を止める)
        _SpecialDamage(target, _specialFinishDamage, zone);
        if (_seSpecialFinishHit != null) {
            foreach (var se in _seSpecialFinishHit) _PlaySpecialSe(se, _seSpecialFinishHitVolume);
        }
        CinemachineManager.Instance?.ShakeCamera(_specialFinishHitShake, _specialFinishHitShakeTime);
        ScreenFlash.Instance?.Flash(0.2f, Color.white);
        yield return _ShowSpecialFinishImage();

        _SpawnWarpEffect(obj.transform.position);
        _DestroySpecialObject(obj);
    }

    // 敵と突撃する姿の両方が映る高さ
    private float _GetFinishZoomHeight(Transform attacker, Vector2 target_center) {
        if (attacker == null) return _specialFinishZoomMinHeight;
        var cam = Camera.main;
        float aspect = cam != null ? cam.aspect : 16f / 9f;
        Vector2 diff = (Vector2)attacker.position - target_center;
        return Mathf.Max(_specialFinishZoomMinHeight,
            (Mathf.Abs(diff.x) + _specialFinishZoomMargin) / aspect,
            Mathf.Abs(diff.y) + _specialFinishZoomMargin);
    }

    /// <summary>
    /// 一枚絵を表示する(表示している間はポーズを開けないようにする)
    /// </summary>
    private IEnumerator _ShowSpecialFinishImage() {
        if (_specialFinishImage == null || _specialFinishImageTime <= 0f) yield break;

        var pause_ui = GetPauseUI();
        if (pause_ui != null) pause_ui.canOpen = false;

        _specialFinishImageObj = _CreateSpecialFinishImage(out var group, out var image_rect);
        var shake_rect = (RectTransform)group.transform;
        float fade = Mathf.Max(_specialFinishImageFadeTime, 0.001f);
        float time = 0f;
        while (time < _specialFinishImageTime) {
            time += _SpecialDeltaTime;
            float fade_in = Mathf.Clamp01(time / fade);
            float fade_out = Mathf.Clamp01((_specialFinishImageTime - time) / fade);
            group.alpha = Mathf.Min(fade_in, fade_out);
            // 表示する時は大きい所から縮み、消える時は広がりながら消える
            float scale = 1f + 0.2f * (1f - fade_in) + 0.2f * (1f - fade_out);
            image_rect.localScale = Vector3.one * scale;
            // 当たった瞬間は一枚絵ごと大きく揺らす(だんだん小さくなる)
            float shake = 1f - Mathf.Clamp01(time / Mathf.Max(_specialFinishHitShakeTime, 0.001f));
            shake_rect.anchoredPosition = Random.insideUnitCircle * _specialFinishImageShake * shake;
            yield return null;
        }
        Destroy(_specialFinishImageObj);
        _specialFinishImageObj = null;

        if (pause_ui != null) pause_ui.canOpen = true;
    }

    /// <summary>
    /// 一枚絵を表示する画面を作る(画面全体を薄暗くし、その上に一枚絵を画面の高さに合わせて表示する)
    /// </summary>
    private GameObject _CreateSpecialFinishImage(out CanvasGroup group, out RectTransform image_rect) {
        var root = new GameObject("IceSpecialFinishImage", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        // 一枚絵と背景をまとめて揺らすための入れ物(Canvas自体は動かせないため)
        var shake_root = new GameObject("ShakeRoot", typeof(RectTransform), typeof(CanvasGroup));
        shake_root.transform.SetParent(root.transform, false);
        var shake_rect = shake_root.GetComponent<RectTransform>();
        _Stretch(shake_rect);
        // 揺らした時に画面の端が見えないように、少し大きくしておく
        shake_rect.offsetMin = new Vector2(-_specialFinishImageShake, -_specialFinishImageShake);
        shake_rect.offsetMax = new Vector2(_specialFinishImageShake, _specialFinishImageShake);
        group = shake_root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(shake_root.transform, false);
        _Stretch(background.GetComponent<RectTransform>());
        var background_image = background.GetComponent<Image>();
        background_image.color = new Color(0.05f, 0.12f, 0.25f, 0.55f);
        background_image.raycastTarget = false;

        var picture = new GameObject("Image", typeof(RectTransform), typeof(Image));
        picture.transform.SetParent(shake_root.transform, false);
        image_rect = picture.GetComponent<RectTransform>();
        _Stretch(image_rect);
        var image = picture.GetComponent<Image>();
        image.sprite = _specialFinishImage;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return root;
    }

    private static void _Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 必殺技のダメージを与える(被弾無敵に関係なく当て、吹き飛ばさない)
    /// </summary>
    private bool _SpecialDamage(Enemy_Base target, int damage, DamageZone zone) {
        if (!_IsValidTarget(target)) return false;
        if (!target.Damage(damage, Vector2.zero, 0f, 0f, false, true)) return false;
        var center = _GetTargetCenter(target);
        if (zone != null) _RunUnscaled(zone.SpawnHitEffectAt(center));
        return true;
    }

    /// <summary>
    /// 進む向きに合わせて向ける(画像は左向き。右へ進む時は左右反転し、上下の傾きは回転で付ける)
    /// </summary>
    private static void _SetFacing(Transform obj, Vector2 dir) {
        var scale = obj.localScale;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (dir.x >= 0f) {
            scale.x = -Mathf.Abs(scale.x);
        } else {
            scale.x = Mathf.Abs(scale.x);
            angle -= 180f;
        }
        obj.localScale = scale;
        obj.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 演出用に当たり判定を無効にする(ダメージは_SpecialDamageで与える)
    /// </summary>
    private static void _DisableDamageZone(GameObject obj) {
        foreach (var damage_zone in obj.GetComponentsInChildren<DamageZone>(true)) {
            if (damage_zone.gameObject != obj) {
                damage_zone.gameObject.SetActive(false);
            } else {
                foreach (var col in damage_zone.GetComponents<Collider2D>()) col.enabled = false;
                damage_zone.enabled = false;
            }
        }
    }

    private static bool _IsValidTarget(Enemy_Base target) {
        return target != null && !target.isDead;
    }

    private static Vector2 _GetTargetCenter(Enemy_Base target) {
        if (target == null) return Vector2.zero;
        var col = target.GetComponent<Collider2D>();
        return col != null && col.enabled ? (Vector2)col.bounds.center : (Vector2)target.transform.position;
    }

    // 必殺技の演出で出す残像(ワープの残像と同じ色・材質。時間を止めている間も消えていく)
    private void _SpawnSpecialAfterImage(SpriteRenderer src, float alpha_rate = 1f, float duration_rate = 1f) {
        if (src == null || _warpControl == null) return;
        _warpControl.SpawnAfterImage(src, alpha_rate, duration_rate, true);
    }

    // 残像にする見た目(絵が設定されている最初のSpriteRenderer)
    private static SpriteRenderer _GetGraphic(GameObject obj) {
        foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>()) {
            if (renderer.sprite != null) return renderer;
        }
        return null;
    }

    // 移動の軌跡を、時間を止めている間も消えていくようにする
    private static void _RunTrailUnscaled(GameObject obj) {
        foreach (var trail in obj.GetComponentsInChildren<WarpTrail>(true)) trail.useUnscaledTime = true;
    }

    /// <summary>
    /// 姿を消し、移動の軌跡が消えきってから破棄する(軌跡が途中で途切れないようにする)
    /// </summary>
    private IEnumerator _DestroyAfterTrail(GameObject obj) {
        float wait = 0f;
        foreach (var trail in obj.GetComponentsInChildren<WarpTrail>(true)) wait = Mathf.Max(wait, trail.lifetime);
        foreach (var renderer in obj.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
        foreach (var light in obj.GetComponentsInChildren<Light2D>()) light.enabled = false;
        float time = 0f;
        while (time < wait && obj != null) {
            if (!Pause_UI.IsOpen) time += Time.unscaledDeltaTime;
            yield return null;
        }
        _DestroySpecialObject(obj);
    }

    private void _SpawnWarpEffect(Vector3 position) {
        if (_warpAnimationPrefab != null) _RunUnscaled(Instantiate(_warpAnimationPrefab, position, Quaternion.identity));
    }

    private void _DestroySpecialObject(GameObject obj) {
        _specialObjects.Remove(obj);
        if (obj != null) Destroy(obj);
    }

    // 必殺技の途中で能力が破棄された時に、止めた時間や敵のHPの下限を元に戻す
    private void OnDestroy() {
        if (_isSpecialTimeStopped) {
            _ResumeWorld();
            var pause_ui = GetPauseUI();
            if (pause_ui != null) pause_ui.canOpen = true;
        }
        if (_specialFinishImageObj != null) Destroy(_specialFinishImageObj);
        _EndSpecialZoom();
        _RestoreSpecialBgm();
        foreach (var obj in _specialObjects) {
            if (obj != null) Destroy(obj);
        }
        if (_specialTarget != null) _specialTarget.SetMinHP(0);
        if (_isSpecialUsing) isAnySpecialUsing = false;
    }

    #endregion
}
