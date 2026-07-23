using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    // 各スロットの固定位置（能力UIをここへ再配置する）
    [SerializeField] private Transform _slotTransY;
    [SerializeField] private Transform _slotTransX;
    [SerializeField] private Transform _slotTransA;
    [SerializeField] private Transform _slotTransB;

    // フラッシュ用の画像（各スロット専用）
    [SerializeField] private Image _flashY;
    [SerializeField] private Image _flashX;
    [SerializeField] private Image _flashA;
    [SerializeField] private Image _flashB;

    // フラッシュエフェクトのパラメータ
    [SerializeField] private float _flashDuration = 0.2f; // 光る時間
    [SerializeField] private Color _flashColor = Color.white; // 光る色

    // 各スロットのフラッシュコルーチン管理
    private Coroutine _flashCoroutineY = null;
    private Coroutine _flashCoroutineX = null;
    private Coroutine _flashCoroutineA = null;
    private Coroutine _flashCoroutineB = null;

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
    /// 能力タイプに対応する固定のAbilityUI_Baseを取得
    /// （各能力専用のUIオブジェクトは常に同じ参照。表示位置はスロットへの再配置で変える）
    /// </summary>
    private AbilityUI_Base _GetAbilityUIForType(eAbilityType type) {
        return type switch {
            eAbilityType.Ice => _abilityUI_Y,
            eAbilityType.Light => _abilityUI_X,
            eAbilityType.Fire => _abilityUI_A,
            eAbilityType.Warp => _abilityUI_B,
            _ => null
        };
    }

    /// <summary>
    /// スロットに対応する固定位置（Ability_Trans）を取得
    /// </summary>
    private Transform _GetSlotTransform(eAbilitySlot slot) {
        return slot switch {
            eAbilitySlot.Y => _slotTransY,
            eAbilitySlot.X => _slotTransX,
            eAbilitySlot.A => _slotTransA,
            eAbilitySlot.B => _slotTransB,
            _ => null
        };
    }

    /// <summary>
    /// 現在の能力割り当て（能力タイプ→スロット）を見て、各能力UIを対応する位置へ再配置する
    /// </summary>
    public void SyncAbilityUIToCurrentSlots(System.Collections.Generic.Dictionary<eAbilityType, eAbilitySlot> abilities) {
        if (abilities == null) return;

        foreach (var pair in abilities) {
            var abilityUI = _GetAbilityUIForType(pair.Key);
            var slotTrans = _GetSlotTransform(pair.Value);
            if (abilityUI == null || slotTrans == null) continue;

            abilityUI.transform.SetParent(slotTrans, false);
            abilityUI.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// ボタン押下時の光る演出を再生（AbilityUIManager内で完結）
    /// </summary>
    /// <param name="slot">スロット指定</param>
    public void FlashAbilityUI(eAbilitySlot slot) {
        switch (slot) {
            case eAbilitySlot.Y:
                if (_flashCoroutineY != null) {
                    StopCoroutine(_flashCoroutineY);
                }
                _flashCoroutineY = StartCoroutine(_FlashEffect(_flashY));
                break;
            case eAbilitySlot.X:
                if (_flashCoroutineX != null) {
                    StopCoroutine(_flashCoroutineX);
                }
                _flashCoroutineX = StartCoroutine(_FlashEffect(_flashX));
                break;
            case eAbilitySlot.A:
                if (_flashCoroutineA != null) {
                    StopCoroutine(_flashCoroutineA);
                }
                _flashCoroutineA = StartCoroutine(_FlashEffect(_flashA));
                break;
            case eAbilitySlot.B:
                if (_flashCoroutineB != null) {
                    StopCoroutine(_flashCoroutineB);
                }
                _flashCoroutineB = StartCoroutine(_FlashEffect(_flashB));
                break;
        }
    }

    /// <summary>
    /// フラッシュエフェクトのコルーチン
    /// </summary>
    private IEnumerator _FlashEffect(Image flashImage) {
        if (flashImage == null) {
            yield break;
        }

        // フラッシュ画像を表示
        Color flashColor = _flashColor;
        flashColor.a = 1.0f;
        flashImage.color = flashColor;
        flashImage.gameObject.SetActive(true);

        float elapsedTime = 0f;

        while (elapsedTime < _flashDuration) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _flashDuration;

            // アルファ値を徐々に0にする
            flashColor.a = Mathf.Lerp(1.0f, 0.0f, t);
            flashImage.color = flashColor;

            yield return null;
        }

        // フラッシュ画像を非表示
        flashImage.gameObject.SetActive(false);
    }
}