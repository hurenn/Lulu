using UnityEngine;

/// <summary>
/// 指定した位置を中心に、指定した向きへ画面を横断する閃光(斬撃の光の線)。
/// 一瞬で端まで伸び、細くなりながら消えてから自身を破棄する。
/// 子のSpriteRenderer(芯・外側の光など)をまとめて伸縮・フェードさせる
/// </summary>
public class SlashStreak : MonoBehaviour {
    [SerializeField] private SpriteRenderer[] _layers;          // 伸縮・フェードさせる絵(芯・外側の光など)
    [SerializeField] private float _duration = 0.28f;          // 出てから消えるまでの時間(秒)
    [SerializeField] private float _extendTime = 0.05f;        // 端まで伸びきるまでの時間(秒)
    [SerializeField] private float _lengthMargin = 1.2f;       // 画面の対角線に対する長さの倍率(端が画面内に見えないように)
    [SerializeField] private AnimationCurve _thickness = new AnimationCurve(new Keyframe(0f, 1.3f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f)); // 時間(0～1)に対する太さの倍率

    private float _time;
    private float _length = 1f;
    private float[] _baseThickness;
    private float[] _baseAlpha;

    /// <summary>
    /// 閃光を出す
    /// </summary>
    public static SlashStreak Spawn(SlashStreak prefab, Vector3 center, Vector2 direction) {
        if (prefab == null) return null;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var streak = Instantiate(prefab, new Vector3(center.x, center.y, prefab.transform.position.z), Quaternion.Euler(0f, 0f, angle));
        streak._Begin();
        return streak;
    }

    private void _Begin() {
        // 画面の対角線より長くして、どの向きでも画面を横断させる
        var cam = Camera.main;
        float view_height = cam != null && cam.orthographic ? cam.orthographicSize * 2f : 20f;
        float view_width = view_height * (cam != null ? cam.aspect : 16f / 9f);
        _length = Mathf.Sqrt(view_width * view_width + view_height * view_height) * _lengthMargin;

        _baseThickness = new float[_layers.Length];
        _baseAlpha = new float[_layers.Length];
        for (int i = 0; i < _layers.Length; i++) {
            _baseThickness[i] = _layers[i].transform.localScale.y;
            _baseAlpha[i] = _layers[i].color.a;
        }
        _Apply(0f);
    }

    private void Update() {
        // ワープ中のヒットストップなどで時間が遅くなっても、閃光は一瞬で走らせる(ポーズ中は止める)
        if (Pause_UI.IsOpen) return;
        _time += Time.unscaledDeltaTime;
        float rate = Mathf.Clamp01(_time / _duration);
        _Apply(rate);
        if (rate >= 1f) Destroy(gameObject);
    }

    private void _Apply(float rate) {
        // 始めに一瞬で端まで伸ばす
        float extend = _extendTime > 0f ? Mathf.Clamp01(_time / _extendTime) : 1f;
        extend = 1f - (1f - extend) * (1f - extend);
        float thickness = _thickness.Evaluate(rate);
        // 消える間際に薄くする
        float fade = rate < 0.6f ? 1f : 1f - (rate - 0.6f) / 0.4f;
        for (int i = 0; i < _layers.Length; i++) {
            var layer = _layers[i];
            if (layer == null || layer.sprite == null) continue;
            float sprite_width = layer.sprite.bounds.size.x;
            layer.transform.localScale = new Vector3(_length * extend / sprite_width, _baseThickness[i] * thickness, 1f);
            var color = layer.color;
            color.a = _baseAlpha[i] * fade;
            layer.color = color;
        }
    }
}
