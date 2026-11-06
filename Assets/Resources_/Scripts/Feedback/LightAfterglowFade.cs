using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// LightAfterglowが残した光を、時間をかけて消してから破棄する
/// </summary>
public class LightAfterglowFade : MonoBehaviour {
    private Light2D _light;
    private float _startIntensity;
    private float _duration;
    private float _time;
    private bool _useUnscaledTime;

    public void Begin(Light2D light, float intensity, float duration, bool use_unscaled_time) {
        _light = light;
        _startIntensity = intensity;
        _duration = Mathf.Max(0.01f, duration);
        _useUnscaledTime = use_unscaled_time;
        _time = 0f;
        _light.intensity = intensity;
    }

    private void Update() {
        if (_light == null) {
            Destroy(gameObject);
            return;
        }
        if (_useUnscaledTime && Pause_UI.IsOpen) return;

        _time += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float rate = Mathf.Clamp01(_time / _duration);
        // 始めはゆっくり、終わりにかけて素早く消える(光がその場に少し留まって見えるように)
        _light.intensity = _startIntensity * (1f - rate * rate);
        if (rate >= 1f) Destroy(gameObject);
    }
}
