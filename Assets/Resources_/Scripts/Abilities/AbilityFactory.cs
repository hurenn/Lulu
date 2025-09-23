using System;
using UnityEngine;

[Serializable]
public enum eAbilityType {
    None,
    Ice,
    Fire,
    Light,
    Warp,
}

public static class AbilityFactory {
    // 能力UI全体管理
    private static AbilityUIManager _abilityUIManager = null;

    /// <summary>
    /// 能力生成
    /// </summary>
    /// <param name="type">能力の種類</param>
    /// <returns>生成した能力</returns>
    public static Ability_Base CreateAbility(
        eAbilityType type, eAbilitySlot slot) {
        if (type == eAbilityType.None) {
            Debug.LogError("Ability type is None");
            return null;
        }

        // 能力生成
        Ability_Base ability = null;
        switch (type) {
            case eAbilityType.Ice:
                ability = UnityEngine.Object.Instantiate(Resources.Load<Ability_Ice>("Prefabs/Abilities/Ability_Ice"));
                break;
            case eAbilityType.Fire:
                ability = UnityEngine.Object.Instantiate(Resources.Load<Ability_Fire>("Prefabs/Abilities/Ability_Fire"));
                break;
            case eAbilityType.Light:
                ability = UnityEngine.Object.Instantiate(Resources.Load<Ability_Light>("Prefabs/Abilities/Ability_Light"));
                break;
            default:
                Debug.LogError("能力タイプが見つかりませんでした");
                break;
        }
        if(ability == null) {
            Debug.LogError("能力生成失敗：" + type);
            return null;
        }

        // UI更新
        if(_abilityUIManager == null) {
            _abilityUIManager = GameObject.FindObjectOfType<AbilityUIManager>();
            if(_abilityUIManager == null) {
                Debug.LogError("AbilityUIManagerが見つかりません");
                return ability;
            }
        }
        _abilityUIManager.SetAbilityUI(slot, type);

        return ability;
    }
}
