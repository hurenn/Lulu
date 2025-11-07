using UnityEngine;

public class Ability_Light : Ability_Base
{
    [SerializeField] private GameObject _lightDomePrefab;
    private GameObject _lightDomeInstance;

    private bool _isNotHide => _rend.color.a > 0.9f;
    private GoalMarker _goalMarker;

    public override void Setup(bool is_right, Transform chara_pos, CommonParameter common_param, CharacterParameter_Player chara_param, WarpControl warp_control) {
        base.Setup(is_right, chara_pos, common_param, chara_param, warp_control);

        var goal_marker = FindAnyObjectByType<GoalMarker>();
        if (goal_marker != null) {
            _goalMarker = goal_marker;
        }
    }

    private void Update() {
        if (_isNotHide && _IsOutOfScreen()) {
            // 画面外に出たら非表示にする
            _anim?.Play("Pepe_ToHide");
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
        _charaParam.ConsumeMP(eAbilityType.Light);
        _charaParam.SetUnRecoverTime_MP(1.0f);

        if (_lightDomeInstance == null) {
            _lightDomeInstance = Instantiate(_lightDomePrefab, _playerTransform);
        }
        _lightDomeInstance.SetActive(true);

        if(_goalMarker != null) {
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

        // MP回復不可
        //_charaParam.SetUnRecoverTime_MP(1.0f);

        // 無敵化
        _charaParam.isLightInvincible = true;
        _charaParam.currentInvincibilityTimer = 0f;

        return eAbilityResult.LightDome;
    }

    public override void ExecuteRelease() {
        if (_charaParam == null) {
            return;
        }

        if (_lightDomeInstance != null) {
            _lightDomeInstance.SetActive(false);
        }
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

    /// <summary>
    /// 画面外判定
    /// </summary>
    private bool _IsOutOfScreen() {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        return screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;
    }
}
