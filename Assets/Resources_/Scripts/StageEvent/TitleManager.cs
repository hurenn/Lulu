using System.Collections;
using UnityEditor;
using UnityEngine;

public class TitleManager : MonoBehaviour {
#if UNITY_EDITOR
    [SerializeField] private SceneAsset _sceneAsset; // シーンアセット（エディター専用）
#endif
    [SerializeField] private string _sceneName;

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private GameObject[] _japaneseUIs;  // 日本語UI要素
    [SerializeField] private GameObject[] _englishUIs;   // 英語UI要素
    [SerializeField] private RectTransform _japaneseUiRect;  // 日本語選択時の位置
    [SerializeField] private RectTransform _englishUiRect;   // 英語選択時の位置
    [SerializeField] private RectTransform _frameUiRect;     // 選択フレームの位置

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;       // 選択時の効果音
    [SerializeField] private AudioClip _seDecide;       // 決定時の効果音

    private bool _isSelected = false;  // 言語選択中フラグ（連続選択防止用）
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
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// 入力監視と言語選択、決定処理を行う
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
            // 上方向入力：日本語を選択
            _SetLaunguage(PlayerParameter.eLanguage.Japanese);
        } else if (_playerController.Input.move.y < -0.5f) {
            // 下方向入力：英語を選択
            _SetLaunguage(PlayerParameter.eLanguage.English);
        } else {
            // 入力が無い場合：選択状態をリセット（連続選択を防ぐため）
            _isSelected = false;
        }

        // 決定ボタン（メッセージ送りボタン）の入力チェック
        if (_playerController.Input.messageNextPressed) {
            // 決定音を再生
            if (_audioSource != null && _seDecide != null) {
                _audioSource.PlayOneShot(_seDecide);
            }
            
            // 決定状態にして、指定シーンに遷移
            _isDecided = true;
            ChangeScene.LoadScene(_sceneName);
        }
    }
    
    /// <summary>
    /// 言語設定の変更処理
    /// </summary>
    /// <param name="language">設定する言語</param>
    private void _SetLaunguage(PlayerParameter.eLanguage language) {
        // 既に選択中、または同じ言語が設定済みの場合は処理をスキップ
        if (_isSelected || _playerParameter.language == language) {
            return;
        }
        
        // 選択中フラグを立てる（連続選択防止）
        _isSelected = true;
        
        // プレイヤーパラメーターに言語を設定
        _playerParameter.language = language;
        
        // UIの表示を更新
        _UpdateUiView(language);
        
        // 選択音を再生
        if (_audioSource != null && _seSelect != null) {
            _audioSource.PlayOneShot(_seSelect);
        }
    }

    /// <summary>
    /// 言語に応じたUIの表示切り替え
    /// </summary>
    /// <param name="language">表示する言語</param>
    private void _UpdateUiView(PlayerParameter.eLanguage language) {
        // 日本語UI要素の表示/非表示切り替え
        foreach (var _japaneseUI in _japaneseUIs) {
            _japaneseUI.SetActive(language == PlayerParameter.eLanguage.Japanese);
        }
        
        // 英語UI要素の表示/非表示切り替え
        foreach (var _englishUI in _englishUIs) {
            _englishUI.SetActive(language == PlayerParameter.eLanguage.English);
        }
        
        // 選択フレームの位置をアニメーション付きで移動
        if (language == PlayerParameter.eLanguage.Japanese) {
            // 日本語選択時：日本語UIの位置に移動
            StartCoroutine(_UiMoveAnim(_japaneseUiRect.anchoredPosition, 0.1f));
        } else {
            // 英語選択時：英語UIの位置に移動
            StartCoroutine(_UiMoveAnim(_englishUiRect.anchoredPosition, 0.1f));
        }
    }

    /// <summary>
    /// UIの位置移動アニメーション
    /// </summary>
    /// <param name="set_anchor_pos">移動先の位置</param>
    /// <param name="over_rate">オーバーレート（未使用）</param>
    /// <returns>コルーチン</returns>
    private IEnumerator _UiMoveAnim(Vector2 set_anchor_pos, float over_rate) {
        // アニメーション開始位置を記録
        var start_pos = _frameUiRect.anchoredPosition;
        
        // アニメーション時間の設定
        float anim_time = 0.05f;    // 総アニメーション時間
        float elapsed_time = 0f;    // 経過時間
        
        // アニメーションループ
        while (elapsed_time < anim_time) {
            elapsed_time += Time.deltaTime;
            
            // 補間値の計算（0〜1の範囲）
            float t = Mathf.Clamp01(elapsed_time / anim_time);
            
            // 開始位置から目標位置へ線形補間で移動
            _frameUiRect.anchoredPosition = Vector2.Lerp(start_pos, set_anchor_pos, t);
            
            // 次フレームまで待機
            yield return null;
        }
        
        // アニメーション完了時に正確な位置に設定（誤差防止）
        _frameUiRect.anchoredPosition = set_anchor_pos;
    }
}
