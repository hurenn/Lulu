using UnityEngine;

/// <summary>
/// 虹色の波紋(RainbowRippleシェーダーの_Progressを0から1へ進め、終わったら自身を破棄する)
/// </summary>
public class RainbowRipple : MonoBehaviour {
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private float _duration = 0.45f; // 広がって消えるまでの時間

    private static readonly int PROGRESS_ID = Shader.PropertyToID("_Progress");
    private MaterialPropertyBlock _block;
    private float _time;

    private void Awake() {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        _block = new MaterialPropertyBlock();
        _Apply(0f);
    }

    private void Update() {
        _time += Time.deltaTime;
        float rate = Mathf.Clamp01(_time / Mathf.Max(0.01f, _duration));
        _Apply(rate);
        if (rate >= 1f) Destroy(gameObject);
    }

    private void _Apply(float rate) {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(PROGRESS_ID, rate);
        _renderer.SetPropertyBlock(_block);
    }
}
