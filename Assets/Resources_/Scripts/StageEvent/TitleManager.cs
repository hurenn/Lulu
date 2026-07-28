using System.Collections;
using UnityEditor;
using UnityEngine;

public class TitleManager : MonoBehaviour {
#if UNITY_EDITOR
    [SerializeField] private SceneAsset _sceneAsset; // シーンアセット（エディター専用）
#endif
    [SerializeField] private string _sceneName;

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private GameObject[] _japaneseUIs;  // 日本語UI要素（選択ハイライトなど）
    [SerializeField] private GameObject[] _englishUIs;   // 英語UI要素（選択ハイライトなど）
    [SerializeField] private GameObject _logoJapanese;   // 日本語タイトルロゴ
    [SerializeField] private GameObject _logoEnglish;    // 英語タイトルロゴ
    [SerializeField] private RectTransform _japaneseUiRect;  // 「はじめから」選択時の位置
    [SerializeField] private RectTransform _englishUiRect;   // 「GameStart」選択時の位置
    [SerializeField] private RectTransform _quitUiRect;      // 「ゲーム終了」選択時の位置
    [SerializeField] private GameObject _quitActiveButton;   // 「ゲーム終了」の選択ハイライト
    [SerializeField] private RectTransform _frameUiRect;     // 選択フレームの位置

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;       // 選択時の効果音
    [SerializeField] private AudioClip _seDecide;       // 決定時の効果音

    private enum eTitleMenu {
        JapaneseStart = 0,  // はじめから（日本語）
        EnglishStart = 1,   // GameStart（英語）
        Quit = 2,           // ゲーム終了
    }
    private eTitleMenu _currentMenu = eTitleMenu.JapaneseStart; // 初期カーソルは「はじめから」

    private bool _isSelected = false;  // メニュー移動中フラグ（連続移動防止用）
    private bool _isDecided = false;   // 決定済みフラグ（重複実行防止用）

    /// <summary>
    /// Reset時の自動設定（エディター用）
    /// </summary>
    private void Reset() {
        // PlayerControllerを自動で見つけて設定
        _playerController = FindAnyObjectByType<PlayerController>();
    }

    /// <summary>
    /// エディターでの値検証（シーン名の自動設定）
    /// </summary>
#if UNITY_EDITOR
    private void OnValidate() {
        // シーンアセットが設定されている場合、そのシーン名を自動設定
        if (_sceneAsset != null) {
            _sceneName = _sceneAsset.name;
        }
    }
#endif

