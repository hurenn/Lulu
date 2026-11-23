using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// パーティクルの粒に2Dライトを追いかけさせる(パーティクルのLightsモジュールは3Dのライト専用で、2Dライトは付けられないため)。
/// 雛形のライト(_template)を複製して数個までの組を作り、生きている粒に数個おきに割り当てる。
/// ライトの明るさは、粒の今の大きさと濃さに合わせる(粒が縮む・薄くなると暗くなる)
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleLights2D : MonoBehaviour {
    [SerializeField] private Light2D _template;          // 雛形のライト(非表示にしておく。色・大きさ・照らす対象はこれに合わせる)
    [SerializeField] private int _maxLights = 8;         // ライトの最大数(負荷を抑えるため)
    [SerializeField] private int _particleStep = 2;      // 何個おきの粒にライトを付けるか

    private ParticleSystem _particle;
    private ParticleSystem.Particle[] _buffer;
    private Light2D[] _lights;
    private float _baseIntensity;

    // 使い終わったライトの置き場(弾などが生成・破棄されるたびにライトを作り直さないよう、同じ見た目の雛形の間で使い回す)
    // シーンが切り替わるとライトは破棄されるので、取り出す時に破棄済みのものは捨てる
    private static readonly Dictionary<string, Stack<Light2D>> s_pool = new Dictionary<string, Stack<Light2D>>();
    private string _poolKey;

    private void Awake() {
        _particle = GetComponent<ParticleSystem>();
        _buffer = new ParticleSystem.Particle[_particle.main.maxParticles];
        // ライトは必要になった時に置き場から取り出す(足りなければ雛形から作る)
        _lights = new Light2D[Mathf.Max(0, _maxLights)];
        if (_template == null) return;
        _baseIntensity = _template.intensity;
        _template.gameObject.SetActive(false);
        _poolKey = $"{_template.name}/{_template.lightType}/{_template.color}/{_template.intensity}/{_template.pointLightOuterRadius}/{_template.pointLightInnerRadius}";
    }

    private Light2D _AcquireLight(int index) {
        if (s_pool.TryGetValue(_poolKey, out var stack)) {
            while (stack.Count > 0) {
                var pooled = stack.Pop();
                if (pooled != null) return pooled;
            }
        }
        // ワールド座標で粒を追うので、親子関係は持たない(自身ごと消える時はOnDestroyで置き場へ戻す)
        var light = Instantiate(_template, transform.position, Quaternion.identity);
        light.name = _template.name + "_" + index;
        light.gameObject.SetActive(false);
        return light;
    }

    private void LateUpdate() {
        if (_template == null) return;
        int count = _particle.GetParticles(_buffer);
        bool is_world = _particle.main.simulationSpace == ParticleSystemSimulationSpace.World;
        int step = Mathf.Max(1, _particleStep);
        int used = 0;
        for (int i = 0; i < count && used < _lights.Length; i += step) {
            var p = _buffer[i];
            if (_lights[used] == null) _lights[used] = _AcquireLight(used);
            var light = _lights[used++];
            Vector3 pos = is_world ? p.position : transform.TransformPoint(p.position);
            light.transform.position = new Vector3(pos.x, pos.y, light.transform.position.z);
            float start_size = Mathf.Max(0.0001f, p.startSize);
            float size_rate = Mathf.Clamp01(p.GetCurrentSize(_particle) / start_size);
            float alpha = p.GetCurrentColor(_particle).a / 255f;
            light.intensity = _baseIntensity * size_rate * alpha;
            if (!light.gameObject.activeSelf) light.gameObject.SetActive(true);
        }
        for (int i = used; i < _lights.Length; i++) {
            if (_lights[i] != null && _lights[i].gameObject.activeSelf) _lights[i].gameObject.SetActive(false);
        }
    }

    // 自身が非表示になった時(プールへ戻った時など)は、親子関係の無いライトが点いたまま残らないよう消す
    private void OnDisable() {
        if (_lights == null) return;
        foreach (var light in _lights) {
            if (light != null && light.gameObject.activeSelf) light.gameObject.SetActive(false);
        }
    }

    // 自身が破棄される時は、ライトを消して置き場へ戻す(シーンごと破棄される時は、ライトもシーンと一緒に破棄される)
    private void OnDestroy() {
        if (_lights == null || _poolKey == null) return;
        if (!s_pool.TryGetValue(_poolKey, out var stack)) {
            stack = new Stack<Light2D>();
            s_pool[_poolKey] = stack;
        }
        foreach (var light in _lights) {
            if (light == null) continue;
            light.gameObject.SetActive(false);
            stack.Push(light);
        }
    }
}
