using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 光の能力で発光している間、ルルがまとう光(以前の発光ドームの代わり)。
/// 一定の明るさのまま、光の筋をゆっくり回し、全体を少しだけ拡縮させ続ける。
/// 発光の開始・終了で、短い時間をかけて現れる・消える
/// </summary>
public class LightAura : MonoBehaviour {
    [SerializeField] private SpriteRenderer[] _renderers;   // 濃さを変える絵(現れる・消える時)
    [SerializeField] private Transform _spinRoot;           // 回す部分(光の筋など)
    [SerializeField] private Light2D _light;                // 周りを照らすライト(未設定なら照らさない)
    [SerializeField] private float _spinSpeed = 25f;        // 回す速さ(度/秒)
    [SerializeField] private float _pulseAmount = 0.06f;    // 拡縮の幅(1に対する割合)
    [SerializeField] private float _pulsePeriod = 1.4f;     // 拡縮の周期(秒)
    [SerializeField] private float _fadeInTime = 0.1f;      // 現れるまでの時間(秒)
    [SerializeField] private float _fadeOutTime = 0.15f;    // 消えるまでの時間(秒)

    private float[] _baseAlpha;
    private float _baseLightIntensity;
    private Vector3 _baseScale;
    private bool _isVisible = false;
    private float _rate = 0f;
    private float _time = 0f;

    private void Awake() {
        _baseAlpha = new float[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++) {
            _baseAlpha[i] = _renderers[i] != null ? _renderers[i].color.a : 0f;
        }
        _baseLightIntensity = _light != null ? _light.intensity : 0f;
        _baseScale = transform.localScale;
        _Apply();
    }

    /// <summary>
    /// 見せるかを設定する(発光している間true)
    /// </summary>
    public void SetVisible(bool visible) {
        _isVisible = visible;
    }

    private void Update() {
        float fade_time = _isVisible ? _fadeInTime : _fadeOutTime;
        float step = fade_time > 0f ? Time.deltaTime / fade_time : 1f;
        _rate = Mathf.MoveTowards(_rate, _isVisible ? 1f : 0f, step);
        if (_rate <= 0f && !_isVisible) {
            _Apply();
            return;
        }

        _time += Time.deltaTime;
        if (_spinRoot != null) _spinRoot.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
        float pulse = _pulsePeriod > 0f ? Mathf.Sin(_time * Mathf.PI * 2f / _pulsePeriod) * _pulseAmount : 0f;
        transform.localScale = _baseScale * (1f + pulse);
        _Apply();
    }

    private void _Apply() {
        for (int i = 0; i < _renderers.Length; i++) {
            var r = _renderers[i];
            if (r == null) continue;
            r.enabled = _rate > 0f;
            var color = r.color;
            color.a = _baseAlpha[i] * _rate;
            r.color = color;
        }
        if (_light != null) {
            _light.enabled = _rate > 0f;
            _light.intensity = _baseLightIntensity * _rate;
        }
    }
}
