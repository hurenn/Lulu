using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Pause_UI : MonoBehaviour
{
    private const float BGM_VOLUME_SCALE_WHILE_PAUSED = 0.5f; // ポーズ中のBGM音量スケール
    private const float FRAME_MOVE_TIME = 0.02f; // 枠移動アニメ時間

    [SerializeField] private GameObject pausePanel; // ポーズUIのパネル
    [SerializeField] private Image selectFrame; // 選択枠画像（1つだけ）
    [SerializeField] private Image[] menuButtonImages; // メニューボタン（0:ゲームに戻る, 1:ステージセレクト）
    [SerializeField] private AudioSource _audioSource; // 効果音再生用AudioSource
    [SerializeField] private AudioClip _seSelect; // メニュー選択音

    private static bool isOpen = false;
    private float _originalTimeScale = 1f;
    private float _originalBgmVolume = 1f;
    private static AudioSource s_bgmSource = null; // キャッシュ用

    private int _selectedIndex = 0; // 0:ゲームに戻る, 1:ステージセレクト
    private Coroutine _frameMoveCoroutine;

    public event Action<int> OnMoveMenu; // 上下入力イベント（+1:下, -1:上）

    // BGM AudioSource取得（キャッシュ利用）
    private AudioSource GetBgmSource()
    {
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
            _selectedIndex = 0;
            MoveFrameToSelected(true);
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

    // PlayerControllerから呼ばれる上下入力処理
    public void MoveMenu(int dir)
    {
        int prevIndex = _selectedIndex;
        _selectedIndex = Mathf.Clamp(_selectedIndex + dir, 0, menuButtonImages.Length - 1);
        if (_selectedIndex != prevIndex)
        {
            MoveFrameToSelected();

            // 選択音を再生
            if (_audioSource != null && _seSelect != null) {
                _audioSource.PlayOneShot(_seSelect);
            }
        }
    }

    private void MoveFrameToSelected(bool instant = false)
    {
        if (selectFrame == null || menuButtonImages == null || _selectedIndex >= menuButtonImages.Length) return;
        var target = menuButtonImages[_selectedIndex].GetComponent<RectTransform>();
        if (target == null) return;
        if (_frameMoveCoroutine != null)
        {
            StopCoroutine(_frameMoveCoroutine);
        }
        if (instant)
        {
            selectFrame.rectTransform.anchoredPosition = target.anchoredPosition;
        }
        else
        {
            _frameMoveCoroutine = StartCoroutine(FrameMoveAnim(target.anchoredPosition));
        }
    }

    private IEnumerator FrameMoveAnim(Vector2 targetPos)
    {
        Vector2 start = selectFrame.rectTransform.anchoredPosition;
        float t = 0f;
        while (t < FRAME_MOVE_TIME)
        {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / FRAME_MOVE_TIME);
            selectFrame.rectTransform.anchoredPosition = Vector2.Lerp(start, targetPos, rate);
            yield return null;
        }
        selectFrame.rectTransform.anchoredPosition = targetPos;
    }

    /// <summary>
    /// 選択中のメニューを実行
    /// </summary>
    public void ExecuteSelectedMenu()
    {
        switch (_selectedIndex)
        {
            case 0: // ゲームに戻る
                UIViewSwitch();
                break;
            case 1: // ステージセレクト
                // 空の動作
                break;
        }
    }

    // 現在開いているかどうか（staticでどこからでも参照可能）
    public static bool IsOpen => isOpen;
}
