using UnityEngine;

public class Pause_UI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; // ポーズUIのパネル
    private static bool isOpen = false;
    private float _originalTimeScale = 1f;

    // ポーズUIの開閉トグル
    public void UIViewSwitch()
    {
        isOpen = !isOpen;
        if (pausePanel != null)
        {
            pausePanel.SetActive(isOpen);
        }
        if (isOpen)
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _originalTimeScale;
        }
    }

    // 明示的に開く
    public void OpenPauseUI()
    {
        isOpen = true;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        _originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    // 明示的に閉じる
    public void ClosePauseUI()
    {
        isOpen = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        Time.timeScale = _originalTimeScale;
    }

    // 現在開いているかどうか（staticでどこからでも参照可能）
    public static bool IsOpen => isOpen;
}
