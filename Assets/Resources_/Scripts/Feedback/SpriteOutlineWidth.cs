using UnityEngine;

/// <summary>
/// AllIn1SpriteShaderのアウトライン(ピクセル幅指定)の太さを、どの絵でも画面上で同じ太さに揃える。
/// アウトラインの太さはテクスチャのピクセル数で指定するため、1ピクセルの大きさが違うシート
/// (Pixels Per Unitの違い・取り込み時の縮小・Transformの拡大率)では線の太さが変わってしまう。
/// マテリアルの「Outline Base Pixel Width」を「1ピクセル=0.005ワールド単位(ルルの待機の絵)での太さ」として扱い、今の絵に合わせて換算する。
/// 太さ・色などはマテリアルで調整する。
/// ワープのエフェクトなど、アウトラインを付けたくないコマは_noOutlineSpritesに入れる(その間は線を透明にする)。
/// また、AllIn1のアウトラインは見た目の透明度(SpriteRendererのcolor.a)を反映しないため、透明度を線の濃さに掛ける
/// (隠れている間や点滅中に、線だけが残らないように)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutlineWidth : MonoBehaviour {
    private const float REFERENCE_TEXEL_SIZE = 0.005f; // マテリアルの太さの基準にする、1ピクセルのワールド単位での大きさ(ルルの待機の絵)
    private const float MAX_WIDTH = SpriteOutlinePaddingLimit.PADDING; // 絵の周りの余白を超えると線が欠けるため、これ以上太くしない

    private static readonly int WIDTH_ID = Shader.PropertyToID("_OutlinePixelWidth");
    private static readonly int ALPHA_ID = Shader.PropertyToID("_OutlineAlpha");

    [SerializeField] private Sprite[] _noOutlineSprites; // アウトラインを付けないコマ(ワープのエフェクトなど)
    private System.Collections.Generic.HashSet<Sprite> _noOutlineSet;

    private SpriteRenderer _renderer;
    private MaterialPropertyBlock _block;
    private float _lastWidth = -1f;
    private float _lastAlpha = -1f;

    private void Awake() {
        _renderer = GetComponent<SpriteRenderer>();
        _block = new MaterialPropertyBlock();
        _noOutlineSet = new System.Collections.Generic.HashSet<Sprite>(_noOutlineSprites ?? new Sprite[0]);
    }

    private void LateUpdate() {
        var sprite = _renderer.sprite;
        var material = _renderer.sharedMaterial;
        if (sprite == null || material == null || !material.HasProperty(WIDTH_ID)) return;

        // 今の絵の1ピクセルのワールド単位での大きさ(取り込み時の縮小・Transformの拡大率も含む)
        float texel_size = sprite.bounds.size.x / sprite.rect.width * Mathf.Abs(transform.lossyScale.x);
        // 粗い絵(1ピクセルが大きい絵)では1ピクセル未満になることもあるため、小数のまま渡す
        float width = Mathf.Min(material.GetFloat(WIDTH_ID) * REFERENCE_TEXEL_SIZE / texel_size, MAX_WIDTH);
        float alpha = _noOutlineSet.Contains(sprite) ? 0f : material.GetFloat(ALPHA_ID) * _renderer.color.a;
        if (width == _lastWidth && Mathf.Approximately(alpha, _lastAlpha)) return;
        _lastWidth = width;
        _lastAlpha = alpha;

        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(WIDTH_ID, width);
        _block.SetFloat(ALPHA_ID, alpha);
        _renderer.SetPropertyBlock(_block);
    }
}

/// <summary>
/// シートに付けたアウトライン用の余白(取り込み後のテクスチャのピクセル数)。エディタ用のSpriteOutlinePaddingと値を合わせる
/// </summary>
public static class SpriteOutlinePaddingLimit {
    public const int PADDING = 12;
}
