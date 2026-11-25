using System.Collections;
using UnityEngine;

/// <summary>
/// ステージセレクト(黒板に貼った10枚の紙からステージを選ぶ)。
/// 方向入力でカーソル移動(横5枚×2段)、決定で各ステージの最初のエリアを開始する。
/// 左上の「戻る」ボタン(上の段からさらに上で選べる)を決定するか、いつでもBボタンでメインメニューへ戻る。
/// このプロジェクトはABXYのどれでも「決定」になるため、ここではBだけをキャンセルとして扱う。
/// 画面の切り替え(ズーム・クロスフェード)はMainMenuManagerが行う
/// </summary>
public class StageSelectMenu : MonoBehaviour {
    private const int COLUMNS = 5;                // 1段に並べる枚数
    private const float INPUT_THRESHOLD = 0.5f;   // スティックを倒したとみなす量
    private const float CLOSE_FADE_TIME = 0.15f;  // 閉じる時のフェード時間(秒)
    private const int BACK_INDEX = -1;            // 「戻る」ボタンを選んでいる時のカーソル位置
    private const float BUTTON_SCALE_TIME = 0.15f; // 「戻る」ボタンを選んだ時の拡縮時間(秒)
    private const float BUTTON_SCALE_MAX = 1.1f;   // 「戻る」ボタンの最大拡大率

    [SerializeField] private StageSelectPaper[] _papers;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private float _appearInterval = 0.04f;  // 紙が1枚ずつ出てくる間隔(秒)

    [Header("戻るボタン")]
    [SerializeField] private RectTransform _backButton;
    [SerializeField] private GameObject _backActiveButton;   // 選択中だけ表示するハイライト
    [SerializeField] private GameObject _backFrame;          // 選択中だけ表示する選択枠

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;   // 選択時の効果音
    [SerializeField] private AudioClip _seDecide;   // 決定時の効果音
    [SerializeField] private AudioClip _seCancel;   // 戻る・選べない時の効果音

    private int _currentIndex;
    private int _lastPaperIndex;     // 最後に選んでいた紙(「戻る」から下りる時・開き直した時に使う)
    private Coroutine _backScaleCoroutine;
    private bool _isActive;          // 入力を受け付けているか
    private bool _isMoveHeld;        // 方向を倒し続けている間は1回だけ動かす
    private int _openedFrame;
    private System.Action _onCancel;
    private Coroutine _fadeCoroutine;

    public bool IsActive => _isActive;

    /// <summary>
    /// 開く。紙を1枚ずつ貼り出し、前回選んでいた紙にカーソルを合わせる
    /// </summary>
    /// <param name="onCancel">キャンセルされた時の処理</param>
    public void Open(System.Action onCancel) {
        _onCancel = onCancel;
        gameObject.SetActive(true);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        _currentIndex = Mathf.Clamp(_lastPaperIndex, 0, Mathf.Max(0, _papers.Length - 1));
        for (int i = 0; i < _papers.Length; i++) {
            _papers[i].SetSelected(i == _currentIndex, instant: true);
            _papers[i].PlayAppear(i * _appearInterval);
        }
        _SetBackSelected(false, animate: false);

        _isActive = true;
        _isMoveHeld = true;  // 開いた時に倒していた方向では動かさない
        _openedFrame = Time.frameCount;
    }

    /// <summary>
    /// 閉じる(フェードアウトして非表示にする)
    /// </summary>
    public void Close() {
        _isActive = false;
        if (!gameObject.activeInHierarchy) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(_FadeOutAndHide());
    }

    private IEnumerator _FadeOutAndHide() {
        float start = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        float t = 0f;
        while (t < CLOSE_FADE_TIME) {
            t += Time.unscaledDeltaTime;
            if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Lerp(start, 0f, t / CLOSE_FADE_TIME);
            yield return null;
        }
        _fadeCoroutine = null;
        gameObject.SetActive(false);
    }

