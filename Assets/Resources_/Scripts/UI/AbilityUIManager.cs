using UnityEngine;

public enum eAbilitySlot {
    Y,
    X,
    A,
    B,
}

public class AbilityUIManager : MonoBehaviour {
    [SerializeField] private AbilityUI_Base _abilityUI_Y;
    [SerializeField] private AbilityUI_Base _abilityUI_X;
    [SerializeField] private AbilityUI_Base _abilityUI_A;
    [SerializeField] private AbilityUI_Base _abilityUI_B;

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
            case eAbilitySlot.B:
                if (_abilityUI_B != null) {
                    _abilityUI_B.gameObject.SetActive(true);
                    _abilityUI_B.SetAbilityUI(ability_type, is_effect);
                    ability.SetOnChargeSpecialCallback(_abilityUI_B.OnChargeSpecial);
                }
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
            case eAbilitySlot.B:
                if (_abilityUI_B != null) {
                    _abilityUI_B.gameObject.SetActive(false);
                }
                break;
        }
    }

    /// <summary>
    /// AbilityUI_Baseを取得
    /// </summary>
    public AbilityUI_Base GetAbilityUI(eAbilitySlot slot) {
        return slot switch {
            eAbilitySlot.Y => _abilityUI_Y,
            eAbilitySlot.X => _abilityUI_X,
            eAbilitySlot.A => _abilityUI_A,
            eAbilitySlot.B => _abilityUI_B,
            _ => null
        };
    }

    /// <summary>
    /// 二つのスロットのUIを入れ替える
    /// </summary>
    public void SwapAbilityUI(eAbilitySlot slotA, eAbilitySlot slotB) {
        var uiA = GetAbilityUI(slotA);
        var uiB = GetAbilityUI(slotB);

        if (uiA == null || uiB == null) {
            Debug.LogWarning($"UIが見つかりません: {slotA}, {slotB}");
            return;
        }

        // 親とローカル位置を保存
        var parentA = uiA.transform.parent;
        var parentB = uiB.transform.parent;
        var localPosA = uiA.transform.localPosition;
        var localPosB = uiB.transform.localPosition;

        // 親を入れ替え
        uiA.transform.SetParent(parentB, false);
        uiB.transform.SetParent(parentA, false);

        // ローカル位置をゼロに戻す
        uiA.transform.localPosition = Vector3.zero;
        uiB.transform.localPosition = Vector3.zero;

        Debug.Log($"UI入れ替え完了: {slotA} ⇔ {slotB}");
    }
}