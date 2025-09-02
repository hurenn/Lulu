using System.Collections;
using UnityEngine;

public class Ability_Ice : Ability_Base {

    private int _attackStep = 0; // 0:未攻撃, 1:1段目, 2:2段目, 3:3段目

    [SerializeField] private float _comboReceptionTime = 0.7f; // コンボ入力受付時間
    private float _currentComboTime = 0f;
    [SerializeField] private float _comboIntervalTime = 0.15f; // 1コンボインターバル時間
    [SerializeField] private float _comboCoolTime = 0.2f; // コンボ終了後のクールタイム
    [SerializeField] private float _moveDuration = 0.05f; // 移動にかける時間
    // コンボ攻撃のクールタイム
    private float _currentComboCoolTime = 0f;

    // 長押し判定時間
    private float _longPressThreshold = 0.5f;
    private float _pressHoldTime = 0f;

    // 長押し実行済みフラグ
    private bool _isHoldExecuted = false;

    [SerializeField] private GameObject _slash1;
    [SerializeField] private GameObject _slash2;
    [SerializeField] private GameObject _slash3;

    private void Update() {
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
        // 帰還タイマー
        if (_isAppearing) {
            _currentReturnTime -= Time.deltaTime;
            if (_currentReturnTime <= 0f) {
                // 帰還
                _anim.Play("ToHide");
            }
        }
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_charaParam.isOverheat && !_isAppearing) {
            return eAbilityResult.None;
        }

        // コンボ攻撃判定
        var attack_result = _ComboSlash();
        // 切り離し攻撃判定
        // if(attack_result == eAbilityResult.None) attack_result = _SeparateSlash();

        // 攻撃実行
        if (attack_result != eAbilityResult.None) {
            // 召喚エフェクト判定
            if (!_isAppearing) {
                // MP消費
                _charaParam.AddUnRecoverableTime_MP(0.5f);
                _charaParam.ConsumeMP(CharacterParameter.eAbilityType.Ice);
                // 召喚エフェクト再生
                Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity);
            }
            // 帰還タイマーリセット
            _currentReturnTime = _returnTime;
            return attack_result;
        }

        return eAbilityResult.None;
    }

    /// <summary>
    /// コンボ攻撃実行
    /// </summary>
    private eAbilityResult _ComboSlash() {

        // クールタイム中は実行不可
        if (_currentComboCoolTime > 0f) {
            return eAbilityResult.None;
        }

        System.Action<GameObject, string> callback = (GameObject attack_effect, string anim_name) => {
            UpdateTransform(_charaTransform.position, _inputDir);
            Instantiate(attack_effect, transform.position, Quaternion.identity); // エフェクト生成
            _anim?.Play(anim_name, 0, 0.0f);       // アニメーション再生
        };

        if (_attackStep == 0) {
            // 1段目
            Debug.Log("Slash 1");
            StartCoroutine(_UpdateTransformEasing(_slash1, "Node_Attack1"));
            _attackStep = 1;                            // 次の攻撃へ
            _currentComboTime = _comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _comboIntervalTime; // クールタイムセット
            return eAbilityResult.IceSlash1;            // 実行結果返却
        } else if (_attackStep == 1) {
            // 2段目
            Debug.Log("Slash 2");
            StartCoroutine(_UpdateTransformEasing(_slash2, "Node_Attack2"));
            _attackStep = 2;                            // 次の攻撃へ
            _currentComboTime = _comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _comboIntervalTime; // クールタイムセット
            return eAbilityResult.IceSlash2;            // 実行結果返却
        } else if (_attackStep == 2) {
            // 3段目
            Debug.Log("Slash 3");
            StartCoroutine(_UpdateTransformEasing(_slash3, "Node_Attack3"));
            _attackStep = 0;                            // コンボリセット
            _currentComboTime = _comboReceptionTime;    // 次のコンボ受付時間
            _currentComboCoolTime = _comboCoolTime;     // クールタイムセット
            return eAbilityResult.IceSlash3;            // 実行結果返却
        }

        return eAbilityResult.None;
    }

    private IEnumerator _UpdateTransformEasing(GameObject effect, string anim_name) {
        if (_isAppearing) {
            // イージングで位置を更新
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = _charaTransform.position + new Vector3(
                _localPosition.x * (_isRight ? -1 : 1),
                _localPosition.y,
                _localPosition.z);
            while (elapsedTime < _moveDuration) {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / _moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        UpdateTransform(_charaTransform.position, _inputDir);   // 最終的な位置を設定
        Instantiate(effect, transform.position, Quaternion.identity);  // エフェクト生成
        _anim?.Play(anim_name, 0, 0.0f);   // アニメーション再生
    }

    public override eAbilityResult ExecuteLong() {
        // 切り離し
        if (!_isHoldExecuted) {
            _pressHoldTime += Time.deltaTime;
            if (_pressHoldTime >= _longPressThreshold) {
                Debug.Log("Ice Separate");
                _isHoldExecuted = true;
                _attackStep = 0; // コンボリセット
                return eAbilityResult.IceSeparate;
            }
        }
        return eAbilityResult.None;
    }

    public override void ExecuteRelease() {
        _pressHoldTime = 0f;
        _isHoldExecuted = false;
    }

    public override void SetCharacterTransform(bool is_right, Transform chara_transform, CharacterParameter chara_param) {
        base.SetCharacterTransform(is_right, chara_transform, chara_param);
        // 向きに応じて攻撃エフェクトの向きを調整
        if (_slash1 != null) {
            var scale = _slash1.transform.localScale;
            scale.x = is_right ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _slash1.transform.localScale = scale;
        }
        if (_slash2 != null) {
            var scale = _slash2.transform.localScale;
            scale.x = is_right ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _slash2.transform.localScale = scale;
        }
        if (_slash3 != null) {
            var scale = _slash3.transform.localScale;
            scale.x = is_right ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _slash3.transform.localScale = scale;
        }
    }

    public override void OnWarp() {
        // 即座に帰還
        if (_isAppearing) {
            _currentReturnTime = 0.01f;
        }
    }
}
