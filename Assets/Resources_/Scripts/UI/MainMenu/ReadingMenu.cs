using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メインメニューの「よみもの」。画面左に読み物の一覧の窓、決定で画面中央から右側に内容の窓を大きく開く。
/// 一覧: 上下で選ぶ、決定で内容を開く、Bでメインメニューへ戻る。
/// 一覧の一番上からさらに上で左上の「戻る」ボタンを選べ、決定するとメインメニューへ戻る。
/// 内容: 上下で本文をスクロール、決定かBで閉じて一覧へ戻る。
/// 一覧の中身は ReadingTable(Assets/Resources/Localization/Readings.json)から作る
/// </summary>
public class ReadingMenu : MainMenuSubScreen {
    private const float INPUT_THRESHOLD = 0.5f;     // スティックを倒したとみなす量
    private const float OPEN_TIME = 0.25f;          // 一覧の窓が出てくる時間(秒)
    private const float CLOSE_FADE_TIME = 0.15f;    // 画面を閉じる時のフェード時間(秒)
    private const float CONTENT_OPEN_TIME = 0.22f;  // 内容の窓が開く時間(秒)
    private const float CONTENT_CLOSE_TIME = 0.12f; // 内容の窓が閉じる時間(秒)
    private const float LIST_SLIDE = 80f;           // 一覧の窓が左から滑り込む距離
    private const float FOLLOW_SPEED = 20f;         // カーソル・一覧のスクロールが追いつく速さ
    private const float LIST_DIM_ALPHA = 0.6f;      // 内容を読んでいる間の一覧の濃さ
    private const int BACK_INDEX = -1;              // 「戻る」ボタンを選んでいる時のカーソル位置

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private MainMenuBackButton _backButton;  // 左上の「戻る」ボタン

    [Header("一覧の窓")]
    [SerializeField] private RectTransform _listWindow;
    [SerializeField] private CanvasGroup _listGroup;
    [SerializeField] private RectTransform _listViewport;   // 一覧の見える範囲(RectMask2D)
    [SerializeField] private RectTransform _listContent;    // 行を並べる親(上端基準)
    [SerializeField] private TMP_Text _rowTemplate;         // 行の雛形(非表示にしておく)
    [SerializeField] private RectTransform _cursor;         // 選択中の行の後ろに敷くカーソル
    [SerializeField] private float _rowHeight = 80f;

    [Header("内容の窓")]
    [SerializeField] private RectTransform _contentWindow;  // 左端を基準(pivot x=0)にして右へ開く
    [SerializeField] private CanvasGroup _contentGroup;
    [SerializeField] private TMP_Text _contentTitle;
    [SerializeField] private RectTransform _contentViewport; // 本文の見える範囲(RectMask2D)
    [SerializeField] private TMP_Text _contentBody;          // 本文(上端基準、高さは文章に合わせる)
    [SerializeField] private float _scrollSpeed = 900f;      // 本文のスクロールの速さ(Canvas上の単位/秒)

