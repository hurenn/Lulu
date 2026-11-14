using UnityEngine;

/// <summary>
/// 光の能力で発光している間のルルの見た目(リムライト)。
/// 今の絵を写した子を手前に重ね、シルエットの縁(内側)だけを光らせる(Lulu/SpriteRimLight)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LuluLightAppearance : MonoBehaviour {
    private static readonly int RIM_WIDTH_ID = Shader.PropertyToID("_RimWidth");
    private const float MAX_RIM_WIDTH = SpriteOutlinePaddingLimit.PADDING; // 絵の周りの余白を超えて調べると、隣のコマを拾うため

    [SerializeField] private float _fadeInTime = 0.08f;       // 光り始めてから最大になるまでの時間
    [SerializeField] private float _fadeOutTime = 0.15f;      // 発光をやめてから消えるまでの時間

    [Header("リムライト")]
    [SerializeField] private Material _rimMaterial;           // Lulu/SpriteRimLight(未設定ならリムライトを出さない)
    [SerializeField] private Color _rimColor = new Color(1f, 0.95f, 0.75f, 0.9f); // 色(アルファは強さ)
    [SerializeField] private float _rimWidth = 0.04f;         // 縁の幅(ワールド単位。絵の粗さが違っても同じ幅に見えるよう換算する)

    private SpriteRenderer _renderer;
    private bool _isLighting = false;
    private float _rate = 0f;          // 0〜1の光り具合
    private SpriteRenderer _rim;
    private MaterialPropertyBlock _rimBlock;

    private void Awake() {
        _renderer = GetComponent<SpriteRenderer>();
        _rimBlock = new MaterialPropertyBlock();
        if (_rimMaterial != null) {
            _rim = new GameObject("RimLight").AddComponent<SpriteRenderer>();
            _rim.transform.SetParent(transform, false);
            _rim.sharedMaterial = _rimMaterial;
            _rim.enabled = false;
        }
    }

    /// <summary>
    /// 発光しているかを設定する(光の能力で発光している間true)
    /// </summary>
    public void SetLighting(bool is_lighting) {
        _isLighting = is_lighting;
    }

    private void LateUpdate() {
        float fade_time = _isLighting ? _fadeInTime : _fadeOutTime;
        float step = fade_time > 0f ? Time.deltaTime / fade_time : 1f;
        _rate = Mathf.MoveTowards(_rate, _isLighting ? 1f : 0f, step);
        _UpdateRim();
    }

    private void _UpdateRim() {
        if (_rim == null) return;
        // 姿を消している時・透明な時は重ねない
        _rim.enabled = _rate > 0f && _renderer.enabled && _renderer.sprite != null && _renderer.color.a > 0.01f;
        if (!_rim.enabled) return;

        var sprite = _renderer.sprite;
        _rim.sprite = sprite;
        _rim.flipX = _renderer.flipX;
        _rim.flipY = _renderer.flipY;
        _rim.sortingLayerID = _renderer.sortingLayerID;
        _rim.sortingOrder = _renderer.sortingOrder + 1; // 本体の手前に重ねる
        var color = _rimColor;
        color.a *= _rate * _renderer.color.a;
        _rim.color = color;

        // 縁の幅を、今の絵の1ピクセルの大きさに合わせてテクスチャのピクセル数へ換算する
        float texel_size = sprite.bounds.size.x / sprite.rect.width * Mathf.Abs(transform.lossyScale.x);
        float width = texel_size > 0f ? Mathf.Clamp(_rimWidth / texel_size, 0.5f, MAX_RIM_WIDTH) : 1f;
        _rim.GetPropertyBlock(_rimBlock);
        _rimBlock.SetFloat(RIM_WIDTH_ID, width);
        _rim.SetPropertyBlock(_rimBlock);
    }
}
