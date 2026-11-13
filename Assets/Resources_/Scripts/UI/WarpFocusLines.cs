using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ワープした時の集中線を出す(描画はLulu/UI/WarpFocusLinesシェーダーが、画面全体に貼った板に計算で描く)
/// 初めて使う時に、HUD(UIのCanvas、描画順100)のすぐ後ろに描く専用のCanvasを作る
/// 線の位置・太さは画面の縦の長さに対する割合で決まるので、4Kなど解像度が変わっても見え方は変わらない
/// シーンが切り替わって消えた時は、次に使う時に作り直す
/// </summary>
public class WarpFocusLines : MonoBehaviour {
    private const string MATERIAL_PATH = "Materials/WarpFocusLines"; // Resources内のマテリアル
    private const int SORTING_ORDER = 99;          // HUD(100)の後ろ、ゲーム画面の手前
    private const float FADE_IN_TIME = 0.03f;      // 現れる時間
    private const float DURATION = 0.3f;           // 現れてから消えるまでの時間
    private const float FRAME_RATE = 12f;          // 線を描き替える速さ(アニメのコマ送りのように、1秒に何回描き替えるか)

    private static readonly int _SEED_ID = Shader.PropertyToID("_FocusSeed");
    private static readonly int _PROGRESS_ID = Shader.PropertyToID("_FocusProgress");
    private static WarpFocusLines _instance;

    private RawImage _image;
    private Material _material;
    private float _elapsed = -1f; // 負の時は出していない
    private float _seedOffset;

    /// <summary>
    /// 集中線を出す(出している途中でも、最初から出し直す)
    /// </summary>
    public static void Play() {
        if (_instance == null && !_Create()) return;
        _instance._elapsed = 0f;
        _instance._seedOffset = Random.Range(0f, 1000f);
        _instance._image.enabled = true;
        _instance._Apply();
    }

    private static bool _Create() {
        var material = Resources.Load<Material>(MATERIAL_PATH);
        if (material == null) return false;

        var obj = new GameObject("WarpFocusLines", typeof(Canvas), typeof(WarpFocusLines));
        var canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORTING_ORDER;

        var image_obj = new GameObject("Lines", typeof(RectTransform), typeof(RawImage));
        image_obj.transform.SetParent(obj.transform, false);
        var rect = (RectTransform)image_obj.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _instance = obj.GetComponent<WarpFocusLines>();
        _instance._material = new Material(material);
        _instance._image = image_obj.GetComponent<RawImage>();
        _instance._image.material = _instance._material;
        _instance._image.raycastTarget = false;
        _instance._image.enabled = false;
        return true;
    }

    private void Update() {
        if (_elapsed < 0f) return;
        // ヒットストップや必殺技で時間が止まっていても、消えずに残らないように実時間で進める
        _elapsed += Time.unscaledDeltaTime;
        _Apply();
    }

    private void _Apply() {
        if (_elapsed >= DURATION) {
            _elapsed = -1f;
            _image.enabled = false; // 出していない間は、画面全体の板を描かない
            return;
        }
        float rate = _elapsed / DURATION;
        float alpha = Mathf.Min(_elapsed / FADE_IN_TIME, 1f) * (1f - rate * rate);
        _image.color = new Color(1f, 1f, 1f, alpha);
        _material.SetFloat(_SEED_ID, _seedOffset + Mathf.Floor(_elapsed * FRAME_RATE));
        _material.SetFloat(_PROGRESS_ID, rate);
    }

    private void OnDestroy() {
        if (_instance == this) _instance = null;
        if (_material != null) Destroy(_material);
    }
}