    [Header("効果音")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;
    [SerializeField] private AudioClip _seDecide;
    [SerializeField] private AudioClip _seCancel;

    private enum eState {
        Closed,
        List,      // 一覧を選んでいる
        Content,   // 内容を読んでいる
    }
    private eState _state = eState.Closed;

    private readonly List<TMP_Text> _rows = new List<TMP_Text>();
    private IReadOnlyList<ReadingTable.Entry> _entries;
    private int _currentIndex;
    private int _lastRowIndex;             // 最後に選んでいた行(「戻る」から下りる時・開き直した時に使う)
    private bool _isMoveHeld;
    private int _inputLockFrame;           // このフレームの入力は使わない(開いた・切り替えたフレーム)
    private System.Action _onCancel;
    private Vector2 _listWindowBasePos;
    private Coroutine _openCoroutine;
    private Coroutine _contentCoroutine;
    private Coroutine _closeCoroutine;

    private void Awake() {
        if (_listWindow != null) _listWindowBasePos = _listWindow.anchoredPosition;
        if (_rowTemplate != null) _rowTemplate.gameObject.SetActive(false);
    }

    /// <summary>
    /// 開く。一覧の窓を左から滑り込ませる
    /// </summary>
    public override void Open(System.Action onCancel) {
        _onCancel = onCancel;
        gameObject.SetActive(true);
        _StopAll();
        _BuildRows();

        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        if (_contentWindow != null) _contentWindow.gameObject.SetActive(false);
        if (_listGroup != null) _listGroup.alpha = 0f;
        _currentIndex = Mathf.Clamp(_lastRowIndex, 0, Mathf.Max(0, _entries.Count - 1));
        if (_backButton != null) _backButton.SetSelected(false, animate: false);
        if (_cursor != null) _cursor.gameObject.SetActive(true);
        _SnapCursor();

        _state = eState.List;
        _isMoveHeld = true;  // 開いた時に倒していた方向では動かさない
        _inputLockFrame = Time.frameCount;
        _openCoroutine = StartCoroutine(_OpenListAnim());
    }

    /// <summary>
    /// 閉じる(フェードアウトして非表示にする)
    /// </summary>
    public override void Close() {
        _state = eState.Closed;
        if (!gameObject.activeInHierarchy) return;
        _StopAll();
        _closeCoroutine = StartCoroutine(_FadeOutAndHide());
    }

    private void _StopAll() {
        if (_openCoroutine != null) StopCoroutine(_openCoroutine);
        if (_contentCoroutine != null) StopCoroutine(_contentCoroutine);
        if (_closeCoroutine != null) StopCoroutine(_closeCoroutine);
        _openCoroutine = _contentCoroutine = _closeCoroutine = null;
    }

    // 一覧の行を作る(言語が変わっていてもよいように開くたびに文字を入れ直す)
    private void _BuildRows() {
        _entries = ReadingTable.Entries;
        var language = PlayerParameter.Instance != null ? PlayerParameter.Instance.language : PlayerParameter.eLanguage.Japanese;
        while (_rows.Count < _entries.Count) {
            var row = Instantiate(_rowTemplate, _listContent);
            row.name = "Row_" + _rows.Count;
            _rows.Add(row);
        }
        for (int i = 0; i < _rows.Count; i++) {
            bool used = i < _entries.Count;
            _rows[i].gameObject.SetActive(used);
            if (!used) continue;
            _rows[i].text = _entries[i].GetTitle(language);
            var rt = _rows[i].rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -_rowHeight * (i + 0.5f));
        }
        _listContent.sizeDelta = new Vector2(_listContent.sizeDelta.x, _rowHeight * _entries.Count);
    }

    private void Update() {
        if (_state == eState.Closed) return;
        _UpdateCursor();

        if (_playerController == null) {
            _playerController = PlayerCharacterManager.Controller;
            if (_playerController == null) return;
        }
        if (Time.frameCount == _inputLockFrame) return;

        var input = _playerController.Input;
        int dy = input.move.y > INPUT_THRESHOLD ? 1 : input.move.y < -INPUT_THRESHOLD ? -1 : 0;

        if (_state == eState.List) {
            int dx = input.move.x > INPUT_THRESHOLD ? 1 : input.move.x < -INPUT_THRESHOLD ? -1 : 0;
            if (dx == 0 && dy == 0) {
                _isMoveHeld = false;
            } else if (!_isMoveHeld) {
                _isMoveHeld = true;
                int next = _GetNextIndex(dx, dy);
                if (next != _currentIndex) _Select(next);
            }

            if (input.abilityB.pressed || (input.messageNextPressed && _currentIndex == BACK_INDEX)) {
                _PlaySe(_seCancel);
                Close();
                _onCancel?.Invoke();
            } else if (input.messageNextPressed && _entries != null && _IsRowActive(_currentIndex)) {
                _PlaySe(_seDecide);
                _OpenContent(_entries[_currentIndex]);
            }
        } else if (_state == eState.Content) {
            // 倒している間、本文をスクロールする
            if (dy != 0) _ScrollBody(-dy * _scrollSpeed * Time.unscaledDeltaTime);

            if (input.abilityB.pressed || input.messageNextPressed) {
                _PlaySe(_seCancel);
                _CloseContent();
            }
        }
    }

