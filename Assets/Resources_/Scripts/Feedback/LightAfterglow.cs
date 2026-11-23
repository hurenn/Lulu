using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 攻撃の光を、通った跡や消えた場所に少しの間残す(HD-2D風に、光がその場に滲んで残る感じ)。
/// ・移動中は一定間隔で光の複製を落とし、それぞれがフェードして消える
/// ・光が消えた時(オブジェクトの非表示・破棄、またはライトの無効化)に、その場で同じ光をフェードさせる
/// ・炎用に、光の強さを揺らめかせることもできる
/// 光の複製は_lightのオブジェクトごと作るので、_lightのオブジェクトにはLight2D以外を付けないこと
/// </summary>
public class LightAfterglow : MonoBehaviour {
    [SerializeField] private Light2D _light;

    [Header("消えた時の残光")]
    [SerializeField] private float _lingerTime = 0.4f; // 消えてからフェードしきるまでの時間(秒)。0なら残さない

    [Header("通った跡の残光")]
    [SerializeField] private float _dropInterval = 0f;          // 光の複製を落とす間隔(秒)。0なら落とさない
    [SerializeField] private float _dropIntensityRate = 0.5f;   // 落とす光の強さ(元の光に対する割合)
    [SerializeField] private float _dropRadiusRate = 0.7f;      // 落とす光の大きさ(元の光に対する割合。ポイントライトのみ)
    [SerializeField] private float _dropFadeTime = 0.45f;       // 落とした光がフェードしきるまでの時間(秒)

    [Header("揺らめき")]
    [SerializeField, Range(0f, 1f)] private float _flickerAmount = 0f; // 光の強さの揺れ幅(元の強さに対する割合)
    [SerializeField] private float _flickerSpeed = 12f;

    // 時間が止まっていても(timeScaleに関係なく)動かすか(時間を止める必殺技の演出で出すエフェクト用)
    public bool useUnscaledTime { get; set; } = false;

    private static bool _isQuitting = false;

    private float _baseIntensity;
    private float _dropTimer;
    private float _flickerSeed;
    private bool _wasLightOn;

    private void Reset() {
        _light = GetComponentInChildren<Light2D>(true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void _ResetStatics() {
        _isQuitting = false;
        Application.quitting -= _OnQuitting;
        Application.quitting += _OnQuitting;
    }

    private static void _OnQuitting() {
        _isQuitting = true;
    }

    private void Awake() {
        if (_light != null) _baseIntensity = _light.intensity;
        _flickerSeed = Random.value * 100f;
    }

    private void OnEnable() {
        _dropTimer = 0f;
        // プールから再利用された時、揺らめきで変えた強さを元に戻す
        if (_light != null && _flickerAmount > 0f) _light.intensity = _baseIntensity;
        _wasLightOn = false; // 表示されてから1フレーム経つまでは、消えても残光を出さない(生成直後にプールへしまう時など)
    }

    private void OnDisable() {
        if (_wasLightOn) _SpawnLinger();
        _wasLightOn = false;
    }

    private void LateUpdate() {
        if (_light == null) return;
        bool is_light_on = _light.enabled && _light.gameObject.activeInHierarchy;

        // ライトだけ無効にされた時(破棄を待つ間に姿だけ消す演出など)
        if (_wasLightOn && !is_light_on) _SpawnLinger();
        _wasLightOn = is_light_on;
        if (!is_light_on) return;
        // 実時間で動かしている時も、ポーズ中は止める(複製は止まったまま消えないので、同じ場所に溜まらないように)
        if (useUnscaledTime && Pause_UI.IsOpen) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (_flickerAmount > 0f) {
            float t = (useUnscaledTime ? Time.unscaledTime : Time.time) * _flickerSpeed + _flickerSeed;
            float noise = Mathf.PerlinNoise(t, _flickerSeed) * 2f - 1f;
            _light.intensity = _baseIntensity * (1f + noise * _flickerAmount);
        }

        if (_dropInterval > 0f) {
            _dropTimer += dt;
            if (_dropTimer >= _dropInterval) {
                _dropTimer -= _dropInterval;
                _Spawn(_baseIntensity * _dropIntensityRate, _dropRadiusRate, _dropFadeTime);
            }
        }
    }

    private void _SpawnLinger() {
        if (_lingerTime <= 0f) return;
        _Spawn(_baseIntensity, 1f, _lingerTime);
    }

    private void _Spawn(float intensity, float radius_rate, float fade_time) {
        if (_isQuitting || _light == null || !gameObject.scene.isLoaded) return;

        var src = _light.transform;
        var copy = Instantiate(_light.gameObject, src.position, src.rotation);
        copy.name = _light.name + "(Afterglow)";
        copy.transform.localScale = src.lossyScale;
        copy.SetActive(true);

        var light = copy.GetComponent<Light2D>();
        light.enabled = true;
        if (light.lightType == Light2D.LightType.Point) {
            light.pointLightOuterRadius *= radius_rate;
            light.pointLightInnerRadius *= radius_rate;
        }
        copy.AddComponent<LightAfterglowFade>().Begin(light, intensity, fade_time, useUnscaledTime);
    }
}
