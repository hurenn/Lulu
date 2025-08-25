using UnityEngine;

public class Ability_Ice : Ability_Base {

    private int _attackStep = 0; // 0:未攻撃, 1:1段目, 2:2段目, 3:3段目

    [SerializeField] private float _comboReceptionTime = 0.7f; // コンボ入力受付時間
    private float _currentReceptionTime = 0f;
    [SerializeField] private float _comboIntervalTime = 0.15f; // 1コンボインターバル時間
    [SerializeField] private float _comboCoolTime = 0.2f; // コンボ終了後のクールタイム
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
            _currentReceptionTime -= Time.deltaTime;
            if (_currentReceptionTime <= 0f) {
                _attackStep = 0; // コンボリセット
            }
        }
        if (_currentComboCoolTime > 0f) {
            _currentComboCoolTime -= Time.deltaTime;
        }
    }

    public override eAbilityResult ExecuteSimple() {
        var slash_result = _ComboSlash();
        if (slash_result != eAbilityResult.None) {
            return slash_result;
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

        if (_attackStep == 0) {
            // 1段目
            Debug.Log("Slash 1");
            Instantiate(_slash1, transform.position, Quaternion.identity);
            _attackStep = 1;
            _currentReceptionTime = _comboReceptionTime;
            _currentComboCoolTime = _comboIntervalTime;
            return eAbilityResult.IceSlash1;
        } else if (_attackStep == 1) {
            // 2段目
            Debug.Log("Slash 2");
            Instantiate(_slash2, transform.position, Quaternion.identity);
            _attackStep = 2;
            _currentReceptionTime = _comboReceptionTime;
            _currentComboCoolTime = _comboIntervalTime;
            return eAbilityResult.IceSlash2;
        } else if (_attackStep == 2) {
            // 3段目
            Debug.Log("Slash 3");
            Instantiate(_slash3, transform.position, Quaternion.identity);
            _attackStep = 3;
            _currentReceptionTime = _comboReceptionTime;
            _currentComboCoolTime = _comboIntervalTime;
            return eAbilityResult.IceSlash3;
        } else {
            // 3段目以降はリセット
            _attackStep = 0;
            _currentComboCoolTime = _comboCoolTime;
        }

        return eAbilityResult.None;
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

    public override void SetIsRight(bool isRight) {
        base.SetIsRight(isRight);
        // 向きに応じて攻撃エフェクトの向きを調整
        if (_slash1 != null) {
            var scale = _slash1.transform.localScale;
            scale.x = isRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _slash1.transform.localScale = scale;
        }
        if (_slash2 != null) {
            var scale = _slash2.transform.localScale;
            scale.x = isRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _slash2.transform.localScale = scale;
        }
        if (_slash3 != null) {
            var scale = _slash3.transform.localScale;
            scale.x = isRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _slash3.transform.localScale = scale;
        }
    }
}
