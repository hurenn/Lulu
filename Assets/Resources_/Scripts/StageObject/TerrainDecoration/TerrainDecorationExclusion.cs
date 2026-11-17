using UnityEngine;

/// <summary>
/// 地形の自動装飾(TerrainDecorator)で装飾を置かない範囲。
/// 演出の邪魔になる場所や、手作業で装飾したい場所に置く。実行時の処理は無い。
/// </summary>
public class TerrainDecorationExclusion : MonoBehaviour {
    [SerializeField] private Vector2 _size = new Vector2(4f, 4f);

    /// <summary>ワールド座標での除外範囲</summary>
    public Rect WorldRect {
        get {
            Vector2 center = transform.position;
            Vector2 size = Vector2.Scale(_size, transform.lossyScale);
            size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            return new Rect(center - size * 0.5f, size);
        }
    }

    private void OnDrawGizmos() {
        Rect r = WorldRect;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.15f);
        Gizmos.DrawCube(r.center, r.size);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(r.center, r.size);
    }
}
