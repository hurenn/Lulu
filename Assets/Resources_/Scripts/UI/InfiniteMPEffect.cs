using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// MP無限モードの演出
/// ・発動の瞬間: プレイヤーが強く光り、虹色の混ざった白いオーラが画面全体に広がる
/// ・発動中: 画面の周りが虹色に光り、キラキラがうっすら舞う。MPゲージの上にもキラキラが舞う
/// 画像(柔らかい円・輪・画面の縁・星)は実行時に作る。ポーズや必殺技で時間が止まっていても動かす
/// </summary>
public class InfiniteMPEffect : MonoBehaviour {
    [Tooltip("発動の瞬間に光らせる対象(プレイヤーの見た目のSpriteRenderer)")]
    [SerializeField] private SpriteRenderer _target;
    [Tooltip("キラキラを舞わせるMPゲージ(MP無限モードのゲージ)")]
    [SerializeField] private RectTransform _gauge;
    [Tooltip("虹色に光らせるマテリアル(AllIn1Shader。MP無限モードのゲージと同じもの)")]
    [SerializeField] private Material _rainbowMaterial;
    [Tooltip("虹色の色相が一巡する時間(秒)")]
    [SerializeField] private float _hueCycleTime = 1.5f;

    [Header("発動の瞬間")]
    [Tooltip("オーラが画面全体に広がる時間(秒)")]
    [SerializeField] private float _burstTime = 0.8f;
    [Tooltip("広がるオーラの、白い部分の濃さ")]
    [SerializeField] private float _burstWhiteAlpha = 0.85f;
    [Tooltip("プレイヤーが光る時間(秒)")]
    [SerializeField] private float _glowTime = 0.7f;
    [Tooltip("プレイヤーを照らす光の強さ")]
    [SerializeField] private float _glowLightIntensity = 4.0f;
    [Tooltip("プレイヤーを照らす光の届く範囲")]
    [SerializeField] private float _glowLightRadius = 5.0f;
    [Tooltip("プレイヤーに重ねる光の大きさ(最大)")]
    [SerializeField] private float _glowSpriteSize = 4.0f;

    [Header("発動中:画面の周り")]
    [Tooltip("画面の周りの虹色の光の濃さ(最小)")]
    [SerializeField] private float _edgeAlphaMin = 0.25f;
    [Tooltip("画面の周りの虹色の光の濃さ(最大)")]
    [SerializeField] private float _edgeAlphaMax = 0.5f;
    [Tooltip("画面の周りの光が明滅する1回の時間(秒)")]
    [SerializeField] private float _edgePulseTime = 1.2f;
    [Tooltip("発動・終了の時に、画面の周りの光が現れる・消える時間(秒)")]
    [SerializeField] private float _edgeFadeTime = 0.4f;
    [Tooltip("画面に舞うキラキラの数")]
    [SerializeField] private int _screenSparkleCount = 24;
    [Tooltip("画面に舞うキラキラの大きさ(画面の高さを1080とした時のピクセル数)")]
    [SerializeField] private float _screenSparkleSize = 48f;
    [Tooltip("画面に舞うキラキラの濃さ")]
    [SerializeField] private float _screenSparkleAlpha = 0.45f;

    [Header("発動中:MPゲージ")]
    [Tooltip("MPゲージに舞うキラキラの数")]
    [SerializeField] private int _gaugeSparkleCount = 6;
    [Tooltip("MPゲージに舞うキラキラの大きさ(ゲージの座標での大きさ)")]
    [SerializeField] private float _gaugeSparkleSize = 1.2f;
    [Tooltip("MPゲージに舞うキラキラの濃さ")]
    [SerializeField] private float _gaugeSparkleAlpha = 0.9f;

    private const float _SCREEN_HEIGHT = 1080f;

    // キラキラ1つ分
    private class Sparkle {
        public RectTransform rect;
        public Image image;
        public float time;
        public float duration;
        public float size;
        public Vector2 velocity;
        public float rotateSpeed;
        public Color color;
    }

    private bool _isPlaying = false;
    private float _edgeRate = 0f; // 画面の周りの光の出ている割合(0〜1。現れる・消える途中)
    private float _burstElapsed = -1f;
    private float _glowElapsed = -1f;

    private GameObject _canvasObj;
    private RectTransform _canvasRect;
    private Material _rainbowInstance;
    private Image _edgeImage;
    private Image _burstWhite;
    private Image _burstRing;
    private readonly List<Sparkle> _screenSparkles = new List<Sparkle>();
    private readonly List<Sparkle> _gaugeSparkles = new List<Sparkle>();
    private Light2D _glowLight;
    private SpriteRenderer _glowSprite;
    private Material _glowMaterial;

