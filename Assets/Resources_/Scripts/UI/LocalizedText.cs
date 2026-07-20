using TMPro;
using UnityEngine;

/// <summary>
/// 言語設定に応じてTMP_Textの表示文字列を切り替える
/// </summary>
public class LocalizedText : MonoBehaviour {
    [SerializeField] private TMP_Text _text;
    [SerializeField, TextArea] private string _japaneseText;
    [SerializeField, TextArea] private string _englishText;

    private void Reset() {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable() {
        _Apply();
    }

    private void _Apply() {
        if (_text == null) return;
        var language = PlayerParameter.Instance != null ? PlayerParameter.Instance.language : PlayerParameter.eLanguage.Japanese;
        _text.text = language == PlayerParameter.eLanguage.English ? _englishText : _japaneseText;
    }
}
