using System.Collections;
using UnityEngine;

/// <summary>
/// メインメニュー(部屋の画面)。「ステージ」「はなす」「よみもの」「オプション」を横に並べ、
/// 左右入力でカーソルを動かし、決定ボタンで選ぶ。
/// カーソルを合わせた項目に対応する部屋の家具(黒板・机・本棚・箱)を発光アウトラインで強調する。
/// 開く画面が設定された項目(ステージ→ステージセレクト、よみもの→読み物)を選ぶと、
/// その家具を中心に部屋を少しズームしながら家具のアップの背景へクロスフェードし、画面を開く。
/// はなす(ランダム会話)・オプションは未実装
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
        public UIGlowOutline furniture;      // 選択中に光らせる家具(決定した時のズームの中心にもなる)

        [Header("決定した時に開く画面(未設定なら未実装扱い)")]
        public MainMenuSubScreen subScreen;
        public CanvasGroup backGround;               // 画面の背景(家具のアップ)。部屋の上にクロスフェードで重ねる
        public float zoomScale = 1.15f;              // 部屋をズームする倍率
        public float backGroundStartScale = 0.92f;   // 背景が出始める時の大きさ
        public bool hideRoom = true;                 // 開いている間、部屋(家具・ボタン・選択枠)を非表示にするか。falseならボタン類が消えるだけで部屋は見えたまま
    }

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private MenuItem[] _items;
    [SerializeField] private RectTransform _frame;       // 選択枠
    [SerializeField] private eMainMenu _initialMenu = eMainMenu.Stage;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;        // 選択時の効果音
    [SerializeField] private AudioClip _seDecide;        // 決定時の効果音

    [Header("項目の画面への切り替え")]
    [SerializeField] private RectTransform[] _zoomTargets;    // ズームする物(部屋の背景・家具)
    [SerializeField] private GameObject[] _hideObjects;       // 項目の画面を開いている間は非表示にする物(家具・ボタン・選択枠)
    [SerializeField] private CanvasGroup[] _fadeGroups;       // 切り替え中にフェードさせるUI(ボタン・選択枠)
    [SerializeField] private float _transitionTime = 0.45f;   // 切り替えの時間(秒)

    private enum eState {
        Menu,         // メインメニュー操作中
        Transition,   // 画面切り替え中
        SubScreen,    // 項目の画面(ステージセレクトなど)を開いている
    }
    private eState _state = eState.Menu;
    private MenuItem _openedItem;          // 開いている項目
    private Vector3[] _zoomBasePositions;
    private Vector3[] _zoomBaseScales;
    private Vector3[] _zoomCenterLocals;

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

        // ズームする物の元の位置・大きさを覚えておく
        if (_zoomTargets == null) _zoomTargets = new RectTransform[0];
        int count = _zoomTargets.Length;
        _zoomBasePositions = new Vector3[count];
        _zoomBaseScales = new Vector3[count];
        _zoomCenterLocals = new Vector3[count];
        for (int i = 0; i < count; i++) {
            _zoomBasePositions[i] = _zoomTargets[i].localPosition;
            _zoomBaseScales[i] = _zoomTargets[i].localScale;
        }

        // 項目の画面と背景は最初は隠しておく
        foreach (var item in _items) {
            if (item.backGround != null) {
                item.backGround.alpha = 0f;
                item.backGround.gameObject.SetActive(false);
            }
            if (item.subScreen != null) item.subScreen.gameObject.SetActive(false);
        }
    }

    private void Update() {
        if (_state != eState.Menu) return;
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
    /// 決定。開く画面が設定されていない項目(はなす・オプション)は未実装なので、決定音を鳴らしてログを出すだけ
    /// </summary>
    private void _Decide() {
        _PlaySe(_seDecide);
        var item = _items[_currentIndex];
        if (item.subScreen != null) {
            StartCoroutine(_EnterSubScreen(item));
            return;
        }
        // TODO: はなす(ランダム会話)・オプション
        Debug.Log($"[MainMenu] {item.menu} を選択(未実装)");
    }

    /// <summary>
    /// メインメニュー → 項目の画面。
    /// 家具を中心に部屋を少しズームしながら、家具のアップの背景をクロスフェードで重ね、ボタン類を消す
    /// </summary>
    private IEnumerator _EnterSubScreen(MenuItem item) {
        _state = eState.Transition;
        _openedItem = item;

        // ズームの中心(家具の位置)を、ズームする物の親の座標で求める(ズームしていない今の位置で求める)
        Vector3 center = item.furniture != null ? item.furniture.transform.position : Vector3.zero;
        for (int i = 0; i < _zoomTargets.Length; i++) {
            _zoomCenterLocals[i] = _zoomTargets[i].parent.InverseTransformPoint(center);
        }
        if (item.backGround != null) item.backGround.gameObject.SetActive(true);

        yield return _TransitionAnim(item, 0f, 1f);

        if (item.hideRoom) {
            foreach (var obj in _hideObjects) {
                if (obj != null) obj.SetActive(false);
            }
        }
        _state = eState.SubScreen;
        item.subScreen.Open(_OnSubScreenCanceled);
    }

    private void _OnSubScreenCanceled() {
        if (_state != eState.SubScreen) return;
        StartCoroutine(_ReturnToMenu(_openedItem));
    }

    /// <summary>
    /// 項目の画面 → メインメニュー(入った時の逆再生)
    /// </summary>
    private IEnumerator _ReturnToMenu(MenuItem item) {
        _state = eState.Transition;
        if (item.hideRoom) {
            foreach (var obj in _hideObjects) {
                if (obj != null) obj.SetActive(true);
            }
            // 非表示の間に消えた家具の光・ハイライトを付け直す
            _UpdateView(instant: true);
        }

        yield return _TransitionAnim(item, 1f, 0f);

        if (item.backGround != null) item.backGround.gameObject.SetActive(false);
        _openedItem = null;
        _isHorizontalHeld = true;  // 戻った時に倒していた方向では動かさない
        _state = eState.Menu;
    }

    /// <summary>
    /// 切り替えの進み具合(0:メインメニュー 1:項目の画面)をfromからtoへ動かす
    /// </summary>
    private IEnumerator _TransitionAnim(MenuItem item, float from, float to) {
        float t = 0f;
        while (t < _transitionTime) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Lerp(from, to, Mathf.Clamp01(t / _transitionTime));
            _ApplyTransition(item, Mathf.SmoothStep(0f, 1f, rate), rate);
            yield return null;
        }
        _ApplyTransition(item, to, to);
    }

    // zoomRate: ズームの進み具合(なめらかにしたもの) fadeRate: フェードの進み具合
    private void _ApplyTransition(MenuItem item, float zoomRate, float fadeRate) {
        // 家具を中心に拡大する: 中心からの距離を倍率分だけ伸ばす
        float scale = Mathf.Lerp(1f, item.zoomScale, zoomRate);
        for (int i = 0; i < _zoomTargets.Length; i++) {
            var target = _zoomTargets[i];
            if (target == null) continue;
            Vector3 center = _zoomCenterLocals[i];
            target.localPosition = center + (_zoomBasePositions[i] - center) * scale;
            target.localScale = _zoomBaseScales[i] * scale;
        }

        // ボタン類は前半で消す/後半で出す
        float uiAlpha = 1f - Mathf.Clamp01(fadeRate * 2f);
        foreach (var group in _fadeGroups) {
            if (group != null) group.alpha = uiAlpha;
        }

        // 家具のアップの背景は少し小さい所から広がりながら現れる
        if (item.backGround != null) {
            item.backGround.alpha = fadeRate;
            item.backGround.transform.localScale = Vector3.one * Mathf.Lerp(item.backGroundStartScale, 1f, zoomRate);
        }
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
