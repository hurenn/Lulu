using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Pause_UI : MonoBehaviour {
    private const float BGM_VOLUME_SCALE_WHILE_PAUSED = 0.5f; // ポーズ中のBGM音量スケール
    private int _currentPanelIndex = 0;       // 現在選択中のメニュー

    [SerializeField] private GameObject pausePanel; // ポーズUIのパネル
    [SerializeField] private Pause_MenuBase[] menuPanels; // 0:ゲームメニュー, 1:ボタンコンフィグ, 2:その他設定
    [SerializeField] private GameObject[] subPanels; // サブパネル

    [SerializeField] protected AudioSource _audioSource; // 効果音再生用AudioSource
    [SerializeField] protected AudioClip _seSelect; // メニュー選択音
    [SerializeField] protected AudioClip _seDecide; // メニュー決定音

    // 現在開いているかどうか（staticでどこからでも参照可能）
    public static bool IsOpen => isOpen;
    private static bool isOpen = false;
    public bool canOpen = true; // ポーズUIを開けるかどうか
    private bool _isInitialized = false;

    private float _originalTimeScale = 1f;
    private float _originalBgmVolume = 1f;
    private static AudioSource s_bgmSource = null; // キャッシュ用

    public event Action<int> OnMoveMenu; // 上下入力イベント（+1:下, -1:上）

    // BGM AudioSource取得（キャッシュ利用）
    private AudioSource GetBgmSource() {
        if (s_bgmSource != null) return s_bgmSource;
        var bgmObj = GameObject.Find("BGM");
        if (bgmObj != null) {
            s_bgmSource = bgmObj.GetComponent<AudioSource>();
            return s_bgmSource;
        }
        return null;
    }

    // ポーズUIの開閉トグル
    public void UIViewSwitch() {
        // カットイン演出中などで開閉禁止
        if (!canOpen) {
            return;
        }

        isOpen = !isOpen;
        if (pausePanel != null) {
            pausePanel.SetActive(isOpen);
        }

        var source = GetBgmSource();
        if (isOpen) {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (source != null) {
                _originalBgmVolume = source.volume;
                source.volume = _originalBgmVolume * BGM_VOLUME_SCALE_WHILE_PAUSED;
            }
            // 最初のパネルのみ表示
            if (menuPanels != null && menuPanels.Length > 0) {
                for (int i = 0; i < menuPanels.Length; i++) {
                    if (i == 0) {
                        menuPanels[i].Open(SwitchPanel, UIViewSwitch, _audioSource, _seSelect, _seDecide);
                    } else {
                        menuPanels[i].Close();
                    }
                    if (subPanels != null && i < subPanels.Length) {
                        subPanels[i].SetActive(i == 0);
                    }
                }
                _currentPanelIndex = 0;
            }
        } else {
            Time.timeScale = _originalTimeScale;
            if (source != null) {
                source.volume = _originalBgmVolume;
            }
        }
    }

    /// <summary>
    /// メニューパネル切り替え
    /// </summary>
    public void SwitchPanel(int dir) {
        if (menuPanels == null || menuPanels.Length == 0) return;
        _currentPanelIndex = (_currentPanelIndex + dir + menuPanels.Length) % menuPanels.Length;
        for (int i = 0; i < menuPanels.Length; i++) {
            if (i == _currentPanelIndex) {
                menuPanels[i].Open(SwitchPanel, UIViewSwitch, _audioSource, _seSelect, _seDecide);
            } else {
                menuPanels[i].Close();
            }
            if (subPanels != null && i < subPanels.Length)
                subPanels[i].SetActive(i == _currentPanelIndex);
        }
        // 選択音を再生
        if (_audioSource != null && _seSelect != null) {
            _audioSource.PlayOneShot(_seSelect);
        }
    }

    public void InputVerticalDir(int dir) {
        // 現在アクティブ状態のメニューに対して上下入力を与える
        _GetActiveMenu()?.OnInputVertical(dir);
    }

    public void InputHorizonDir(int dir) {
        // 現在アクティブ状態のメニューに対して左右入力を与える
        _GetActiveMenu()?.OnInputHorizontal(dir);
    }

    public void InputDecide() {
        // 現在アクティブ状態のメニューに対して決定入力を与える
        _GetActiveMenu()?.ExecuteSelectedMenu();
    }

    private Pause_MenuBase _GetActiveMenu() {
        if ((_currentPanelIndex < 0 || _currentPanelIndex >= menuPanels.Length)) {
            return null;
        }
        return menuPanels[_currentPanelIndex];
    }
}
