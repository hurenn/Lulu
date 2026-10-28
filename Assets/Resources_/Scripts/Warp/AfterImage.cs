using UnityEngine;

/// <summary>
/// 見た目をその場に複製した残像(薄くしながら消え、消えたら自身を破棄する)
/// </summary>
public class AfterImage : MonoBehaviour {
    private SpriteRenderer _renderer;
    private Color _color;
    private float _duration;
    private float _time;
    private bool _useUnscaledTime;

    /// <summary>
    /// 今の見た目をその場に複製し、durationかけて薄くしながら消す
    /// </summary>
    /// <param name="color">残像の色(本体の色に掛ける。アルファは出始めの濃さ)</param>
    /// <param name="material">残像の材質(nullなら本体と同じ材質)</param>
    /// <param name="use_unscaled_time">時間を止めている間も消えていくように実時間で動かすか(ポーズ中は止まる)</param>
    public static AfterImage Spawn(SpriteRenderer src, Color color, Material material, float duration, bool use_unscaled_time = false) {
        if (src == null || src.sprite == null || duration <= 0f) return null;

        var image = new GameObject("AfterImage").AddComponent<SpriteRenderer>();
        image.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
        image.transform.localScale = src.transform.lossyScale;
        image.sprite = src.sprite;
        image.flipX = src.flipX;
        image.flipY = src.flipY;
        image.sharedMaterial = material != null ? material : src.sharedMaterial;
        image.sortingLayerID = src.sortingLayerID;
        image.sortingOrder = src.sortingOrder - 1; // 本体より後ろに表示する
        image.color = src.color * color;

        var after_image = image.gameObject.AddComponent<AfterImage>();
        after_image._renderer = image;
        after_image._color = image.color;
        after_image._duration = duration;
        after_image._useUnscaledTime = use_unscaled_time;
        return after_image;
    }

    private void Update() {
        if (_useUnscaledTime) {
            if (Pause_UI.IsOpen) return;
            _time += Time.unscaledDeltaTime;
        } else {
            _time += Time.deltaTime;
        }
        if (_time >= _duration) {
            Destroy(gameObject);
            return;
        }
        var color = _color;
        color.a = _color.a * (1f - _time / _duration);
        _renderer.color = color;
    }
}
