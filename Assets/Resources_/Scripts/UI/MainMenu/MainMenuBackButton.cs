using System.Collections;
using UnityEngine;

/// <summary>
/// メインメニューの項目の画面(ステージセレクト・よみもの)の左上にある「戻る」ボタンの見た目。
/// 選択中はハイライトと選択枠を出し、選ばれた瞬間に一瞬ふくらむ。決定時の処理は各画面が行う
/// </summary>
public class MainMenuBackButton : MonoBehaviour {
    private const float SCALE_TIME = 0.15f;  // ふくらむ時間(秒)
    private const float SCALE_MAX = 1.1f;    // 最大拡大率

    [SerializeField] private GameObject _activeButton;  // 選択中だけ表示するハイライト
    [SerializeField] private GameObject _frame;         // 選択中だけ表示する選択枠

    private Coroutine _scaleCoroutine;

    private void OnDisable() {
        _scaleCoroutine = null;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 選択状態の切り替え
    /// </summary>
    /// <param name="animate">trueなら選ばれた時にふくらませる</param>
    public void SetSelected(bool selected, bool animate) {
        if (_activeButton != null) _activeButton.SetActive(selected);
        if (_frame != null) _frame.SetActive(selected);
        if (_scaleCoroutine != null) {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }
        transform.localScale = Vector3.one;
        if (selected && animate && gameObject.activeInHierarchy) {
            _scaleCoroutine = StartCoroutine(_ScaleAnim());
        }
    }

    private IEnumerator _ScaleAnim() {
        float t = 0f;
        while (t < SCALE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / SCALE_TIME);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, SCALE_MAX, Mathf.Sin(rate * Mathf.PI));
            yield return null;
        }
        transform.localScale = Vector3.one;
        _scaleCoroutine = null;
    }
}
