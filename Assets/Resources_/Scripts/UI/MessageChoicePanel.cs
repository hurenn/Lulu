using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 選択肢UI表示クラス
/// </summary>
public class MessageChoicePanel : MonoBehaviour {
    [SerializeField] private GameObject _rootPanel;        // 選択肢パネル本体(ChoiceBox)
    [SerializeField] private Animator _animator;           // _rootPanelのAnimator(Showパラメータで表示/非表示アニメを制御)
    [SerializeField] private Transform _rowContainer;       // 選択肢行を並べる親(VerticalLayoutGroup)
    [SerializeField] private MessageChoiceRow _rowTemplate; // 選択肢行の複製元(1行だけ用意すればよい)

    private readonly List<MessageChoiceRow> _rows = new List<MessageChoiceRow>();
    private Coroutine _hideCoroutine;

    private void Reset() {
        _animator = _rootPanel != null ? _rootPanel.GetComponent<Animator>() : GetComponent<Animator>();
    }

    private void Awake() {
        _rows.Add(_rowTemplate);
        if (_animator == null) _animator = _rootPanel.GetComponent<Animator>();
    }

    public void Show(MessageChoiceOption[] choices, int selectedIndex, PlayerParameter.eLanguage language) {
        if (_hideCoroutine != null) {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        _rootPanel.SetActive(true);
        _animator?.SetBool("Show", true);
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
        if (_animator != null) {
            _hideCoroutine = StartCoroutine(_HideCo());
        } else {
            _rootPanel.SetActive(false);
        }
    }

    // Showをfalseにして非表示アニメを再生し、再生し終わってからパネルを非表示にする
    private IEnumerator _HideCo() {
        _animator.SetBool("Show", false);
        yield return null; // 遷移が反映されるまで1フレーム待つ
        yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
        _rootPanel.SetActive(false);
        _hideCoroutine = null;
    }

    // プールされた行数が足りなければ、テンプレート行を複製して補充する
    private void _EnsureRowCount(int count) {
        while (_rows.Count < count) {
            _rows.Add(Instantiate(_rowTemplate, _rowContainer));
        }
    }
}
