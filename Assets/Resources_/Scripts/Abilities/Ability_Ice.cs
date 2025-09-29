using System.Collections;
using UnityEngine;

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

    [SerializeField] private GameObject _slash1;
    [SerializeField] private GameObject _slash2;
    [SerializeField] private GameObject _slash3;
    [SerializeField] private GameObject _lockonSlash;
    private IEnumerator _separateRoutine = null;

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
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat) {
            return eAbilityResult.None;
        }
        // 切り離し攻撃終了
        _EndSeparate();

        // ロックオン攻撃判定
        if (LockonManager.Instance.HasTarget) {
            _LockonSlash();

            // ロックオン中の攻撃
            return eAbilityResult.IceLockonSlash;
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
    private void _LockonSlash() {
        if (_lockonSlash == null) {
            Debug.Log("ロックオン攻撃エフェクトが見つかりません");
            return;
        }
        Debug.Log("Lockon Slash");

        // ロックオン対象の方向を向く
        var lockon = LockonManager.Instance;
        Vector3 to_target = lockon.targetTransform.position - _playerTransform.position;
        to_target.y = 0; // 水平成分のみ

        // ロックオン対象の近くにワープ
        WarpChecker warp_checker = null;
        // ワープチェッカー取得
        if (to_target.x > 0) {
            warp_checker = lockon.GetTargetWarpPos(WarpControl.eWarpDirection.Left);
        } else if (to_target.x < 0) {
            warp_checker = lockon.GetTargetWarpPos(WarpControl.eWarpDirection.Right);
        }

        IEnumerator attack_routine() {
            yield return _warpControl?.TargetWarp(warp_checker);

            // キャラクター位置を更新
            _isRight = to_target.x > 0;
            UpdatePartnerTransform();

            // エフェクトの向きを調整
            _AttackEffectSetup(_lockonSlash);

            // エフェクト生成
            Instantiate(_lockonSlash, transform.position, Quaternion.identity);
            // アニメーション再生
            _anim?.Play("Node_Attack2", 0, 0.0f);

            // コンボリセット
            _attackStep = 0;
            _currentComboTime = _param.comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _param.comboIntervalTime; // クールタイムセット
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
            _attackStep = 1;                            // 次の攻撃へ
            _currentComboTime = _param.comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _param.comboIntervalTime; // クールタイムセット
            return eAbilityResult.IceSlash1;            // 実行結果返却
        } else if (_attackStep == 1) {
            // 2段目
            Debug.Log("Slash 2");
            StartCoroutine(_UpdateTransformEasing(_slash2, "Node_Attack2"));
            _attackStep = 2;                            // 次の攻撃へ
            _currentComboTime = _param.comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _param.comboIntervalTime; // クールタイムセット
            return eAbilityResult.IceSlash2;            // 実行結果返却
        } else if (_attackStep == 2) {
            // 3段目
            Debug.Log("Slash 3");
            StartCoroutine(_UpdateTransformEasing(_slash3, "Node_Attack3"));
            _attackStep = 0;                            // コンボリセット
            _currentComboTime = _param.comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _param.comboCoolTime;     // クールタイムセット
            return eAbilityResult.IceSlash3;            // 実行結果返却
        }

        return eAbilityResult.None;
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

        Instantiate(effect, transform.position, Quaternion.identity);  // エフェクト生成
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
                Instantiate(_slash1, transform.position, Quaternion.identity);  // エフェクト生成
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
        return eAbilityResult.None;
    }

    public override void ExecuteRelease() {
        _pressHoldTime = 0f;
        _isHoldExecuted = false;
    }

    public override void Setup(bool is_right, Transform chara_transform, CommonParameter param, CharacterParameter chara_param, WarpControl warp_control) {
        base.Setup(is_right, chara_transform, param, chara_param, warp_control);
        // 向きに応じて攻撃エフェクトの向きを調整
        _AttackEffectSetup(_slash1);
        _AttackEffectSetup(_slash2);
        _AttackEffectSetup(_slash3);
    }

    /// <summary>
    /// 攻撃エフェクトの向きを調整
    /// </summary>
    private void _AttackEffectSetup(GameObject effect) {
        if (effect == null) {
            Debug.Log($"{effect}が登録されていません");
            return;
        }
        var scale = effect.transform.localScale;
        if (!_isAttackDirectionLocked) {
            scale.x = _isRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        }
        effect.transform.localScale = scale;
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
}
