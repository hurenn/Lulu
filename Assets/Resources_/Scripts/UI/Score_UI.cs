using UnityEngine;

/// <summary>
/// スコア表示用UIコンポーネント
/// </summary>
public class Score_UI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _scoreText; // スコア表示用テキスト

    private void Start()
    {
        if (_scoreText == null) {
            _scoreText = GetComponent<TMPro.TextMeshProUGUI>();
            if (_scoreText == null) {
                Debug.LogError("Score_UI: TextMeshProUGUI component not found!");
                return;
            }
        }
        // 初期スコア表示
        //_UpdateScoreDisplay(PlayerParameter.Instance.GetScore());
        // スコアが変化したときにUIを更新するイベントを登録
        //PlayerParameter.Instance.OnScoreChanged += _UpdateScoreDisplay;
    }

    private void OnDestroy() {
        // イベント登録解除
        if (PlayerParameter.Instance != null) {
           //PlayerParameter.Instance.OnScoreChanged -= _UpdateScoreDisplay;
        }
    }

    private void _UpdateScoreDisplay(int newScore) {
        if (_scoreText != null) {
            _scoreText.text = $"Score: {newScore}";
        }
    }

}
