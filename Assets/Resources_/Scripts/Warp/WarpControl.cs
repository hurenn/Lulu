using UnityEngine;

public class WarpControl : MonoBehaviour
{
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

    // ワープチェック用のコンポーネント
    [SerializeField] private WarpChecker[] warpCheckers;

    /// <summary>
    /// ワープチェッカーのセットアップ
    /// </summary>
    /// <param name="character_size">キャラクターサイズ</param>
    /// <param name="obstacle_layer">対象レイヤー</param>
    public void Setup(Vector2 character_size, LayerMask obstacle_layer)
    {
        foreach(var checker in warpCheckers)
        {
            // キャラクターサイズと障害物レイヤーを設定
            checker.Setup(character_size, obstacle_layer);
        }
    }

    /// <summary>
    /// ワープ処理を実行
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="character_size">キャラクターサイズ</param>
    public void Warp(eWarpDirection direction)
    {
        Vector2 origin = transform.position;
        WarpChecker warp_checker = warpCheckers[(int)direction];

        var safe_point = warp_checker.GetWarpPoint(origin, warp_checker.transform.position);

        // ワープ先に移動
        transform.position = safe_point;
    }
}
