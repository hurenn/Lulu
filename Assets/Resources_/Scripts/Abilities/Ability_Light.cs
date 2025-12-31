using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Ability_Light : Ability_Base
{
    [SerializeField] private GameObject _lightDomePrefab;
    private GameObject _lightDomeInstance;
    private SpriteRenderer _lightDomeRenderer;
    private float _lightDomeDefaultAlpha = 0.5f;
    private Light2D _light;
    private float _lightDefaultIntensity = 0.7f;

    private bool _isNotHide => _rend.color.a > 0.9f;
    private GoalMarker _goalMarker;

    // 自動発光タイマー
    private bool _isAutoLight = false;
    private bool _isManualLight = false;

    // ジャスト回避時間
    private float _justAvoidTime = 0.2f;
    private float _currentJustAvoidTime = 0.0f;
    private float _justAvoidCooltime = 0.5f;
    private float _currentJustAvoidCooltime = 0.0f;
    public bool canJustAvoid => _currentJustAvoidTime > 0f;
    // ジャスト回避タイマーリセット
    private void _ResetJustAvoidTimer() {
        if (_currentJustAvoidCooltime > 0f) {
            return;
        }
        _currentJustAvoidTime = _justAvoidTime;
        _currentJustAvoidCooltime = _justAvoidCooltime;
    }

    public override void UpdateParameter(bool is_right, Transform chara_pos, CommonParameter common_param, CharacterParameter_Player chara_param, WarpControl warp_control, MotorStates motor_states) {
        base.UpdateParameter(is_right, chara_pos, common_param, chara_param, warp_control, motor_states);

        var goal_marker = FindAnyObjectByType<GoalMarker>();
        if (goal_marker != null) {
            _goalMarker = goal_marker;
        }

        if (_lightDomeInstance == null) {
            _lightDomeInstance = Instantiate(_lightDomePrefab, _playerTransform);
            _lightDomeRenderer = _lightDomeInstance.GetComponentInChildren<SpriteRenderer>();
            if (_lightDomeRenderer != null) {
                _lightDomeDefaultAlpha = _lightDomeRenderer.color.a;
            }
            _light = _lightDomeInstance.GetComponentInChildren<Light2D>();
            if (_light != null) {
                _lightDefaultIntensity = _light.intensity;
            }
        }
    }

    protected override void _Update() {
        if (_isNotHide && _IsOutOfScreen()) {
            // 画面外に出たら非表示にする
            _anim?.Play("Pepe_ToHide");
        }

        // ジャスト回避タイマー更新
        if (_currentJustAvoidTime > 0f) {
            _currentJustAvoidTime -= Time.deltaTime;
        }
        if (_currentJustAvoidCooltime > 0f) {
            _currentJustAvoidCooltime -= Time.deltaTime;
        }

        _UpdateLightDomeActive();
    }

    // 自動発光設定
    public void SetAutoLight(bool is_active) {
        if(_charaParam == null) {
            return;
        }
        // ジャスト回避タイマーリセット
        _ResetJustAvoidTimer();

        _isAutoLight = is_active && !_charaParam.isOverheat;
        _charaParam.isAutoLightInvincible = _isAutoLight;
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if ((_charaParam.isOverheat && !_isAppearing) || _lightDomePrefab == null) {
            UpdatePartnerTransform(); // 位置更新
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }

        // MP消費
        if (!_isAppearing) {
            _charaParam.ConsumeMP(eAbilityType.Light);
            _charaParam.SetUnRecoverTime_MP(1.0f);
        }

        // アニメーション再生
        _anim?.Play("Pepe_Appear", 0, 0.0f);
        UpdatePartnerTransform(); // 位置更新
        _ResetReturnTimer();

        // ライトドーム表示
        _isManualLight = true;

        if (_goalMarker != null) {
            // ゴールマーカー表示
            _goalMarker.SetMarkerActive(true);
        }

        // ジャスト回避タイマーリセット
        _ResetJustAvoidTimer();

        return eAbilityResult.LightParry;
    }

    public override eAbilityResult ExecuteLong() {
        // オーバーヒート中は使用不可
        if (_charaParam.isOverheat || _lightDomePrefab == null) {
            ExecuteRelease();
            return eAbilityResult.None;
        }

        // 必殺チャージ停止
        if (_isManualLight) {
            _StopSpecialCharge();
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

        // 手動発光解除
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
        if (_isManualLight || _isNotHide) {
            return;
        }

        // アニメーション再生
        _anim?.Play("Pepe_Appear", 0, 0.0f);
        UpdatePartnerTransform(); // 位置更新

        _ResetReturnTimer();
    }

    /// <summary>
    /// ジャスト回避
    /// </summary>
    public void _StartJustAvoid() {
        if (_charaParam == null) {
            return;
        }
        
        // コルーチンを開始（MonoBehaviourのStartCoroutineが必要なため、呼び出し元で実行）
        StartCoroutine(_AutoAvoidSlowMotionCoroutine());
    }

    /// <summary>
    /// 自動回避スローモーションのコルーチン
    /// </summary>
    private IEnumerator _AutoAvoidSlowMotionCoroutine() {
        float slow_duration = 1.0f;  // スロー持続時間（1秒)
        float slow_scale = 0.01f;    // スロー倍率（0.01倍速）
        
        // 元のタイムスケールを保存
        float original_time_scale = Time.timeScale;
        
        // スローモーション開始
        Time.timeScale = slow_scale;
        
        // 実時間で1秒待機
        yield return new WaitForSecondsRealtime(slow_duration);
        
        // タイムスケールを元に戻す
        Time.timeScale = original_time_scale;
    }

    private void _UpdateLightDomeActive() {
        if(_lightDomeRenderer == null) {
            return;
        }
        var light_color = _lightDomeRenderer.color;
        if (_isManualLight || _isAutoLight) {
            // 発光中は徐々に明るくする
            light_color.a = Mathf.Min(light_color.a + Time.deltaTime * 30.0f, _lightDomeDefaultAlpha);
            if (_light != null) {
                _light.intensity = Mathf.Min(_light.intensity + Time.deltaTime * 2.0f, _lightDefaultIntensity);
            }
        } else {
            // すぐに暗くする
            if (light_color.a < _lightDomeDefaultAlpha / 2) {
                light_color.a = 0;
                if (_light != null) {
                    _light.intensity = 0;
                }
            } else {
                light_color.a = light_color.a / 2;
                if (_light != null) {
                    _light.intensity = _light.intensity / 2;
                }
            }
        }
        _lightDomeRenderer.color = light_color;
    }

    /// <summary>
    /// 画面外判定
    /// </summary>
    private bool _IsOutOfScreen() {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        return screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;
    }
}