    private void Update() {
        if (!_isActive || _papers == null || _papers.Length == 0) return;
        if (_playerController == null) {
            _playerController = PlayerCharacterManager.Controller;
            if (_playerController == null) return;
        }
        // 開いたフレームの決定入力(メインメニューで押したもの)は使わない
        if (Time.frameCount == _openedFrame) return;

        var input = _playerController.Input;

        // カーソル移動(横:1枚ずつ、縦:段を移る)
        int dx = input.move.x > INPUT_THRESHOLD ? 1 : input.move.x < -INPUT_THRESHOLD ? -1 : 0;
        int dy = input.move.y > INPUT_THRESHOLD ? 1 : input.move.y < -INPUT_THRESHOLD ? -1 : 0;
        if (dx == 0 && dy == 0) {
            _isMoveHeld = false;
        } else if (!_isMoveHeld) {
            _isMoveHeld = true;
            int next = _GetNextIndex(dx, dy);
            if (next != _currentIndex) {
                _Select(next);
            }
        }

        // Bでいつでもキャンセル、それ以外のボタンで決定
        if (input.abilityB.pressed) {
            _Cancel();
        } else if (input.messageNextPressed) {
            _Decide();
        }
    }

    // 方向入力からカーソルの移動先を求める(動けない時は今の位置を返す)
    private int _GetNextIndex(int dx, int dy) {
        if (_currentIndex == BACK_INDEX) {
            // 「戻る」から下か右で、最後に選んでいた紙の列の上の段へ
            if (dy < 0 || dx > 0) return _lastPaperIndex % COLUMNS;
            return BACK_INDEX;
        }
        if (dx != 0) {
            int next = _currentIndex + dx;
            return next >= 0 && next < _papers.Length ? next : _currentIndex;
        }
        if (dy > 0) {
            // 上: 前の段へ。一番上の段からさらに上で「戻る」へ
            int next = _currentIndex - COLUMNS;
            return next >= 0 ? next : (_backButton != null ? BACK_INDEX : _currentIndex);
        }
        if (dy < 0) {
            int next = _currentIndex + COLUMNS;
            return next < _papers.Length ? next : _currentIndex;
        }
        return _currentIndex;
    }

    private void _Select(int index) {
        if (index == _currentIndex) return;
        if (_currentIndex == BACK_INDEX) {
            _SetBackSelected(false, animate: false);
        } else {
            _papers[_currentIndex].SetSelected(false);
        }
        _currentIndex = index;
        if (_currentIndex == BACK_INDEX) {
            _SetBackSelected(true, animate: true);
        } else {
            _lastPaperIndex = _currentIndex;
            _papers[_currentIndex].SetSelected(true);
        }
        _PlaySe(_seSelect);
    }

    // 「戻る」ボタンの選択表示(ハイライト・選択枠・ふくらむ動き)
    private void _SetBackSelected(bool selected, bool animate) {
        if (_backActiveButton != null) _backActiveButton.SetActive(selected);
        if (_backFrame != null) _backFrame.SetActive(selected);
        if (_backButton == null) return;
        if (_backScaleCoroutine != null) {
            StopCoroutine(_backScaleCoroutine);
            _backScaleCoroutine = null;
        }
        _backButton.localScale = Vector3.one;
        if (selected && animate && gameObject.activeInHierarchy) {
            _backScaleCoroutine = StartCoroutine(_BackButtonScaleAnim());
        }
    }

    private IEnumerator _BackButtonScaleAnim() {
        float t = 0f;
        while (t < BUTTON_SCALE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / BUTTON_SCALE_TIME);
            _backButton.localScale = Vector3.one * Mathf.Lerp(1f, BUTTON_SCALE_MAX, Mathf.Sin(rate * Mathf.PI));
            yield return null;
        }
        _backButton.localScale = Vector3.one;
        _backScaleCoroutine = null;
    }

    private void _Decide() {
        if (_currentIndex == BACK_INDEX) {
            _Cancel();
            return;
        }
        var paper = _papers[_currentIndex];
        if (!paper.IsAvailable) {
            // まだ遊べないステージ
            paper.Shake();
            _PlaySe(_seCancel);
            return;
        }
        _isActive = false;
        _PlaySe(_seDecide);
        ChangeScene.LoadScene(false, paper.SceneName, _playerController);
    }

    private void _Cancel() {
        _PlaySe(_seCancel);
        Close();
        _onCancel?.Invoke();
    }

    private void _PlaySe(AudioClip clip) {
        if (_audioSource != null && clip != null) {
            _audioSource.PlayOneShot(clip);
        }
    }
}