    // 方向入力からカーソルの移動先を求める(動けない時は今の位置を返す)
    private int _GetNextIndex(int dx, int dy) {
        if (_currentIndex == BACK_INDEX) {
            // 「戻る」から下か右で、最後に選んでいた行へ
            return dy < 0 || dx > 0 ? _lastRowIndex : BACK_INDEX;
        }
        if (dy > 0) {
            // 上: 前の行へ。一番上からさらに上で「戻る」へ
            if (_currentIndex > 0) return _currentIndex - 1;
            return _backButton != null ? BACK_INDEX : _currentIndex;
        }
        if (dy < 0 && _IsRowActive(_currentIndex + 1)) return _currentIndex + 1;
        return _currentIndex;
    }

    private void _Select(int index) {
        if (index == _currentIndex) return;
        if (_currentIndex == BACK_INDEX) _backButton.SetSelected(false, animate: false);
        _currentIndex = index;
        if (_currentIndex == BACK_INDEX) {
            _backButton.SetSelected(true, animate: true);
        } else {
            _lastRowIndex = _currentIndex;
        }
        // 「戻る」を選んでいる間は一覧のカーソルを隠す
        if (_cursor != null) {
            bool showCursor = _currentIndex != BACK_INDEX;
            if (showCursor && !_cursor.gameObject.activeSelf) {
                _cursor.gameObject.SetActive(true);
                _SnapCursor();
            } else {
                _cursor.gameObject.SetActive(showCursor);
            }
        }
        _PlaySe(_seSelect);
    }

    private bool _IsRowActive(int index) => index >= 0 && index < _rows.Count && _rows[index].gameObject.activeSelf;

    // カーソルを選択中の行へなめらかに動かし、一覧を選択中の行が見える位置までスクロールする
    private void _UpdateCursor() {
        if (!_IsRowActive(_currentIndex)) return;
        float rate = 1f - Mathf.Exp(-FOLLOW_SPEED * Time.unscaledDeltaTime);
        var target = _rows[_currentIndex].rectTransform.anchoredPosition;
        if (_cursor != null) {
            _cursor.anchoredPosition = Vector2.Lerp(_cursor.anchoredPosition, new Vector2(_cursor.anchoredPosition.x, target.y), rate);
        }
        float scrollY = Mathf.Lerp(_listContent.anchoredPosition.y, _ListScrollTarget(), rate);
        _listContent.anchoredPosition = new Vector2(_listContent.anchoredPosition.x, scrollY);
    }

    private void _SnapCursor() {
        if (!_IsRowActive(_currentIndex)) return;
        if (_cursor != null) {
            _cursor.anchoredPosition = new Vector2(_cursor.anchoredPosition.x, _rows[_currentIndex].rectTransform.anchoredPosition.y);
        }
        _listContent.anchoredPosition = new Vector2(_listContent.anchoredPosition.x, _ListScrollTarget());
    }

    // 選択中の行が見える範囲に収まる一覧のスクロール位置
    private float _ListScrollTarget() {
        float viewHeight = _listViewport != null ? _listViewport.rect.height : 0f;
        float maxScroll = Mathf.Max(0f, _rowHeight * _entries.Count - viewHeight);
        float rowTop = _rowHeight * _currentIndex;
        float rowBottom = rowTop + _rowHeight;
        float scroll = _listContent.anchoredPosition.y;
        if (rowTop < scroll) scroll = rowTop;
        else if (rowBottom > scroll + viewHeight) scroll = rowBottom - viewHeight;
        return Mathf.Clamp(scroll, 0f, maxScroll);
    }

