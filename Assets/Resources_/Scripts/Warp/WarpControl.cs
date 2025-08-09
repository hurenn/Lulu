using UnityEngine;

public class WarpControl : MonoBehaviour
{
    // ワープチェック用のコンポーネント
    [SerializeField] private WarpChecker[] warpCheckers;

    public enum eWarpDirection
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft
    }

    // 障害物のレイヤーマスク
    [SerializeField] private LayerMask obstacleLayer;
    // キャラクターのコライダー
    [SerializeField] private Collider2D col;

    public void Warp(eWarpDirection direction)
    {
        Vector2 origin = transform.position;
        WarpChecker warp_checker = warpCheckers[(int)direction];
        Vector2 cheracter_size = GetCharacterSize();

        var safe_point = warp_checker.GetWarpPoint(origin, warp_checker.transform.position, cheracter_size, obstacleLayer);

        // ワープ先に移動
        transform.position = safe_point;
    }

    private Vector2 GetCharacterSize()
    {
        if(col == null) return new Vector2(0.5f, 1f); // デフォルトのキャラクターサイズ

        // キャラクターのコライダーサイズを取得
        return col.bounds.size;
    }
}
