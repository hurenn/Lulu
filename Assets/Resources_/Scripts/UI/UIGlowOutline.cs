using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの画像の周りを、ぼんやり光るアウトラインで強調する(メインメニューで選択中の家具など)。
/// SetHighlighted(true)で光がふわっと現れて明滅し、falseで消える。
/// 光っていない間は元のマテリアルに戻し、メッシュも広げない(普段は普通の画像と同じ描き方)。
/// メッシュの広げ方はUIFloatingOutlineと共通で、シェーダーはLulu/UI/GlowOutline(UIGlowOutline.mat)。
/// AllIn1SpriteShaderのグローは絵の切り抜き枠の内側にしか描けず、枠ぎりぎりの絵では光が欠けるため使わない
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIGlowOutline : BaseMeshEffect {
    private static readonly int STRENGTH_ID = Shader.PropertyToID("_Strength");
    private static readonly int WIDTH_SCALE_ID = Shader.PropertyToID("_WidthScale");

    [SerializeField] private Material _glowMaterial;            // 発光用マテリアル(UIGlowOutline.mat)。画像ごとに複製して使う
    [SerializeField] private float _glowWidth = 28f;            // 光の幅(Canvas上の単位)。伸び縮みの最大幅
    [SerializeField] private float _fadeInTime = 0.12f;         // 光が現れるまでの時間(秒)
    [SerializeField] private float _fadeOutTime = 0.12f;        // 光が消えるまでの時間(秒)
    [SerializeField] private float _pulseSpeed = 4f;            // 明滅の速さ(ラジアン/秒)
    [SerializeField, Range(0f, 1f)] private float _pulseDepth = 0.3f; // 明滅の深さ(0で明滅しない)
    [SerializeField, Range(0f, 1f)] private float _widthPulseDepth = 0.25f; // 幅の伸び縮みの深さ(0で伸び縮みしない)。明滅と同じ周期
    [SerializeField] private float _highlightBoost = 1.5f;      // 光り始めの一瞬だけ強くする倍率

    private Material _materialInstance;
    private Material _originalMaterial;
    private bool _isHighlighted;
    private float _fade;          // 0:消えている 1:光っている
    private float _pulseTime;     // 光り始めからの経過時間
    private float _meshScale;     // メッシュを作った時の拡大率

    public bool IsHighlighted => _isHighlighted;

    protected override void Awake() {
        base.Awake();
        if (graphic != null) {
            _originalMaterial = graphic.material == graphic.defaultMaterial ? null : graphic.material;
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (_materialInstance != null) {
            Destroy(_materialInstance);
            _materialInstance = null;
        }
    }

    protected override void OnDisable() {
        // 非表示になったら光を消した状態に戻す
        _isHighlighted = false;
        _fade = 0f;
        _RestoreMaterial();
        base.OnDisable();
    }

    /// <summary>
    /// 光らせる/消す
    /// </summary>
    /// <param name="highlighted">trueで光らせる</param>
    /// <param name="instant">trueでフェードせずに切り替える</param>
    public void SetHighlighted(bool highlighted, bool instant = false) {
        if (_isHighlighted == highlighted && !instant) return;
        _isHighlighted = highlighted;
        if (highlighted) {
            _pulseTime = 0f;
            _ApplyGlowMaterial();
        }
        if (instant) {
            _fade = highlighted ? 1f : 0f;
            if (!highlighted) _RestoreMaterial();
            _UpdateStrength();
        }
    }

    private void Update() {
        if (_materialInstance == null || graphic.material != _materialInstance) return;

        // 光の幅はメッシュを作る時の拡大率で決まるので、拡縮アニメ中は作り直して幅を一定に保つ
        float lossy = transform.lossyScale.x;
        if (Mathf.Abs(lossy - _meshScale) > 0.005f) {
            _meshScale = lossy;
            graphic.SetVerticesDirty();
        }

        // フェードイン/アウト(時間停止中のメニューでも動くようにunscaledで進める)
        float dt = Time.unscaledDeltaTime;
        if (_isHighlighted) {
            _fade = _fadeInTime > 0f ? Mathf.MoveTowards(_fade, 1f, dt / _fadeInTime) : 1f;
        } else {
            _fade = _fadeOutTime > 0f ? Mathf.MoveTowards(_fade, 0f, dt / _fadeOutTime) : 0f;
            if (_fade <= 0f) {
                _RestoreMaterial();
                return;
            }
        }
        _pulseTime += dt;
        _UpdateStrength();
    }

    // 光の強さ = フェード × 明滅。光り始めは強めに出してから落ち着かせる
    // 幅も同じ周期で伸び縮みさせる(明るい時に一番広い)
    private void _UpdateStrength() {
        if (_materialInstance == null) return;
        float wave = 0.5f * (1f - Mathf.Cos(_pulseTime * _pulseSpeed)); // 0→1→0
        float pulse = 1f - _pulseDepth * wave;
        float boost = Mathf.Lerp(_highlightBoost, 1f, Mathf.Clamp01(_pulseTime / 0.25f));
        _materialInstance.SetFloat(STRENGTH_ID, _fade * pulse * boost);
        _materialInstance.SetFloat(WIDTH_SCALE_ID, 1f - _widthPulseDepth * wave);
    }

    private void _ApplyGlowMaterial() {
        if (_glowMaterial == null || graphic == null) return;
        if (_materialInstance == null) {
            _materialInstance = new Material(_glowMaterial) { name = _glowMaterial.name + " (Instance)" };
        }
        if (graphic.material != _materialInstance) {
            graphic.material = _materialInstance;
        }
        graphic.SetVerticesDirty();
    }

    private void _RestoreMaterial() {
        if (graphic == null || graphic.material != _materialInstance || _materialInstance == null) return;
        graphic.material = _originalMaterial;
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh) {
        // 光っている間だけメッシュを広げる
        if (!IsActive() || _materialInstance == null || graphic.material != _materialInstance) return;
        _meshScale = transform.lossyScale.x;
        // 大きさ0付近では幅の換算が破綻するので広げない(出てくる演出の最初の一瞬)
        if (_meshScale < 0.01f) return;
        UIFloatingOutline.ExpandMesh(vh, graphic, _glowWidth, Vector2.zero);
    }
}
