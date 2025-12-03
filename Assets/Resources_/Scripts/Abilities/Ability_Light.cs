using UnityEngine;

public class Ability_Light : Ability_Base
{
    [SerializeField] private GameObject _lightDomePrefab;
    private GameObject _lightDomeInstance;

    private bool _isNotHide => _rend.color.a > 0.9f;
    private GoalMarker _goalMarker;

    // 自動発光タイマー
    private float _autoLightTimer = 1.0f;
    private float _currentAutoLightTimer = 0f;
    private bool _isAutoLight => _currentAutoLightTimer > _autoLightTimer;
    private bool _isManualLight = false;

    public override void UpdateParameter(bool is_right, Transform chara_pos, CommonParameter common_param, CharacterParameter_Player chara_param, WarpControl warp_control) {
        base.UpdateParameter(is_right, chara_pos, common_param, chara_param, warp_control);

        var goal_marker = FindAnyObjectByType<GoalMarker>();
        if (goal_marker != null) {
            _goalMarker = goal_marker;
        }

        if (_lightDomeInstance == null) {
            _lightDomeInstance = Instantiate(_lightDomePrefab, _playerTransform);
        }
    }

    private void Update() {
        if (_isNotHide && _IsOutOfScreen()) {
            // 画面外に出たら非表示にする
            _anim?.Play("Pepe_ToHide");
        }

        // 自動発光タイマー更新
        if (_currentAutoLightTimer < _autoLightTimer) {
            _currentAutoLightTimer += Time.deltaTime;
            if (_currentAutoLightTimer >= _autoLightTimer) {
                // 自動発光
                SetAutoLight(true);
            }
        }

        _UpdateLightDomeActive();
    }

    // 自動発光設定
    public void SetAutoLight(bool is_active) {
        _isManualLight = is_active;
        _charaParam.isAutoLightInvincible = is_active;

        if (!is_active) {
            _currentAutoLightTimer = 0f;
        }
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if ((_charaParam.isOverheat && !_isAppearing) || _lightDomePrefab == null) {
            UpdatePartnerTransform(); // 位置更新
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }

        // アニメーション再生
        _anim?.Play("Pepe_Appear", 0, 0.0f);
        UpdatePartnerTransform(); // 位置更新

        // MP消費
        if (!_isAutoLight) {
            _charaParam.ConsumeMP(eAbilityType.Light);
            _charaParam.SetUnRecoverTime_MP(1.0f);
            _currentAutoLightTimer = _autoLightTimer; // 自動発光タイマーリセット
        }

        // ライトドーム表示
        _isManualLight = true;

        if (_goalMarker != null) {
            // ゴールマーカー表示
            _goalMarker.SetMarkerActive(true);
        }

        Debug.Log("Light Parry");
        return eAbilityResult.LightParry;
    }

    public override eAbilityResult ExecuteLong() {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat || _lightDomePrefab == null) {
            ExecuteRelease();
            return eAbilityResult.None;
        }

        // 無敵化
        _charaParam.isLightInvincible = true;
        _charaParam.currentInvincibilityTimer = 0f;

        return eAbilityResult.LightDome;
    }

    public override void ExecuteRelease() {
        if (_charaParam == null) {
            return;
        }

        // ライトドーム非表示
        _isManualLight = false;

        // 帰還
        if (_isNotHide) {
            _anim?.Play("Pepe_ToHide");
        }
        // 無敵解除
        _charaParam.isLightInvincible = false;

        if(_goalMarker != null) {
            // ゴールマーカー非表示
            _goalMarker.SetMarkerActive(false);
        }
    }

    // 自動発光回避
    public void AutoAvoid() {
        if (_isManualLight) {
            return;
        }

        // アニメーション再生
        _anim?.Play("Pepe_Appear", 0, 0.0f);
        UpdatePartnerTransform(); // 位置更新

        _ResetReturnTimer();
    }

    private void _UpdateLightDomeActive() {
        if (_lightDomeInstance != null) {
            _lightDomeInstance.SetActive(_isManualLight || _isAutoLight);
        }
    }

    /// <summary>
    /// 画面外判定
    /// </summary>
    private bool _IsOutOfScreen() {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        return screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;
    }
}