    // プレイヤーパラメーター参照
    private PlayerParameter _playerParameter;
    
    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Start() {
        // プレイヤーパラメーターのインスタンスを初期化して取得
        PlayerParameter.CreateNewInstance();
        _playerParameter = PlayerParameter.Instance;

        // 初期言語を日本語に設定
        _playerParameter.language = PlayerParameter.eLanguage.Japanese;

        // 初期カーソル位置のUIを反映
        _UpdateUiView(_currentMenu);
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// 入力監視とメニューカーソル移動、決定処理を行う
    /// </summary>
    public void Update() {
        // PlayerControllerが未設定の場合は再取得を試行
        if (_playerController == null) {
            _playerController = FindAnyObjectByType<PlayerController>();
        }

        // プレイヤーパラメーターが無い、または決定済みの場合は処理をスキップ
        if (_playerParameter == null || _isDecided) {
            return;
        }

        // 縦方向の入力チェック
        if (_playerController.Input.move.y > 0.5f) {
            // 上方向入力：カーソルを上へ（はじめから方向）
            _MoveMenu(-1);
        } else if (_playerController.Input.move.y < -0.5f) {
            // 下方向入力：カーソルを下へ（ゲーム終了方向）
            _MoveMenu(1);
        } else {
            // 入力が無い場合：選択状態をリセット（連続移動を防ぐため）
            _isSelected = false;
        }

        // 決定ボタン（メッセージ送りボタン）の入力チェック
        if (_playerController.Input.messageNextPressed) {
            _DecideMenu();
        }
    }

    /// <summary>
    /// メニューカーソルの移動処理
    /// </summary>
    /// <param name="dir">-1:上へ +1:下へ</param>
    private void _MoveMenu(int dir) {
        // 既に移動中の場合は処理をスキップ（連続移動防止）
        if (_isSelected) {
            return;
        }
        _isSelected = true;

        // QuitButtonが割り当てられていない場合はGameStartまでの2択で完結させる
        eTitleMenu maxMenu = _quitUiRect != null ? eTitleMenu.Quit : eTitleMenu.EnglishStart;

        // はじめから(0)～ゲーム終了(2)の範囲でカーソルを移動
        int nextIndex = Mathf.Clamp((int)_currentMenu + dir, (int)eTitleMenu.JapaneseStart, (int)maxMenu);
        if (nextIndex == (int)_currentMenu) {
            // 端に到達している場合は何もしない
            return;
        }
        _currentMenu = (eTitleMenu)nextIndex;

        // UIの表示を更新
        _UpdateUiView(_currentMenu);

        // 選択音を再生
        if (_audioSource != null && _seSelect != null) {
            _audioSource.PlayOneShot(_seSelect);
        }
    }

    /// <summary>
    /// 決定ボタン入力時の処理
    /// </summary>
    private void _DecideMenu() {
        // 決定音を再生
        if (_audioSource != null && _seDecide != null) {
            _audioSource.PlayOneShot(_seDecide);
        }

        if (_currentMenu == eTitleMenu.Quit) {
            // ゲーム終了
            _QuitGame();
            return;
        }

        // 選択中の言語を確定し、指定シーンに遷移
        _playerParameter.language = _currentMenu == eTitleMenu.EnglishStart ?
            PlayerParameter.eLanguage.English : PlayerParameter.eLanguage.Japanese;
        _isDecided = true;
        ChangeScene.LoadScene(false, _sceneName);
    }

    /// <summary>
    /// ゲームを終了する
    /// </summary>
    private void _QuitGame() {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 選択中メニューに応じたUIの表示切り替え
    /// </summary>
    /// <param name="menu">選択中のメニュー</param>
    private void _UpdateUiView(eTitleMenu menu) {
        // 日本語UI要素の表示/非表示切り替え
        foreach (var _japaneseUI in _japaneseUIs) {
            _japaneseUI.SetActive(menu == eTitleMenu.JapaneseStart);
        }

        // 英語UI要素の表示/非表示切り替え
        foreach (var _englishUI in _englishUIs) {
            _englishUI.SetActive(menu == eTitleMenu.EnglishStart);
        }

        // タイトルロゴ：ゲーム終了選択中は英語ロゴのまま表示する
        if (_logoJapanese != null) {
            _logoJapanese.SetActive(menu == eTitleMenu.JapaneseStart);
        }
        if (_logoEnglish != null) {
            _logoEnglish.SetActive(menu != eTitleMenu.JapaneseStart);
        }

        // ゲーム終了の選択ハイライト切り替え
        if (_quitActiveButton != null) {
            _quitActiveButton.SetActive(menu == eTitleMenu.Quit);
        }

        // 選択フレームの位置・大きさをアニメーション付きで項目に合わせる
        var targetRect = menu switch {
            eTitleMenu.Quit => _quitUiRect,
            eTitleMenu.EnglishStart => _englishUiRect,
            _ => _japaneseUiRect,
        };
        if (targetRect != null) {
            StartCoroutine(_UiMoveAnim(targetRect.anchoredPosition, targetRect.sizeDelta, 0.1f));
        }
    }

    /// <summary>
    /// UIの位置・大きさ移動アニメーション
    /// </summary>
    /// <param name="set_anchor_pos">移動先の位置</param>
    /// <param name="set_size">移動先の大きさ</param>
    /// <param name="over_rate">オーバーレート（未使用）</param>
    /// <returns>コルーチン</returns>
    private IEnumerator _UiMoveAnim(Vector2 set_anchor_pos, Vector2 set_size, float over_rate) {
        // アニメーション開始位置・大きさを記録
        var start_pos = _frameUiRect.anchoredPosition;
        var start_size = _frameUiRect.sizeDelta;

        // アニメーション時間の設定
        float anim_time = 0.05f;    // 総アニメーション時間
        float elapsed_time = 0f;    // 経過時間

        // アニメーションループ
        while (elapsed_time < anim_time) {
            elapsed_time += Time.deltaTime;

            // 補間値の計算（0〜1の範囲）
            float t = Mathf.Clamp01(elapsed_time / anim_time);

            // 開始位置・大きさから目標へ線形補間で移動
            _frameUiRect.anchoredPosition = Vector2.Lerp(start_pos, set_anchor_pos, t);
            _frameUiRect.sizeDelta = Vector2.Lerp(start_size, set_size, t);

            // 次フレームまで待機
            yield return null;
        }

        // アニメーション完了時に正確な位置・大きさに設定（誤差防止）
        _frameUiRect.anchoredPosition = set_anchor_pos;
        _frameUiRect.sizeDelta = set_size;
    }
}