    private static readonly int _HSV_SHIFT_ID = Shader.PropertyToID("_HsvShift");
    private static Sprite _softCircleSprite, _ringSprite, _edgeSprite, _starSprite;

    /// <summary>
    /// 演出を始める・終える(始めた瞬間に、発動の演出を出す)
    /// </summary>
    public void SetPlaying(bool is_playing) {
        if (_isPlaying == is_playing) return;
        _isPlaying = is_playing;
        _Build();
        if (is_playing) {
            _burstElapsed = 0f;
            _glowElapsed = 0f;
            foreach (var sparkle in _screenSparkles) _RespawnSparkle(sparkle, _canvasRect, true);
            foreach (var sparkle in _gaugeSparkles) _RespawnSparkle(sparkle, _gauge, true);
        }
    }

    private void Update() {
        if (_canvasObj == null) return;
        float delta = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;
        if (_rainbowInstance != null) _rainbowInstance.SetFloat(_HSV_SHIFT_ID, Mathf.Repeat(time / _hueCycleTime, 1f) * 360f);

        // 画面の周りの虹色の光(明滅しながら、発動・終了に合わせて現れる・消える)
        _edgeRate = Mathf.MoveTowards(_edgeRate, _isPlaying ? 1f : 0f, delta / Mathf.Max(_edgeFadeTime, 0.001f));
        float pulse = 0.5f - 0.5f * Mathf.Cos(time / Mathf.Max(_edgePulseTime, 0.001f) * Mathf.PI * 2f);
        _SetAlpha(_edgeImage, Mathf.Lerp(_edgeAlphaMin, _edgeAlphaMax, pulse) * _edgeRate);
        _edgeImage.enabled = _edgeRate > 0f;

        // キラキラ(終了したら、今出ている分が消えていく)
        foreach (var sparkle in _screenSparkles) _UpdateSparkle(sparkle, _canvasRect, delta, _screenSparkleAlpha * _edgeRate);
        foreach (var sparkle in _gaugeSparkles) _UpdateSparkle(sparkle, _gauge, delta, _isPlaying ? _gaugeSparkleAlpha : 0f);

        _UpdateBurst(delta);
        _UpdateGlow(delta);
    }

