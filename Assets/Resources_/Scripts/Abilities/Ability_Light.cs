using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class Ability_Light : Ability_Base
{
    [SerializeField] private GameObject _lightDomePrefab;
    private GameObject _lightDomeInstance;

    public override eAbilityResult ExecuteSimple(Vector3 character_pos) {
        if (_lightDomePrefab == null) {
            return eAbilityResult.None;
        }

        if (_lightDomeInstance == null) {
            _lightDomeInstance = Instantiate(_lightDomePrefab, transform);
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
}
