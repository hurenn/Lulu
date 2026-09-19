using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 選択肢1行分の表示(テキスト+選択中カーソル)
public class MessageChoiceRow : MonoBehaviour {
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Image _cursor;

    public void SetLabel(string text) {
        _label.text = text;
    }

    public void SetSelected(bool selected) {
        if (_cursor != null) _cursor.gameObject.SetActive(selected);
    }

    public void SetVisible(bool visible) {
        gameObject.SetActive(visible);
    }
}
