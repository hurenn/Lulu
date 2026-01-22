using UnityEngine;

public class Pause_UI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; // ポーズUIのパネル
    private bool isOpen = false;

    // ポーズUIの開閉トグル
    public void UIViewSwitch()
    {
        isOpen = !isOpen;
        if (pausePanel != null)
        {
            pausePanel.SetActive(isOpen);
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
    }

    // 明示的に閉じる
    public void ClosePauseUI()
    {
        isOpen = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    // 現在開いているかどうか
    public bool IsOpen => isOpen;
}
