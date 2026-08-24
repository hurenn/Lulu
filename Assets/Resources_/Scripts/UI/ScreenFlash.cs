using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour {
    private static ScreenFlash _instance { get; set; }
    public static ScreenFlash Instance {
        get {
            if (_instance == null) {
                _instance = FindAnyObjectByType<ScreenFlash>();
            }
            return _instance;
        }
    }

    [SerializeField] private Image _flashImage;

    private void Reset() {
        _flashImage = GetComponent<Image>();
    }

    /// <summary>
    /// フラッシュ実行
    /// </summary>
    /// <param name="duration">フラッシュ完了までの時間</param>
    /// <param name="color">フラッシュの色</param>
    public void Flash(float duration = 0.1f, Color color = default) {
        if (color == default) color = Color.white;
        if(_instance == null) {
            Debug.LogWarning("ScreenFlash Instance is null.");
            return;
        }

        StartCoroutine(_FlashCoroutine(duration, color));
    }

    public void FadeIn(float duration = 0.1f, Color color = default) {
        if (color == default) color = Color.white;
        if (_instance == null) {
            Debug.LogWarning("ScreenFlash Instance is null.");
            return;
        }
        StartCoroutine(Fade(duration, color));
    }

    private IEnumerator _FlashCoroutine(float duration, Color color) {
        var no_alpha_color = color;         // アルファ値0の色
        no_alpha_color.a = 0f;

        _flashImage.color = no_alpha_color; // 初期状態は透明

        yield return Fade(0.05f, color); // フェードイン
        yield return Fade(duration - 0.05f, no_alpha_color); // フェードアウト
    }

    /// <summary>
    /// フェード処理
    /// </summary>
    /// <param name="duration">処理時間</param>
    /// <param name="next_color">変化先カラー</param>
    private IEnumerator Fade(float duration, Color next_color) {
        float timer = 0f;
        Color last_color = _flashImage.color;

        while (timer < duration) {
            timer += Time.unscaledDeltaTime;
            Color current_color = Color.Lerp(last_color, next_color, timer / duration);
            _flashImage.color = current_color;
            yield return null;
        }
    }
}

