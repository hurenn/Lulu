using System.Collections;
using UnityEngine;

/// <summary>
/// メインメニュー(部屋の画面)。「ステージ」「はなす」「よみもの」「オプション」を横に並べ、
/// 左右入力でカーソルを動かし、決定ボタンで選ぶ。
/// カーソルを合わせた項目に対応する部屋の家具(黒板・机・本棚・箱)を発光アウトラインで強調する。
/// 各項目の先(ステージセレクト・ランダム会話・資料閲覧・オプション)は未実装
/// </summary>
public class MainMenuManager : MonoBehaviour {
    private const float FRAME_MOVE_TIME = 0.06f;    // 選択枠の移動時間(秒)
    private const float BUTTON_SCALE_TIME = 0.15f;  // ボタンの拡縮時間(秒)
    private const float BUTTON_SCALE_MAX = 1.1f;    // ボタンの最大拡大率
    private const float INPUT_THRESHOLD = 0.5f;     // スティックを倒したとみなす量

    public enum eMainMenu {
        Stage = 0,   // ステージ(ステージセレクト)
        Talk = 1,    // はなす(ランダム会話)
        Book = 2,    // よみもの(資料閲覧)
        Option = 3,  // オプション
    }

    [System.Serializable]
    private class MenuItem {
        public eMainMenu menu;
        public RectTransform button;         // 項目のボタン
        public GameObject activeButton;      // 選択中だけ表示するハイライト
        public UIGlowOutline furniture;      // 選択中に光らせる家具
    }

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private MenuItem[] _items;
    [SerializeField] private RectTransform _frame;       // 選択枠
    [SerializeField] private eMainMenu _initialMenu = eMainMenu.Stage;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;        // 選択時の効果音
    [SerializeField] private AudioClip _seDecide;        // 決定時の効果音

    private int _currentIndex;
    private bool _isHorizontalHeld;       // 左右を倒し続けている間は1回だけ動かす
    private Coroutine _frameMoveCoroutine;
    private Coroutine _buttonScaleCoroutine;
    private RectTransform _scalingButton;

    private void Reset() {
        _playerController = FindAnyObjectByType<PlayerController>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start() {
        _currentIndex = Mathf.Max(0, System.Array.FindIndex(_items, item => item.menu == _initialMenu));
        // ボタンはHorizontalLayoutGroupで並べているので、レイアウトを確定させてから選択枠を合わせる
        Canvas.ForceUpdateCanvases();
        _UpdateView(instant: true);
    }

    private void Update() {
        if (_playerController == null) {
            _playerController = PlayerCharacterManager.Controller;
            if (_playerController == null) return;
        }
        if (_items == null || _items.Length == 0) return;

        var input = _playerController.Input;

        // 左右入力でカーソル移動(倒しっぱなしでは連続で動かさない)
        if (input.move.x > INPUT_THRESHOLD) {
            _MoveCursor(1);
        } else if (input.move.x < -INPUT_THRESHOLD) {
            _MoveCursor(-1);
        } else {
            _isHorizontalHeld = false;
        }

        if (input.messageNextPressed) {
            _Decide();
        }
    }

    /// <summary>
    /// カーソル移動
    /// </summary>
    /// <param name="dir">-1:左 +1:右</param>
    private void _MoveCursor(int dir) {
        if (_isHorizontalHeld) return;
        _isHorizontalHeld = true;

        // 端では止める
        int next = Mathf.Clamp(_currentIndex + dir, 0, _items.Length - 1);
        if (next == _currentIndex) return;
        _currentIndex = next;

        _UpdateView(instant: false);
        _PlaySe(_seSelect);
    }

    /// <summary>
    /// 決定。各機能は未実装なので、決定音を鳴らしてログを出すだけ
    /// </summary>
    private void _Decide() {
        _PlaySe(_seDecide);
        var menu = _items[_currentIndex].menu;
        switch (menu) {
            case eMainMenu.Stage:
                // TODO: ステージセレクトへ
                break;
            case eMainMenu.Talk:
                // TODO: ランダム会話へ
                break;
            case eMainMenu.Book:
                // TODO: 資料閲覧へ
                break;
            case eMainMenu.Option:
                // TODO: オプションへ
                break;
        }
        Debug.Log($"[MainMenu] {menu} を選択(未実装)");
    }

    // 選択中の項目に合わせて、ハイライト・家具の光・選択枠を切り替える
    private void _UpdateView(bool instant) {
        for (int i = 0; i < _items.Length; i++) {
            var item = _items[i];
            bool selected = i == _currentIndex;
            if (item.activeButton != null) item.activeButton.SetActive(selected);
            if (item.furniture != null) item.furniture.SetHighlighted(selected, instant);
        }

        var target = _items[_currentIndex].button;
        if (target == null || _frame == null) return;

        if (_frameMoveCoroutine != null) StopCoroutine(_frameMoveCoroutine);
        if (instant) {
            _frame.position = target.position;
            _frame.sizeDelta = target.sizeDelta;
        } else {
            _frameMoveCoroutine = StartCoroutine(_FrameMoveAnim(target));
            if (_buttonScaleCoroutine != null) StopCoroutine(_buttonScaleCoroutine);
            if (_scalingButton != null) _scalingButton.localScale = Vector3.one;
            _scalingButton = target;
            _buttonScaleCoroutine = StartCoroutine(_ButtonScaleAnim(target));
        }
    }

    // 選択枠を項目の位置・大きさへ滑らせる(位置はワールド座標で合わせるので、枠とボタンの親が違ってもよい)
    private IEnumerator _FrameMoveAnim(RectTransform target) {
        Vector3 startPos = _frame.position;
        Vector2 startSize = _frame.sizeDelta;
        float t = 0f;
        while (t < FRAME_MOVE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / FRAME_MOVE_TIME), 3f);
            _frame.position = Vector3.Lerp(startPos, target.position, rate);
            _frame.sizeDelta = Vector2.Lerp(startSize, target.sizeDelta, rate);
            yield return null;
        }
        _frame.position = target.position;
        _frame.sizeDelta = target.sizeDelta;
        _frameMoveCoroutine = null;
    }

    // 選んだボタンを一瞬ふくらませる
    private IEnumerator _ButtonScaleAnim(RectTransform button) {
        float t = 0f;
        while (t < BUTTON_SCALE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / BUTTON_SCALE_TIME);
            float scale = Mathf.Lerp(1f, BUTTON_SCALE_MAX, Mathf.Sin(rate * Mathf.PI));
            button.localScale = Vector3.one * scale;
            yield return null;
        }
        button.localScale = Vector3.one;
        _scalingButton = null;
        _buttonScaleCoroutine = null;
    }

    private void _PlaySe(AudioClip clip) {
        if (_audioSource != null && clip != null) {
            _audioSource.PlayOneShot(clip);
        }
    }
}
