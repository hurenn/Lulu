using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Pause_MenuBase : MonoBehaviour {
    protected const float FRAME_MOVE_TIME = 0.03f; // 枠移動アニメ時間

    protected System.Action<int> OnSwitchMenu;  // メニュー切り替えイベント（引数:切り替え方向）
    protected System.Action OnCloseMenu;        // メニューを閉じる
    protected Coroutine _frameMoveCoroutine;    // 枠移動コルーチン
    private bool _isInitialized = false;

    [SerializeField] protected Image _selectFrame; // 選択枠画像（1つだけ）

    protected AudioSource _audioSource; // 効果音再生用AudioSource
    protected AudioClip _seSelect; // メニュー選択音
    protected AudioClip _seDecide; // メニュー決定音

    /// <summary>
    /// 初期化 
    /// </summary>
    /// <param name="onSwitchMenu">メニュー切り替えコールバック</param>
    protected virtual void Initialize(System.Action<int> onSwitchMenu, System.Action onCloseMenu,
        AudioSource audio_source, AudioClip se_select, AudioClip se_decide) {
        OnSwitchMenu = onSwitchMenu;
        OnCloseMenu = onCloseMenu;
        _audioSource = audio_source;
        _seSelect = se_select;
        _seDecide = se_decide;
    }

    public virtual void Open(System.Action<int> onSwitchMenu, System.Action onCloseMenu,
        AudioSource audio_source, AudioClip se_select, AudioClip se_decide) {
        if (!_isInitialized) {
            Initialize(onSwitchMenu, onCloseMenu, audio_source, se_select, se_decide);
            _isInitialized = true;
        }
        gameObject.SetActive(true);
    }

    public void Close() {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 上下方向の入力
    /// </summary>
    /// <param name="dir">-1:下 +1:上</param>
    public virtual void OnInputVertical(int dir) {
    }

    /// <summary>
    /// 左右方向の入力
    /// </summary>
    /// <param name="dir">-1:左 +1:右</param>
    public virtual void OnInputHorizontal(int dir) {
    }

    /// <summary>
    /// 選択枠を選択中メニューに移動
    /// </summary>
    /// <param name="instant">即座に移動する</param>
    protected void MoveFrameToSelected(RectTransform target, bool instant = false) {
        if (_selectFrame == null) return;

        if (_frameMoveCoroutine != null) {
            StopCoroutine(_frameMoveCoroutine);
            _frameMoveCoroutine = null;
        }
        
        // GameObjectが非アクティブ、またはinstant=trueの場合は即座に移動
        if (instant || !gameObject.activeInHierarchy) {
            _selectFrame.rectTransform.anchoredPosition = target.anchoredPosition;
            _selectFrame.rectTransform.sizeDelta = target.sizeDelta;
        } else {
            _frameMoveCoroutine = StartCoroutine(FrameMoveAnim(target));
        }
    }

    /// <summary>
    /// 選択枠移動アニメーション
    /// </summary>
    protected IEnumerator FrameMoveAnim(RectTransform target) {
        Vector2 startPos = _selectFrame.rectTransform.anchoredPosition;
        Vector2 startSize = _selectFrame.rectTransform.sizeDelta;
        // 移動距離がある場合のみ選択音を再生
        if (Vector2.Distance(startPos, target.anchoredPosition) > 0.01f) {
            if (_audioSource != null && _seSelect != null) {
                _audioSource.PlayOneShot(_seSelect);
            }
        }

        float t = 0f;
        while (t < FRAME_MOVE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / FRAME_MOVE_TIME);
            _selectFrame.rectTransform.anchoredPosition = Vector2.Lerp(startPos, target.anchoredPosition, rate);
            _selectFrame.rectTransform.sizeDelta = Vector2.Lerp(startSize, target.sizeDelta, rate);
            yield return null;
        }
        _selectFrame.rectTransform.anchoredPosition = target.anchoredPosition;
        _selectFrame.rectTransform.sizeDelta = target.sizeDelta;
    }

    /// <summary>
    /// 選択中のメニューを実行
    /// </summary>
    public virtual void ExecuteSelectedMenu() {
    }
}