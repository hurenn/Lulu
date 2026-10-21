using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// LightAbility使用中(isLightInvincible/isAutoLightInvincible)だけ、イージング付きフェードで
/// 非表示になり当たり判定も外れるギミック。
/// </summary>
public class LightHiddenObject : MonoBehaviour {
    [SerializeField] private Renderer _renderer; // SpriteRenderer/TilemapRendererどちらも可
    [SerializeField] private Graphic _graphic; // UIのImage等はこちら
    [SerializeField] private Collider2D _collider;
    [SerializeField] private float _fadeDuration = 0.3f; // 消える/現れるアニメーションの時間(秒)
    [SerializeField] private bool _isMisty = true;        // trueなら霧に覆われたような見た目にする(_rendererの材質を差し替える)
    [SerializeField] private Material _mistMaterial;      // 霧の見た目の材質(未設定ならResources/Materials/LightHiddenMist)

    private const string _DEFAULT_MIST_MATERIAL_PATH = "Materials/LightHiddenMist";

    private Tilemap _tilemap;
    private float _initialAlpha = 1f; // 元々設定されていた(消える前の)透明度
    private float _currentAlpha = 1f;
    private float _fadeStartAlpha = 1f;
    private float _fadeElapsed;
    private bool _targetVisible = true;

    private void Reset() {
        _renderer = GetComponent<Renderer>();
        _graphic = GetComponent<Graphic>();
        _collider = GetComponent<Collider2D>();
    }

    private void Awake() {
        if (_renderer != null) _tilemap = _renderer.GetComponent<Tilemap>();
        if (_isMisty && _renderer != null) {
            var mist = _mistMaterial != null ? _mistMaterial : Resources.Load<Material>(_DEFAULT_MIST_MATERIAL_PATH);
            if (mist != null) {
                _renderer.sharedMaterial = mist;
                if (_tilemap != null) _ApplyTileMask();
            }
        }
        _initialAlpha = _GetCurrentAlpha();
        _currentAlpha = _initialAlpha;
        _fadeStartAlpha = _initialAlpha;
    }

    private Texture2D _tileMask;

    private void OnDestroy() {
        if (_tileMask != null) Destroy(_tileMask);
    }

    // 霧の輪郭をぼかすため、タイルの有無を1マス=1テクセルのマスクにしてシェーダーへ渡す
    // (タイルは絵の端まで不透明で、シェーダーだけでは外周が分からないため)
    private void _ApplyTileMask() {
        _tilemap.CompressBounds();
        var bounds = _tilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0) return;

        // 外周の外側にも空のマスを1つずつ足す(外周で値がちょうど0.5になるように)
        int width = bounds.size.x + 2;
        int height = bounds.size.y + 2;
        _tileMask = new Texture2D(width, height, TextureFormat.Alpha8, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = name + "_TileMask",
        };
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                var cell = new Vector3Int(bounds.xMin + x - 1, bounds.yMin + y - 1, 0);
                pixels[y * width + x] = new Color32(0, 0, 0, (byte)(_tilemap.HasTile(cell) ? 255 : 0));
            }
        }
        _tileMask.SetPixels32(pixels);
        _tileMask.Apply(false, true);

        Vector2 min = _tilemap.CellToWorld(new Vector3Int(bounds.xMin - 1, bounds.yMin - 1, 0));
        Vector2 max = _tilemap.CellToWorld(new Vector3Int(bounds.xMax + 1, bounds.yMax + 1, 0));
        var block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
        block.SetTexture("_MaskTex", _tileMask);
        block.SetVector("_MaskRect", new Vector4(min.x, min.y, max.x - min.x, max.y - min.y));
        block.SetFloat("_UseMask", 1f);
        _renderer.SetPropertyBlock(block);
    }

    private float _GetCurrentAlpha() {
        if (_graphic != null) return _graphic.color.a;
        if (_tilemap != null) return _tilemap.color.a;
        if (_renderer is SpriteRenderer sr) return sr.color.a;
        return 1f;
    }

    // ワープのコルーチン(yield return null相当で再開)がUpdate後・LateUpdate前に発光フラグを立てて
    // テレポートするため、Update()で判定すると同一フレーム内で反映が間に合わないことがある。
    // LateUpdate()なら、その回の処理が確実に済んだ後で判定できる
    private void LateUpdate() {
        var player = PlayerCharacterManager.Current as Player_Character;
        bool isUsingLight = player != null &&
            (player.PlayerCharaParam.isLightInvincible || player.PlayerCharaParam.isAutoLightInvincible);
        bool visible = !isUsingLight;

        if (visible != _targetVisible) {
            _targetVisible = visible;
            _fadeStartAlpha = _currentAlpha;
            _fadeElapsed = 0f;
        }

        float targetAlpha = _targetVisible ? _initialAlpha : 0f;
        if (_fadeDuration <= 0f) {
            _currentAlpha = targetAlpha;
        } else {
            _fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            _currentAlpha = Mathf.Lerp(_fadeStartAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
        }
        _ApplyAlpha(_currentAlpha);

        if (_collider != null) _collider.enabled = visible;
    }

    private void _ApplyAlpha(float alpha) {
        if (_graphic != null) {
            var gc = _graphic.color;
            gc.a = alpha;
            _graphic.color = gc;
        }

        if (_renderer == null) return;
        if (_tilemap != null) {
            var c = _tilemap.color;
            c.a = alpha;
            _tilemap.color = c;
        } else if (_renderer is SpriteRenderer sr) {
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
        } else {
            _renderer.enabled = alpha > 0.5f;
        }
    }
}
