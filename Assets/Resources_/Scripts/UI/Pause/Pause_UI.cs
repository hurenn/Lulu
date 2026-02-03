using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Pause_UI : MonoBehaviour {
    private const float BGM_VOLUME_SCALE_WHILE_PAUSED = 0.5f; // ポーズ中のBGM音量スケール
    private const float ARROW_SCALE_TIME = 0.2f; // 矢印拡縮アニメ時間
    private const float ARROW_SCALE_MAX = 1.3f; // 矢印の最大拡大率
    private const float PANEL_FADE_TIME = 0.1f; // パネルフェード時間
    private const float SUBPANEL_MOVE_TIME = 0.05f; // サブパネル移動アニメ時間

    private int _currentPanelIndex = 0;       // 現在選択中のメニュー

    [SerializeField] private GameObject pausePanel; // ポーズUIのパネル
    [SerializeField] private Pause_MenuBase[] menuPanels; // 0:ゲームメニュー, 1:ボタンコンフィグ, 2:その他設定
    [SerializeField] private RectTransform[] subPanels; // サブパネル位置情報（アクティブ制御なし）
    [SerializeField] private RectTransform activeSubPanel; // 実際に表示されるサブパネル
    [SerializeField] protected Image _rightArrow;  // 右矢印画像
    [SerializeField] protected Image _leftArrow;   // 左矢印画像

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

    private Coroutine _arrowScaleCoroutine; // 矢印拡縮コルーチン
    private Coroutine _panelFadeCoroutine; // パネルフェードコルーチン
    private Coroutine _subPanelMoveCoroutine; // サブパネル移動コルーチン
    private bool _isSwitching = false; // パネル切り替え中フラグ

    public event Action<int> OnMoveMenu; // 上下入力イベント（+1:下, -1:上）

    private void Awake() {
        // 矢印の初期スケールを設定
        if (_rightArrow != null) _rightArrow.rectTransform.localScale = Vector3.one;
        if (_leftArrow != null) _leftArrow.rectTransform.localScale = Vector3.one;

        // 各メニューパネルにCanvasGroupを追加（なければ）
        if (menuPanels != null) {
            foreach (var panel in menuPanels) {
                if (panel != null && panel.canvasGroup == null) {
                    panel.canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        // activeSubPanelの初期位置を設定
        if (activeSubPanel != null && subPanels != null && subPanels.Length > 0 && subPanels[0] != null) {
            activeSubPanel.anchoredPosition = subPanels[0].anchoredPosition;
        }
    }

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
            // すべてのパネルをアクティブにし、最初のパネル以外は透明に
            if (menuPanels != null && menuPanels.Length > 0) {
                for (int i = 0; i < menuPanels.Length; i++) {
                    if (i == 0) {
                        menuPanels[i].gameObject.SetActive(true);
                        menuPanels[i].Open(SwitchPanel, UIViewSwitch, _audioSource, _seSelect, _seDecide);
                        if (menuPanels[i].canvasGroup != null) {
                            menuPanels[i].canvasGroup.alpha = 1f;
                            menuPanels[i].canvasGroup.interactable = true;
                            menuPanels[i].canvasGroup.blocksRaycasts = true;
                        }
                    } else {
                        menuPanels[i].gameObject.SetActive(true);
                        if (menuPanels[i].canvasGroup != null) {
                            menuPanels[i].canvasGroup.alpha = 0f;
                            menuPanels[i].canvasGroup.interactable = false;
                            menuPanels[i].canvasGroup.blocksRaycasts = false;
                        }
                    }
                }
                _currentPanelIndex = 0;

                // activeSubPanelを最初の位置に即座に移動
                if (activeSubPanel != null && subPanels != null && subPanels.Length > 0 && subPanels[0] != null) {
                    activeSubPanel.anchoredPosition = subPanels[0].anchoredPosition;
                }
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
        if (menuPanels == null || menuPanels.Length == 0 || _isSwitching) return;
        
        // 方向に応じて矢印をアニメーション
        if (dir > 0 && _rightArrow != null) {
            StartArrowScaleAnim(_rightArrow);
        } else if (dir < 0 && _leftArrow != null) {
            StartArrowScaleAnim(_leftArrow);
        }

        int nextPanelIndex = (_currentPanelIndex + dir + menuPanels.Length) % menuPanels.Length;
        
        // 選択音を再生
        if (_audioSource != null && _seSelect != null) {
            _audioSource.PlayOneShot(_seSelect);
        }

        // クロスフェードを開始
        if (_panelFadeCoroutine != null) {
            StopCoroutine(_panelFadeCoroutine);
        }
        _panelFadeCoroutine = StartCoroutine(CrossFadePanels(_currentPanelIndex, nextPanelIndex));
    }

    /// <summary>
    /// パネルのクロスフェード
    /// </summary>
    private IEnumerator CrossFadePanels(int fromIndex, int toIndex) {
        _isSwitching = true;

        var fromPanel = menuPanels[fromIndex];
        var toPanel = menuPanels[toIndex];

        // 次のパネルを準備
        toPanel.Open(SwitchPanel, UIViewSwitch, _audioSource, _seSelect, _seDecide);
        
        // サブパネルの移動アニメーションを開始
        if (activeSubPanel != null && subPanels != null && toIndex < subPanels.Length && subPanels[toIndex] != null) {
            if (_subPanelMoveCoroutine != null) {
                StopCoroutine(_subPanelMoveCoroutine);
            }
            _subPanelMoveCoroutine = StartCoroutine(MoveSubPanel(subPanels[toIndex].anchoredPosition));
        }

        float t = 0f;
        while (t < PANEL_FADE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / PANEL_FADE_TIME);
            
            // イージング（スムーズな加速・減速）
            float easedRate = rate < 0.5f 
                ? 2f * rate * rate 
                : 1f - Mathf.Pow(-2f * rate + 2f, 2f) / 2f;

            // フェードアウト
            if (fromPanel.canvasGroup != null) {
                fromPanel.canvasGroup.alpha = 1f - easedRate;
            }

            // フェードイン
            if (toPanel.canvasGroup != null) {
                toPanel.canvasGroup.alpha = easedRate;
            }

            yield return null;
        }

        // 最終状態を確定
        if (fromPanel.canvasGroup != null) {
            fromPanel.canvasGroup.alpha = 0f;
            fromPanel.canvasGroup.interactable = false;
            fromPanel.canvasGroup.blocksRaycasts = false;
        }

        if (toPanel.canvasGroup != null) {
            toPanel.canvasGroup.alpha = 1f;
            toPanel.canvasGroup.interactable = true;
            toPanel.canvasGroup.blocksRaycasts = true;
        }

        _currentPanelIndex = toIndex;
        _isSwitching = false;
        _panelFadeCoroutine = null;
    }

    /// <summary>
    /// サブパネルの移動アニメーション
    /// </summary>
    /// <param name="targetPosition">移動先の位置</param>
    private IEnumerator MoveSubPanel(Vector2 targetPosition) {
        if (activeSubPanel == null) yield break;

        Vector2 startPos = activeSubPanel.anchoredPosition;
        float t = 0f;

        while (t < SUBPANEL_MOVE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / SUBPANEL_MOVE_TIME);
            
            // イージング（EaseOutCubic：減速しながら到達）
            float easedRate = 1f - Mathf.Pow(1f - rate, 3f);
            
            activeSubPanel.anchoredPosition = Vector2.Lerp(startPos, targetPosition, easedRate);
            yield return null;
        }

        // 最終位置を確定
        activeSubPanel.anchoredPosition = targetPosition;
        _subPanelMoveCoroutine = null;
    }

    /// <summary>
    /// 矢印の拡縮アニメーションを開始
    /// </summary>
    /// <param name="arrow">アニメーション対象の矢印</param>
    private void StartArrowScaleAnim(Image arrow) {
        if (arrow == null) return;

        // 前回のアニメーションを停止
        if (_arrowScaleCoroutine != null) {
            StopCoroutine(_arrowScaleCoroutine);
            _arrowScaleCoroutine = null;
        }

        _arrowScaleCoroutine = StartCoroutine(ArrowScaleAnim(arrow));
    }

    /// <summary>
    /// 矢印の拡縮アニメーション
    /// </summary>
    /// <param name="arrow">アニメーション対象の矢印</param>
    private IEnumerator ArrowScaleAnim(Image arrow) {
        if (arrow == null) yield break;

        Vector3 originalScale = Vector3.one;
        float t = 0f;

        // 拡大フェーズ（0 → MAX）より速く
        while (t < ARROW_SCALE_TIME * 0.3f) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / (ARROW_SCALE_TIME * 0.3f));
            // 急激に拡大
            float easedRate = 1f - Mathf.Pow(1f - rate, 4f);
            float scale = Mathf.Lerp(1f, ARROW_SCALE_MAX, easedRate);
            arrow.rectTransform.localScale = originalScale * scale;
            yield return null;
        }

        // 縮小フェーズ（MAX → 1.0）
        t = 0f;
        while (t < ARROW_SCALE_TIME * 0.7f) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / (ARROW_SCALE_TIME * 0.7f));
            // 弾むような縮小
            float easedRate = Mathf.Sin(rate * Mathf.PI * 0.5f);
            float scale = Mathf.Lerp(ARROW_SCALE_MAX, 1f, easedRate);
            arrow.rectTransform.localScale = originalScale * scale;
            yield return null;
        }

        // 最終的に元のサイズに確実に戻す
        arrow.rectTransform.localScale = originalScale;
        _arrowScaleCoroutine = null;
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
