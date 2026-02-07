using UnityEngine;

public enum eAbilitySlot {
    Y,
    X,
    A,
}

public class AbilityUIManager : MonoBehaviour {
    [SerializeField] private AbilityUI_Base _abilityUI_Y;
    [SerializeField] private AbilityUI_Base _abilityUI_X;
    [SerializeField] private AbilityUI_Base _abilityUI_A;

    /// <summary>
    /// 能力UIの設定
    /// </summary>
    /// <param name="slot">スロット指定</param>
    /// <param name="ability_type">能力タイプ</param>
    public void SetAbilityUI(eAbilitySlot slot, eAbilityType ability_type, Ability_Base ability, bool is_effect = true) {
        switch (slot) {
            case eAbilitySlot.Y:
                _abilityUI_Y.gameObject.SetActive(true);
                _abilityUI_Y.SetAbilityUI(ability_type, is_effect);
                ability.SetOnChargeSpecialCallback(_abilityUI_Y.OnChargeSpecial);
                break;
            case eAbilitySlot.X:
                _abilityUI_X.gameObject.SetActive(true);
                _abilityUI_X.SetAbilityUI(ability_type, is_effect);
                ability.SetOnChargeSpecialCallback(_abilityUI_X.OnChargeSpecial);
                break;
            case eAbilitySlot.A:
                _abilityUI_A.gameObject.SetActive(true);
                _abilityUI_A.SetAbilityUI(ability_type, is_effect);
                ability.SetOnChargeSpecialCallback(_abilityUI_A.OnChargeSpecial);
                break;
            default:
                Debug.LogError("不明なスロット：" + slot);
                break;
        }
    }

    /// <summary>
    /// 能力UIの削除（非表示）
    /// </summary>
    /// <param name="slot">スロット指定</param>
    public void RemoveAbilityUI(eAbilitySlot slot) {
        switch (slot) {
            case eAbilitySlot.Y:
                _abilityUI_Y.gameObject.SetActive(false);
                break;
            case eAbilitySlot.X:
                _abilityUI_X.gameObject.SetActive(false);
                break;
            case eAbilitySlot.A:
                _abilityUI_A.gameObject.SetActive(false);
                break;
        }
    }
}