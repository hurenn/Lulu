using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 選択肢UI表示クラス
/// </summary>
public class MessageChoicePanel : MonoBehaviour {
    [SerializeField] private GameObject _rootPanel;        // 選択肢パネル本体(ChoiceBox)
    [SerializeField] private Transform _rowContainer;       // 選択肢行を並べる親(VerticalLayoutGroup)
    [SerializeField] private MessageChoiceRow _rowTemplate; // 選択肢行の複製元(1行だけ用意すればよい)

    private readonly List<MessageChoiceRow> _rows = new List<MessageChoiceRow>();

    private void Awake() {
        _rows.Add(_rowTemplate);
    }

    public void Show(MessageChoiceOption[] choices, int selectedIndex, PlayerParameter.eLanguage language) {
        _rootPanel.SetActive(true);
        _EnsureRowCount(choices.Length);
        for (int i = 0; i < _rows.Count; i++) {
            bool has_choice = i < choices.Length;
            _rows[i].SetVisible(has_choice);
            if (has_choice) {
                _rows[i].SetLabel(MessageTextTable.GetText(choices[i].labelKey, language));
                _rows[i].SetSelected(i == selectedIndex);
            }
        }
    }

    public void UpdateCursor(int selectedIndex) {
        for (int i = 0; i < _rows.Count; i++) {
            _rows[i].SetSelected(i == selectedIndex);
        }
    }

    public void Hide() {
        _rootPanel.SetActive(false);
    }

    // プールされた行数が足りなければ、テンプレート行を複製して補充する
    private void _EnsureRowCount(int count) {
        while (_rows.Count < count) {
            _rows.Add(Instantiate(_rowTemplate, _rowContainer));
        }
    }
}
