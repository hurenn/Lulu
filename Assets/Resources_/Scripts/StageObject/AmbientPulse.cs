using UnityEngine;

/// <summary>
/// スプライトの濃さをゆっくり明滅させる(光の筋など)。子のSpriteRendererもまとめて動かす。
/// </summary>
public class AmbientPulse : MonoBehaviour {
    [Tooltip("明滅の速さ")]
    [SerializeField] private float _speed = 0.8f;
    [Tooltip("明滅の幅(元の濃さに対する割合)")]
    [Range(0f, 1f)] [SerializeField] private float _amount = 0.35f;

    private SpriteRenderer[] _renderers;
    private float[] _baseAlpha;
    private float _phase;

    private void Awake() {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _baseAlpha = new float[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++) _baseAlpha[i] = _renderers[i].color.a;
        _phase = Random.value * Mathf.PI * 2f;
    }

    private void Update() {
        // ゆっくりした揺らぎ + 少し速い揺らぎを重ねて、規則的に見えないようにする
        float t = Time.time * _speed + _phase;
        float k = 1f - _amount * (0.5f + 0.35f * Mathf.Sin(t) + 0.15f * Mathf.Sin(t * 2.7f + 1.3f));
        for (int i = 0; i < _renderers.Length; i++) {
            var c = _renderers[i].color; c.a = _baseAlpha[i] * k; _renderers[i].color = c;
        }
    }
}
