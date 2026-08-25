using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum eAbilitySlot {
    Y,
    X,
    A,
    B,
}

public class AbilityUIManager : MonoBehaviour {
    // 能力タイプごとの専用UI要素（それぞれ固有のアイコン・エフェクトを持つ固定オブジェクト）
    // フィールド名はY/X/A/Bだが、実体はデフォルトの割り当て（Ice→Y, Light→X, Fire→A, Warp→B）に
    // 対応するAbilityUI_Baseへの固定参照であり、スロットの入れ替えに応じてTransformを再配置して使う
    [SerializeField] private AbilityUI_Base _abilityUI_Y;
    [SerializeField] private AbilityUI_Base _abilityUI_X;
    [SerializeField] private AbilityUI_Base _abilityUI_A;
    [SerializeField] private AbilityUI_Base _abilityUI_B;

    // フラッシュ用の画像（各スロット専用、能力の入れ替えに関わらず位置固定）
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

    // 能力タイプ→専用UI要素
    private Dictionary<eAbilityType, AbilityUI_Base> _abilityUIByType;
    // スロット→配置先コンテナ（各UI要素の起動時点の親を、そのスロットの画面位置として記憶する）
    private Dictionary<eAbilitySlot, Transform> _slotContainers;

    private void Awake() {
        _abilityUIByType = new Dictionary<eAbilityType, AbilityUI_Base> {
            { eAbilityType.Ice, _abilityUI_Y },
            { eAbilityType.Light, _abilityUI_X },
            { eAbilityType.Fire, _abilityUI_A },
            { eAbilityType.Warp, _abilityUI_B },
        };
        _slotContainers = new Dictionary<eAbilitySlot, Transform> {
            { eAbilitySlot.Y, _abilityUI_Y.transform.parent },
            { eAbilitySlot.X, _abilityUI_X.transform.parent },
            { eAbilitySlot.A, _abilityUI_A.transform.parent },
        };
        if (_abilityUI_B != null) {
            _slotContainers[eAbilitySlot.B] = _abilityUI_B.transform.parent;
        }
    }

    /// <summary>
    /// 能力UIの設定（能力タイプ専用のUI要素を、指定スロットの画面位置へ配置する）
    /// </summary>
    /// <param name="slot">スロット指定</param>
    /// <param name="ability_type">能力タイプ</param>
    public void SetAbilityUI(eAbilitySlot slot, eAbilityType ability_type, Ability_Base ability, bool is_effect = true) {
        if (!_abilityUIByType.TryGetValue(ability_type, out var ui) || ui == null) {
            Debug.LogError("不明な能力タイプ：" + ability_type);
            return;
        }
        if (!_slotContainers.TryGetValue(slot, out var container) || container == null) {
            Debug.LogError("不明なスロット：" + slot);
            return;
        }

        // このスロットの位置に配置（既に別の能力UIがあれば非表示にする）
        RemoveAbilityUI(slot);

        ui.transform.SetParent(container, false);
        ui.transform.localPosition = Vector3.zero;
        ui.gameObject.SetActive(true);
        ui.SetAbilityUI(ability_type, is_effect);
        ability.SetOnChargeSpecialCallback(ui.OnChargeSpecial);
    }

    /// <summary>
    /// 能力UIの削除（非表示）。指定スロットに現在配置されている能力UIを非表示にする
    /// </summary>
    /// <param name="slot">スロット指定</param>
    public void RemoveAbilityUI(eAbilitySlot slot) {
        if (!_slotContainers.TryGetValue(slot, out var container) || container == null) {
            return;
        }
        var current = container.GetComponentInChildren<AbilityUI_Base>(false);
        if (current != null) {
            current.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 指定スロットに現在配置されているAbilityUI_Baseを取得
    /// </summary>
    public AbilityUI_Base GetAbilityUI(eAbilitySlot slot) {
        if (!_slotContainers.TryGetValue(slot, out var container) || container == null) {
            return null;
        }
        return container.GetComponentInChildren<AbilityUI_Base>(true);
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