    private void _ScrollBody(float delta) {
        if (_contentBody == null || _contentViewport == null) return;
        var rt = _contentBody.rectTransform;
        float maxScroll = Mathf.Max(0f, rt.rect.height - _contentViewport.rect.height);
        float y = Mathf.Clamp(rt.anchoredPosition.y + delta, 0f, maxScroll);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    // 内容の窓を開く
    private void _OpenContent(ReadingTable.Entry entry) {
        var language = PlayerParameter.Instance != null ? PlayerParameter.Instance.language : PlayerParameter.eLanguage.Japanese;
        if (_contentTitle != null) _contentTitle.text = entry.GetTitle(language);
        if (_contentBody != null) {
            _contentBody.text = entry.GetBody(language);
            // 本文の高さを文章に合わせ、先頭から表示する
            var rt = _contentBody.rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, _contentBody.GetPreferredValues(entry.GetBody(language), rt.rect.width, 0f).y);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        }

        _state = eState.Content;
        _inputLockFrame = Time.frameCount;
        if (_contentCoroutine != null) StopCoroutine(_contentCoroutine);
        _contentCoroutine = StartCoroutine(_ContentAnim(open: true));
    }

    private void _CloseContent() {
        _state = eState.List;
        _isMoveHeld = true;
        _inputLockFrame = Time.frameCount;
        if (_contentCoroutine != null) StopCoroutine(_contentCoroutine);
        _contentCoroutine = StartCoroutine(_ContentAnim(open: false));
    }

    // 内容の窓: 開く時は左端を軸に横へ広がりながら現れ、閉じる時は少し縮みながら消える。一覧は読んでいる間だけ薄くする
    private IEnumerator _ContentAnim(bool open) {
        _contentWindow.gameObject.SetActive(true);
        float duration = open ? CONTENT_OPEN_TIME : CONTENT_CLOSE_TIME;
        float startAlpha = _contentGroup != null ? _contentGroup.alpha : (open ? 0f : 1f);
        if (open) startAlpha = 0f;
        float listFrom = _listGroup != null ? _listGroup.alpha : 1f;
        float listTo = open ? LIST_DIM_ALPHA : 1f;
        float t = 0f;
        while (t < duration) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / duration);
            if (open) {
                // 少し大きく広がりすぎてから戻る(イーズアウトバック)
                const float BACK = 1.4f;
                float x = rate - 1f;
                float eased = 1f + (BACK + 1f) * x * x * x + BACK * x * x;
                _contentWindow.localScale = new Vector3(Mathf.Lerp(0.3f, 1f, eased), Mathf.Lerp(0.85f, 1f, eased), 1f);
                if (_contentGroup != null) _contentGroup.alpha = Mathf.Lerp(startAlpha, 1f, rate * 2f);
            } else {
                _contentWindow.localScale = Vector3.one * Mathf.Lerp(1f, 0.95f, rate);
                if (_contentGroup != null) _contentGroup.alpha = Mathf.Lerp(startAlpha, 0f, rate);
            }
            if (_listGroup != null) _listGroup.alpha = Mathf.Lerp(listFrom, listTo, rate);
            yield return null;
        }
        _contentWindow.localScale = Vector3.one;
        if (_contentGroup != null) _contentGroup.alpha = open ? 1f : 0f;
        if (_listGroup != null) _listGroup.alpha = listTo;
        if (!open) _contentWindow.gameObject.SetActive(false);
        _contentCoroutine = null;
    }

    // 一覧の窓を左から滑り込ませる
    private IEnumerator _OpenListAnim() {
        float t = 0f;
        while (t < OPEN_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / OPEN_TIME);
            float eased = 1f - Mathf.Pow(1f - rate, 3f);
            _listWindow.anchoredPosition = _listWindowBasePos + Vector2.left * (LIST_SLIDE * (1f - eased));
            if (_listGroup != null) _listGroup.alpha = eased;
            yield return null;
        }
        _listWindow.anchoredPosition = _listWindowBasePos;
        if (_listGroup != null) _listGroup.alpha = 1f;
        _openCoroutine = null;
    }

    private IEnumerator _FadeOutAndHide() {
        float start = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        float t = 0f;
        while (t < CLOSE_FADE_TIME) {
            t += Time.unscaledDeltaTime;
            if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Lerp(start, 0f, t / CLOSE_FADE_TIME);
            yield return null;
        }
        _listWindow.anchoredPosition = _listWindowBasePos;
        _closeCoroutine = null;
        gameObject.SetActive(false);
    }

    private void _PlaySe(AudioClip clip) {
        if (_audioSource != null && clip != null) {
            _audioSource.PlayOneShot(clip);
        }
    }
}
