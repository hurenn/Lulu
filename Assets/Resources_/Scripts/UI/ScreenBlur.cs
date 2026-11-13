using UnityEngine;

/// <summary>
/// 画面全体の放射状ブラーを出す(描画は、2DレンダラーのFull Screen Pass Renderer Featureで、Lulu/ScreenBlurシェーダーが行う)
/// 強さと中心はシェーダーのグローバルな値で渡す。出した強さから、指定した時間をかけて0に戻す
/// シーンが切り替わって消えた時は、強さを0に戻す
/// </summary>
public class ScreenBlur : MonoBehaviour {
    private static readonly int _INTENSITY_ID = Shader.PropertyToID("_ScreenBlurIntensity");
    private static readonly int _CENTER_ID = Shader.PropertyToID("_ScreenBlurCenter");
    private static ScreenBlur _instance;

    private float _startIntensity = 0f;
    private float _duration = 0f;
    private float _elapsed = 0f;

    /// <summary>
    /// ワールド座標の位置を中心に、強さ(0〜1)のブラーを出し、指定した時間(秒)で消す
    /// </summary>
    public static void Play(Vector3 world_center, float intensity, float duration) {
        if (intensity <= 0f) return;
        if (_instance == null) {
            _instance = new GameObject("ScreenBlur").AddComponent<ScreenBlur>();
        }
        var cam = Camera.main;
        Vector2 center = cam != null ? (Vector2)cam.WorldToViewportPoint(world_center) : new Vector2(0.5f, 0.5f);
        Shader.SetGlobalVector(_CENTER_ID, center);
        _instance._startIntensity = Mathf.Clamp01(intensity);
        _instance._duration = Mathf.Max(duration, 0.01f);
        _instance._elapsed = 0f;
        _instance._Apply();
    }

    private void Update() {
        if (_elapsed < _duration) _elapsed += Time.unscaledDeltaTime;
        _Apply();
    }

    private void _Apply() {
        // 出した瞬間が一番強く、だんだん弱まる
        float rate = Mathf.Clamp01(_elapsed / _duration);
        Shader.SetGlobalFloat(_INTENSITY_ID, _startIntensity * (1f - rate) * (1f - rate));
    }

    private void OnDestroy() {
        if (_instance == this) _instance = null;
        Shader.SetGlobalFloat(_INTENSITY_ID, 0f);
    }
}
