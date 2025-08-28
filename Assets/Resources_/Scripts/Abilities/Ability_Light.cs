using UnityEngine;

public class Ability_Light : Ability_Base
{
    [SerializeField] private GameObject _lightDomePrefab;
    private GameObject _lightDomeInstance;

    private void Update() {
        if( _rend.color.a > 0 && _IsOutOfScreen()) {
            // 画面外に出たら非表示にする
            _anim?.Play("Pepe_ToHide");
        }
    }

    public override eAbilityResult ExecuteSimple() {
        if (_lightDomePrefab == null) {
            return eAbilityResult.None;
        }

        // アニメーション再生
        _anim?.Play("Pepe_Appear", 0, 0.0f);
        UpdateTransform(_charaTransform.position, _inputDir); // 位置更新

        if (_lightDomeInstance == null) {
            _lightDomeInstance = Instantiate(_lightDomePrefab, _charaTransform);
        }
        _lightDomeInstance.SetActive(true);

        Debug.Log("Light Parry");
        return eAbilityResult.LightParry;
    }

    public override eAbilityResult ExecuteLong() {
        return eAbilityResult.LightDome;
    }

    public override void ExecuteRelease() {
        if (_lightDomeInstance != null) {
            _lightDomeInstance.SetActive(false);
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
