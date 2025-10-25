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
    public void SetAbilityUI(eAbilitySlot slot, eAbilityType ability_type, bool is_effect = true) {
        switch (slot) {
            case eAbilitySlot.Y:
                _abilityUI_Y.gameObject.SetActive(true);
                _abilityUI_Y.SetAbilityUI(ability_type, is_effect);
                break;
            case eAbilitySlot.X:
                _abilityUI_X.gameObject.SetActive(true);
                _abilityUI_X.SetAbilityUI(ability_type, is_effect);
                break;
            case eAbilitySlot.A:
                _abilityUI_A.gameObject.SetActive(true);
                _abilityUI_A.SetAbilityUI(ability_type, is_effect);
                break;
            default:
                Debug.LogError("不明なスロット：" + slot);
                break;
        }
    }
}