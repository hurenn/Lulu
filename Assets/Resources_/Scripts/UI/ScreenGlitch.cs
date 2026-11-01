using UnityEngine;

/// <summary>
/// 画面全体のグリッチノイズの強さを管理する(描画は、2DレンダラーのFull Screen Pass Renderer Featureで、Lulu/ScreenGlitchシェーダーが行う)
/// 強さと時間はシェーダーのグローバルな値で渡す。必殺技などで時間が止まっていても、ノイズは動き続ける
/// シーンが切り替わって消えた時は、強さを0に戻す
/// </summary>
public class ScreenGlitch : MonoBehaviour {
    private static readonly int _INTENSITY_ID = Shader.PropertyToID("_GlitchIntensity");
    private static readonly int _TIME_ID = Shader.PropertyToID("_GlitchTime");
    private static ScreenGlitch _instance;

    private float _intensity = 0f;
    private float _fromIntensity = 0f;
    private float _toIntensity = 0f;
    private float _duration = 0f;
    private float _elapsed = 0f;

    /// <summary>
    /// 強さ(0〜1)を、指定した時間(秒)をかけて変える
    /// </summary>
    public static void FadeTo(float intensity, float duration) {
        if (_instance == null) {
            if (intensity <= 0f) {
                Shader.SetGlobalFloat(_INTENSITY_ID, 0f);
                return;
            }
            _instance = new GameObject("ScreenGlitch").AddComponent<ScreenGlitch>();
        }
        _instance._fromIntensity = _instance._intensity;
        _instance._toIntensity = Mathf.Clamp01(intensity);
        _instance._duration = Mathf.Max(duration, 0f);
        _instance._elapsed = 0f;
        _instance._Apply();
    }

    private void Update() {
        if (_elapsed < _duration) _elapsed += Time.unscaledDeltaTime;
        _Apply();
    }

    private void _Apply() {
        float rate = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
        _intensity = Mathf.Lerp(_fromIntensity, _toIntensity, rate);
        Shader.SetGlobalFloat(_INTENSITY_ID, _intensity);
        Shader.SetGlobalFloat(_TIME_ID, Time.unscaledTime);
    }

    private void OnDestroy() {
        if (_instance == this) _instance = null;
        Shader.SetGlobalFloat(_INTENSITY_ID, 0f);
    }
}
