using UnityEngine;

/// <summary>
/// 能力が使えないエリア
/// ・エリア内ではワープも含め、全ての能力が発動失敗する(光の能力の効果も消える)
/// ・外からエリア内へのワープは、エリアとの境界線で止まる
/// ・能力の攻撃(AbilitySealTargetを付けた物)はエリアに触れると消える
/// 判定はAbilitySealAreaレイヤーのコライダー(トリガー)で行う。物理的には何とも衝突しない
/// </summary>
[ExecuteAlways] // エディタ上で動かした時も見た目の範囲を更新する
[RequireComponent(typeof(Collider2D))]
public class AbilitySealArea : MonoBehaviour {
    public const string LAYER_NAME = "AbilitySealArea";
    private const float _BOUNDARY_OFFSET = 0.05f; // 境界線で止める時、エリアの外側へずらす距離

    private static int _layerMask = 0;

    /// <summary>
    /// エリアのレイヤーマスク
    /// </summary>
    public static int LayerMask {
        get {
            if (_layerMask == 0) _layerMask = UnityEngine.LayerMask.GetMask(LAYER_NAME);
            return _layerMask;
        }
    }

    /// <summary>
    /// 指定した位置がエリア内か
    /// </summary>
    public static bool Contains(Vector2 point) {
        return Physics2D.OverlapPoint(point, LayerMask) != null;
    }

    /// <summary>
    /// fromからtoへ移動する経路がエリアに入る場合、toをエリアとの境界線(の少し外側)に変える
    /// </summary>
    /// <returns>経路がエリアに入ったか</returns>
    public static bool ClipPath(Vector2 from, ref Vector2 to) {
        var hit = Physics2D.Linecast(from, to, LayerMask);
        if (hit.collider == null) return false;
        to = hit.point - (to - from).normalized * _BOUNDARY_OFFSET;
        return true;
    }

    [SerializeField] private Renderer _visual; // エリアの見た目(シェーダーにエリアの範囲を渡す。未設定なら同じオブジェクトのRenderer)

    private static readonly int _AREA_RECT_ID = Shader.PropertyToID("_AreaRect");
    private MaterialPropertyBlock _block;

    private void Reset() {
        _Setup();
    }

    private void OnValidate() {
        _Setup();
        _ApplyAreaRect();
    }

    private void OnEnable() {
        _ApplyAreaRect();
    }

    private void LateUpdate() {
        // 動かしたり大きさを変えたりした時だけ、見た目の範囲を更新する
        if (transform.hasChanged) {
            transform.hasChanged = false;
            _ApplyAreaRect();
        }
    }

    // 見た目のシェーダーへエリアの範囲(ワールド座標)を渡す(大きさが変わっても縁の太さを揃えるため)
    private void _ApplyAreaRect() {
        var visual = _visual != null ? _visual : GetComponent<Renderer>();
        if (visual == null) return;
        var bounds = visual.bounds;
        _block ??= new MaterialPropertyBlock();
        visual.GetPropertyBlock(_block);
        _block.SetVector(_AREA_RECT_ID, new Vector4(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y));
        visual.SetPropertyBlock(_block);
    }

    // エリアのレイヤーに置き、コライダーをトリガーにする(キャラクターや地面判定に影響しないように)
    private void _Setup() {
        int layer = UnityEngine.LayerMask.NameToLayer(LAYER_NAME);
        if (layer >= 0 && gameObject.layer != layer) gameObject.layer = layer;
        foreach (var col in GetComponents<Collider2D>()) {
            if (!col.isTrigger) col.isTrigger = true;
        }
    }
}
