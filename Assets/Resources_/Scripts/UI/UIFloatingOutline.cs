using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIの画像に白いアウトラインと浮いたような影を付ける(UIの見た目を統一するための部品)。
/// 画像のメッシュを外側へ広げ、線の太さ・影のずれをUVに直してシェーダー(Lulu/UI/FloatingOutline)へ渡す。
/// 画像のマテリアルにはUIFloatingOutline.matを設定する。色はマテリアル、太さ・ずれはここで調整する。
/// 標準のOutline/Shadowは絵の色に色を掛けるだけなので白い線が作れず、半透明の窓では下に敷いた線が透けるため使わない。
/// AllIn1SpriteShaderのアウトラインは画像の枠の内側にしか描けず、枠いっぱいの窓やボタンでは線が欠けるため使わない
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIFloatingOutline : BaseMeshEffect {
    private const float WORLD_UNITS_PER_CANVAS_UNIT = 0.01f; // ワールド空間のCanvasで、太さ1をワールド単位に直す倍率(スプライトのPixels Per Unit=100に合わせる)

    [SerializeField] private float _outlineWidth = 4f;                        // 線の太さ(Canvas上の単位)
    [SerializeField] private Vector2 _shadowOffset = new Vector2(4f, -7f);  // 影のずれ(Canvas上の単位)

    private const float MAX_SCALE_COMPENSATION = 20f; // 拡大率0付近で線の太さの換算が無限大にならないようにする上限

    private static readonly List<UIVertex> _verts = new List<UIVertex>();

    private float _meshScale = -1f;  // メッシュを作った時の拡大率

    protected override void OnEnable() {
        base.OnEnable();
        EnableShaderChannels(graphic);
    }

    // 線の太さはメッシュを作る時の拡大率で換算するので、拡縮アニメ(選択肢の出現など)で拡大率が変わったら作り直す。
    // 作り直さないと、拡大率が小さい時に作った極端に太い線と影がそのまま残る
    private void LateUpdate() {
        if (graphic == null || _meshScale < 0f) return;
        float scale = transform.lossyScale.x;
        if (Mathf.Abs(scale - _meshScale) > Mathf.Max(0.001f, _meshScale * 0.005f)) {
            _meshScale = scale;
            graphic.SetVerticesDirty();
        }
    }

    // 線の太さ・影のずれ(TEXCOORD1)と絵の範囲(TEXCOORD2)をシェーダーへ渡せるようにする
    public static void EnableShaderChannels(Graphic graphic) {
        var canvas = graphic != null ? graphic.canvas : null;
        if (canvas == null) return;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
    }

    public override void ModifyMesh(VertexHelper vh) {
        if (!IsActive() || vh.currentVertCount == 0) return;
        _meshScale = transform.lossyScale.x;
        ExpandMesh(vh, graphic, _outlineWidth, _shadowOffset);
    }

    /// <summary>
    /// 画像のメッシュを線の太さ・影のずれの分だけ外側へ広げ、太さ・ずれ(UV)と絵の範囲を頂点に入れる。
    /// 発光アウトライン(UIGlowOutline)も同じ広げ方を使う
    /// </summary>
    public static void ExpandMesh(VertexHelper vh, Graphic graphic, float outlineWidth, Vector2 shadowOffset) {
        if (vh.currentVertCount == 0) return;
        EnableShaderChannels(graphic);
        var transform = graphic.transform;

        // 親の拡大率に関わらず、Canvas上で同じ太さになるようにローカルの大きさへ直す
        // ワールド空間のCanvas(頭上のゲージなど)は、1ワールド単位=100として画面上の太さを揃える
        float scale = 1f;
        var canvas = graphic.canvas;
        if (canvas != null) {
            float local = transform.lossyScale.x;
            float root = canvas.renderMode == RenderMode.WorldSpace ? WORLD_UNITS_PER_CANVAS_UNIT : canvas.transform.lossyScale.x;
            if (local > 0f && root > 0f) scale = Mathf.Min(root / local, MAX_SCALE_COMPENSATION);
        }
        float width = outlineWidth * scale;
        Vector2 shadow = shadowOffset * scale;
        float margin = width + Mathf.Max(Mathf.Abs(shadow.x), Mathf.Abs(shadow.y));

        Vector4 uvRect = new Vector4(0f, 0f, 1f, 1f);
        if (graphic is Image image && image.sprite != null) {
            uvRect = UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
        } else if (graphic is RawImage raw) {
            uvRect = new Vector4(raw.uvRect.xMin, raw.uvRect.yMin, raw.uvRect.xMax, raw.uvRect.yMax);
        }

        _verts.Clear();
        vh.GetUIVertexStream(_verts);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (var v in _verts) {
            min = Vector2.Min(min, v.position);
            max = Vector2.Max(max, v.position);
        }
        const float EPS = 0.01f;

        // 三角形ごとに「位置の変化→UVの変化」を求め、外周の頂点だけを外へ広げる(九分割の内側の継ぎ目は動かさない)
        for (int i = 0; i + 2 < _verts.Count; i += 3) {
            Vector2 p0 = _verts[i].position, p1 = _verts[i + 1].position, p2 = _verts[i + 2].position;
            Vector2 u0 = _verts[i].uv0, u1 = _verts[i + 1].uv0, u2 = _verts[i + 2].uv0;
            Vector2 dp1 = p1 - p0, dp2 = p2 - p0, du1 = u1 - u0, du2 = u2 - u0;
            float det = dp1.x * dp2.y - dp2.x * dp1.y;
            if (Mathf.Abs(det) < 1e-6f) continue;
            // UV = M * 位置 の M(2x2)
            float m00 = (du1.x * dp2.y - du2.x * dp1.y) / det;
            float m01 = (du2.x * dp1.x - du1.x * dp2.x) / det;
            float m10 = (du1.y * dp2.y - du2.y * dp1.y) / det;
            float m11 = (du2.y * dp1.x - du1.y * dp2.x) / det;
            Vector4 offsets = new Vector4(
                Mathf.Abs(m00 * width), Mathf.Abs(m11 * width),
                m00 * shadow.x + m01 * shadow.y, m10 * shadow.x + m11 * shadow.y);

            for (int k = i; k < i + 3; k++) {
                var v = _verts[k];
                Vector2 delta = Vector2.zero;
                if (v.position.x <= min.x + EPS) delta.x = -margin;
                else if (v.position.x >= max.x - EPS) delta.x = margin;
                if (v.position.y <= min.y + EPS) delta.y = -margin;
                else if (v.position.y >= max.y - EPS) delta.y = margin;
                v.position += (Vector3)delta;
                v.uv0 += new Vector4(m00 * delta.x + m01 * delta.y, m10 * delta.x + m11 * delta.y, 0f, 0f);
                v.uv1 = offsets;
                v.uv2 = uvRect;
                _verts[k] = v;
            }
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(_verts);
    }
}
