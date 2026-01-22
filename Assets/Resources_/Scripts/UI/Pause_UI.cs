using UnityEngine;

public class Pause_UI : MonoBehaviour
{
    private const float BGM_VOLUME_SCALE_WHILE_PAUSED = 0.5f; // ポーズ中のBGM音量スケール

    [SerializeField] private GameObject pausePanel; // ポーズUIのパネル
    [SerializeField] private AudioSource bgmSource; // BGM用AudioSource
    private static bool isOpen = false;
    private float _originalTimeScale = 1f;
    private float _originalBgmVolume = 1f;
    private static AudioSource s_bgmSource = null; // キャッシュ用

    // BGM AudioSource取得（キャッシュ利用）
    private AudioSource GetBgmSource()
    {
        if (bgmSource != null) return bgmSource;
        if (s_bgmSource != null) return s_bgmSource;
        var bgmObj = GameObject.Find("BGM");
        if (bgmObj != null)
        {
            s_bgmSource = bgmObj.GetComponent<AudioSource>();
            return s_bgmSource;
        }
        return null;
    }

    // ポーズUIの開閉トグル
    public void UIViewSwitch()
    {
        isOpen = !isOpen;
        if (pausePanel != null)
        {
            pausePanel.SetActive(isOpen);
        }
        var source = GetBgmSource();
        if (isOpen)
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (source != null)
            {
                _originalBgmVolume = source.volume;
                source.volume = _originalBgmVolume * BGM_VOLUME_SCALE_WHILE_PAUSED;
            }
        }
        else
        {
            Time.timeScale = _originalTimeScale;
            if (source != null)
            {
                source.volume = _originalBgmVolume;
            }
        }
    }

    // 現在開いているかどうか（staticでどこからでも参照可能）
    public static bool IsOpen => isOpen;
}
