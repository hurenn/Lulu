using System;
using UnityEngine;

[Serializable]
public enum eAbilityType {
    None,
    Ice,
    Fire,
    Light,
    Warp,
    // ここまで固定

    LockonSlash,
    AutoSlash,
    LightAvoid,
    LightAutoAvoid,
    WarpExecute,
}

public static class AbilityFactory {
    // 能力タイプごとのプレハブパス（新しい能力はここに1行追加するだけでよい）
    private static readonly System.Collections.Generic.Dictionary<eAbilityType, string> _abilityPrefabPaths =
        new System.Collections.Generic.Dictionary<eAbilityType, string> {
            { eAbilityType.Ice, "Prefabs/Abilities/Ability_Ice" },
            { eAbilityType.Fire, "Prefabs/Abilities/Ability_Fire" },
            { eAbilityType.Light, "Prefabs/Abilities/Ability_Light" },
            { eAbilityType.Warp, "Prefabs/Abilities/Ability_Warp" },
        };

    // 能力UI全体管理
    private static AbilityUIManager _AUMInstance = null;
    private static AbilityUIManager _AbilityUIManager {
        get {
            if (_AUMInstance == null) {
                _AUMInstance = GameObject.FindAnyObjectByType<AbilityUIManager>();
                if(_AUMInstance == null) {
                    Debug.LogError("AbilityUIManagerが見つかりません");
                }
            }
            return _AUMInstance;
        }
    }

    /// <summary>
    /// 能力生成
    /// </summary>
    /// <param name="type">能力の種類</param>
    /// <returns>生成した能力</returns>
    public static Ability_Base CreateAbility(
        eAbilityType type, eAbilitySlot slot, Action<string> onStartSpecialAnim, Action onEndSpecial, bool is_effect = true) {
        if (type == eAbilityType.None) {
            return null;
        }

        // 能力生成
        Ability_Base ability = null;
        if (_abilityPrefabPaths.TryGetValue(type, out string prefab_path)) {
            ability = UnityEngine.Object.Instantiate(Resources.Load<Ability_Base>(prefab_path));
        } else {
            Debug.LogError("能力タイプが見つかりませんでした");
        }
        if(ability == null) {
            Debug.LogError("能力生成失敗：" + type);
            return null;
        }

        // 必殺技コールバック設定
        ability.SetOnStartSpecialAnim(onStartSpecialAnim);
        ability.SetOnEndSpecialAttack(onEndSpecial);

        // UI更新
        _AbilityUIManager?.SetAbilityUI(slot, type, ability, is_effect);

        return ability;
    }

    /// <summary>
    /// 特定の能力が装備されているか確認
    /// </summary>
    /// <typeparam name="T">確認したい能力の型</typeparam>
    /// <returns>装備されていればtrue</returns>
    public static void DestroyAbility(Ability_Base ability, eAbilitySlot slot) {
        // シーン中から対象の能力を探す
        ability.DestroyAbility();

        // UI更新
        _AbilityUIManager?.RemoveAbilityUI(slot);

        // PlayerParameterから削除
        var playerParam = PlayerParameter.Instance;
        if (playerParam != null) {
            playerParam.RemoveAbility(slot);
        }
    }
}
