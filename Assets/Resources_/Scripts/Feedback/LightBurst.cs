using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 光の能力で発光し始めた瞬間に、ルルを中心に一瞬強く輝くエフェクト(閃光・放射状の光の筋・広がる輪・周りを照らすライト)。
/// 層ごとに、時間(0～1)に対する大きさと濃さのカーブで動かし、終わったら自身を破棄する。
/// 追いかける対象を渡すと、その位置に付いていく(発光中にルルが動いても中心がずれないように)
/// </summary>
public class LightBurst : MonoBehaviour {
    [System.Serializable]
    private class Layer {
        public SpriteRenderer renderer;
        public AnimationCurve scale = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 時間に対する大きさの倍率(作った時の大きさが1)
        public AnimationCurve alpha = AnimationCurve.Linear(0f, 1f, 1f, 0f); // 時間に対する濃さの倍率(作った時の濃さが1)
        public bool scaleOnlyX = false;     // 横だけ伸ばす(光の筋の長さ)。太さは thinning で細くする
        public AnimationCurve thinning = AnimationCurve.Constant(0f, 1f, 1f); // scaleOnlyXの時の太さの倍率
        [HideInInspector] public Vector3 baseScale;
        [HideInInspector] public float baseAlpha;
    }

    [SerializeField] private Layer[] _layers;
    [SerializeField] private Light2D _light;                // 周りを一瞬照らすライト(未設定なら照らさない)
    [SerializeField] private AnimationCurve _lightIntensity = new AnimationCurve(new Keyframe(0f, 3f), new Keyframe(1f, 0f));
    [SerializeField] private float _duration = 0.4f;        // 出てから消えるまでの時間(秒)
    [SerializeField] private float _spinSpeed = 60f;        // 光の筋をゆっくり回す速さ(度/秒)

    private float _time;
    private Transform _follow;

    /// <summary>
    /// エフェクトを出す
    /// </summary>
    /// <param name="follow">付いていく対象(nullなら出した位置に留まる)</param>
    public static LightBurst Spawn(LightBurst prefab, Vector3 center, Transform follow = null) {
        if (prefab == null) return null;
        var burst = Instantiate(prefab, new Vector3(center.x, center.y, prefab.transform.position.z), Quaternion.identity);
        burst._follow = follow;
        burst._Begin();
        return burst;
    }

    private void _Begin() {
        foreach (var layer in _layers) {
            if (layer.renderer == null) continue;
            layer.baseScale = layer.renderer.transform.localScale;
            layer.baseAlpha = layer.renderer.color.a;
        }
        _Apply(0f);
    }

    private void Update() {
        // ヒットストップなどで時間が遅くなっても一瞬で輝かせる(ポーズ中は止める)
        if (Pause_UI.IsOpen) return;
        _time += Time.unscaledDeltaTime;
        if (_follow != null) {
            transform.position = new Vector3(_follow.position.x, _follow.position.y, transform.position.z);
        }
        transform.Rotate(0f, 0f, _spinSpeed * Time.unscaledDeltaTime);
        float rate = Mathf.Clamp01(_time / _duration);
        _Apply(rate);
        if (rate >= 1f) Destroy(gameObject);
    }

    private void _Apply(float rate) {
        foreach (var layer in _layers) {
            var r = layer.renderer;
            if (r == null) continue;
            float s = layer.scale.Evaluate(rate);
            r.transform.localScale = layer.scaleOnlyX
                ? new Vector3(layer.baseScale.x * s, layer.baseScale.y * layer.thinning.Evaluate(rate), layer.baseScale.z)
                : layer.baseScale * s;
            var color = r.color;
            color.a = layer.baseAlpha * layer.alpha.Evaluate(rate);
            r.color = color;
        }
        if (_light != null) {
            _light.intensity = _lightIntensity.Evaluate(rate);
        }
    }
}