    // 虹色の混ざった白いオーラが、プレイヤーの位置から画面全体に広がる
    private void _UpdateBurst(float delta) {
        if (_burstElapsed < 0f) {
            _burstWhite.enabled = false;
            _burstRing.enabled = false;
            return;
        }
        _burstElapsed += delta;
        float rate = Mathf.Clamp01(_burstElapsed / Mathf.Max(_burstTime, 0.001f));
        float ease = 1f - (1f - rate) * (1f - rate);
        Vector2 center = Vector2.zero;
        var cam = Camera.main;
        if (cam != null && _target != null) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, cam.WorldToScreenPoint(_target.bounds.center), null, out center);
        }
        // 画面の端から端まで覆う大きさまで広げる
        float cover = _canvasRect.rect.size.magnitude * 2.2f;
        float size = Mathf.Lerp(0.05f, 1f, ease) * cover;
        foreach (var image in new[] { _burstWhite, _burstRing }) {
            image.enabled = rate < 1f;
            image.rectTransform.anchoredPosition = center;
            image.rectTransform.sizeDelta = new Vector2(size, size);
        }
        _SetAlpha(_burstWhite, _burstWhiteAlpha * (1f - rate));
        _SetAlpha(_burstRing, Mathf.Clamp01((1f - rate) * 1.5f));
        if (rate >= 1f) _burstElapsed = -1f;
    }

    // プレイヤーが強く光る(周りを照らし、白い光を重ねる)
    private void _UpdateGlow(float delta) {
        if (_glowElapsed < 0f || _target == null) {
            if (_glowLight != null) _glowLight.enabled = false;
            if (_glowSprite != null) _glowSprite.enabled = false;
            return;
        }
        _glowElapsed += delta;
        float rate = Mathf.Clamp01(_glowElapsed / Mathf.Max(_glowTime, 0.001f));
        float strength = rate < 0.15f ? rate / 0.15f : 1f - (rate - 0.15f) / 0.85f;
        _glowLight.enabled = true;
        _glowLight.intensity = _glowLightIntensity * strength;
        _glowSprite.enabled = true;
        _glowSprite.transform.position = _target.bounds.center;
        float size = Mathf.Lerp(0.6f, 1f, rate) * _glowSpriteSize;
        _glowSprite.transform.localScale = new Vector3(size, size, 1f);
        _glowSprite.color = new Color(1f, 1f, 1f, strength);
        if (rate >= 1f) _glowElapsed = -1f;
    }

    private void _UpdateSparkle(Sparkle sparkle, RectTransform area, float delta, float alpha) {
        if (area == null) return;
        sparkle.time += delta;
        // 出始めを待っている間は出さない
        if (sparkle.time < 0f) {
            sparkle.image.enabled = false;
            return;
        }
        float rate = sparkle.time / sparkle.duration;
        if (rate >= 1f) {
            // 終了した後は新しく出さない
            if (alpha <= 0f) {
                sparkle.image.enabled = false;
                return;
            }
            _RespawnSparkle(sparkle, area, false);
            rate = 0f;
        }
        // 大きくなって光ってから、小さくなって消える
        float twinkle = Mathf.Sin(rate * Mathf.PI);
        sparkle.image.enabled = true;
        sparkle.rect.anchoredPosition += sparkle.velocity * delta;
        sparkle.rect.localScale = Vector3.one * twinkle;
        sparkle.rect.localRotation = Quaternion.Euler(0f, 0f, sparkle.rect.localEulerAngles.z + sparkle.rotateSpeed * delta);
        var color = sparkle.color;
        color.a *= twinkle * alpha;
        sparkle.image.color = color;
    }

    // キラキラを、範囲の中のどこかに出し直す(is_startなら、出始めの時間をばらつかせる)
    private void _RespawnSparkle(Sparkle sparkle, RectTransform area, bool is_start) {
        if (area == null) return;
        var rect = area.rect;
        bool is_gauge = area == _gauge;
        // ゲージのキラキラは、ゲージから少しはみ出した所にも出す
        float margin = is_gauge ? 0.15f : 0f;
        float x = Mathf.Lerp(rect.xMin - rect.width * margin, rect.xMax + rect.width * margin, Random.value);
        float y = Mathf.Lerp(rect.yMin - rect.height * margin, rect.yMax + rect.height * margin, Random.value);
        if (!is_gauge) {
            // 画面のキラキラは、画面の周りに多く出す
            Vector2 p = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            if (Random.value < 0.7f) {
                if (Random.value < 0.5f) p.x = Mathf.Sign(p.x) * Random.Range(0.7f, 1f);
                else p.y = Mathf.Sign(p.y) * Random.Range(0.7f, 1f);
            }
            x = rect.center.x + p.x * rect.width * 0.5f;
            y = rect.center.y + p.y * rect.height * 0.5f;
        }
        sparkle.rect.anchoredPosition = new Vector2(x, y);
        sparkle.duration = Random.Range(0.6f, 1.4f);
        sparkle.time = is_start ? Random.Range(-sparkle.duration, 0f) : 0f;
        float base_size = is_gauge ? _gaugeSparkleSize : _screenSparkleSize;
        sparkle.size = base_size * Random.Range(0.5f, 1.2f);
        sparkle.rect.sizeDelta = new Vector2(sparkle.size, sparkle.size);
        float speed = is_gauge ? 0.3f : 25f;
        sparkle.velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.3f, 1f)) * speed;
        sparkle.rotateSpeed = Random.Range(-90f, 90f);
        sparkle.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        // 白から虹色寄りの色まで、ばらつかせる
        sparkle.color = Color.Lerp(Color.white, Color.HSVToRGB(Random.value, 1f, 1f), Random.Range(0f, 0.6f));
        sparkle.image.enabled = !is_start;
        if (is_start) sparkle.rect.localScale = Vector3.zero;
    }

    /// <summary>
    /// 演出に使う画面・画像を作る(最初に1回だけ)
    /// </summary>
    private void _Build() {
        if (_canvasObj != null) return;
        _CreateSprites();

        _canvasObj = new GameObject("InfiniteMPEffectCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = _canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100; // ゲーム画面の上、他のUIの下に出す
        var scaler = _canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(_SCREEN_HEIGHT * 16f / 9f, _SCREEN_HEIGHT);
        scaler.matchWidthOrHeight = 1f;
        _canvasRect = (RectTransform)_canvasObj.transform;

        if (_rainbowMaterial != null) _rainbowInstance = new Material(_rainbowMaterial);

        _edgeImage = _CreateImage("Edge", _canvasRect, _edgeSprite, _rainbowInstance);
        var edge_rect = _edgeImage.rectTransform;
        edge_rect.anchorMin = Vector2.zero;
        edge_rect.anchorMax = Vector2.one;
        edge_rect.sizeDelta = Vector2.zero;
        _SetAlpha(_edgeImage, 0f);

        _burstWhite = _CreateImage("BurstWhite", _canvasRect, _softCircleSprite, null);
        _burstRing = _CreateImage("BurstRing", _canvasRect, _ringSprite, _rainbowInstance);
        _burstWhite.enabled = false;
        _burstRing.enabled = false;

        for (int i = 0; i < _screenSparkleCount; i++) _screenSparkles.Add(_CreateSparkle(_canvasRect, null));
        if (_gauge != null) {
            for (int i = 0; i < _gaugeSparkleCount; i++) _gaugeSparkles.Add(_CreateSparkle(_gauge, _gauge.GetComponent<Graphic>()));
        }

        if (_target != null) {
            var light_obj = new GameObject("InfiniteMPGlowLight");
            light_obj.transform.SetParent(_target.transform, false);
            _glowLight = light_obj.AddComponent<Light2D>();
            _glowLight.lightType = Light2D.LightType.Point;
            _glowLight.pointLightOuterRadius = _glowLightRadius;
            _glowLight.pointLightInnerRadius = 0f;
            _glowLight.intensity = 0f;
            _glowLight.enabled = false;

            // 親の大きさ・向きの影響を受けないように、どこの子にもしない(位置は毎フレーム合わせる)
            var glow_obj = new GameObject("InfiniteMPGlowSprite");
            _glowSprite = glow_obj.AddComponent<SpriteRenderer>();
            _glowSprite.sprite = _softCircleSprite;
            var unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit != null) {
                _glowMaterial = new Material(unlit);
                _glowSprite.sharedMaterial = _glowMaterial;
            }
            _glowSprite.sortingLayerID = _target.sortingLayerID;
            _glowSprite.sortingOrder = _target.sortingOrder + 1;
            _glowSprite.enabled = false;
        }
    }

    private Sparkle _CreateSparkle(RectTransform parent, Graphic source) {
        var image = _CreateImage("Sparkle", parent, _starSprite, null);
        image.raycastTarget = false;
        image.enabled = false;
        // ゲージの上に出す時は、ゲージより手前に描く
        if (source != null) image.transform.SetAsLastSibling();
        return new Sparkle { rect = image.rectTransform, image = image, duration = 1f };
    }

    private static Image _CreateImage(string name, RectTransform parent, Sprite sprite, Material material) {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        if (material != null) image.material = material;
        return image;
    }

    private static void _SetAlpha(Graphic graphic, float alpha) {
        var color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void OnDestroy() {
        if (_canvasObj != null) Destroy(_canvasObj);
        if (_rainbowInstance != null) Destroy(_rainbowInstance);
        if (_glowLight != null) Destroy(_glowLight.gameObject);
        if (_glowSprite != null) Destroy(_glowSprite.gameObject);
        if (_glowMaterial != null) Destroy(_glowMaterial);
        foreach (var sparkle in _gaugeSparkles) {
            if (sparkle.rect != null) Destroy(sparkle.rect.gameObject);
        }
    }

    #region 画像を作る

    private static void _CreateSprites() {
        if (_softCircleSprite != null) return;
        const int SIZE = 128;
        // 柔らかい円(中心ほど濃い)
        _softCircleSprite = _CreateSprite(SIZE, (x, y) => {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - r);
            return a * a;
        });
        // 輪(外側の近くが濃い)
        _ringSprite = _CreateSprite(SIZE, (x, y) => {
            float r = Mathf.Sqrt(x * x + y * y);
            float d = (r - 0.82f) / 0.12f;
            return Mathf.Exp(-d * d) * (r < 1f ? 1f : 0f);
        });
        // 画面の縁(中央は透明で、縁ほど濃い)
        _edgeSprite = _CreateSprite(SIZE, (x, y) => {
            float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 4f) + Mathf.Pow(Mathf.Abs(y), 4f), 0.25f);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.825f, 1.05f, d));
        });
        // 星(光る芯と、十字に伸びる光)
        _starSprite = _CreateSprite(64, (x, y) => {
            float r2 = x * x + y * y;
            float core = Mathf.Exp(-r2 * 25f);
            float cross = Mathf.Exp(-Mathf.Abs(x) * 18f) * Mathf.Exp(-Mathf.Abs(y) * 2.5f)
                        + Mathf.Exp(-Mathf.Abs(y) * 18f) * Mathf.Exp(-Mathf.Abs(x) * 2.5f);
            return Mathf.Clamp01(core + cross * 0.8f);
        });
    }

    // -1〜1の座標ごとの濃さから、白い画像を作る
    private static Sprite _CreateSprite(int size, System.Func<float, float, float> alpha_at) {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (int py = 0; py < size; py++) {
            for (int px = 0; px < size; px++) {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                byte a = (byte)(Mathf.Clamp01(alpha_at(x, y)) * 255f);
                pixels[py * size + px] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    #endregion
}
