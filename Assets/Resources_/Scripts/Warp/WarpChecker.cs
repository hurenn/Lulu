using System.Drawing;
using UnityEngine;

public class WarpChecker : MonoBehaviour
{
    [SerializeField]
    float step_interval = 0.25f;
    [SerializeField]
    int max_steps = 30;

    private Vector2 _characterSize = default;
    private LayerMask _obstacleLayer = default;

    // 個別のワープチェック用のオフセット
    [SerializeField]
    private bool _isEnableUpperCheck = true;
    private float _upperOffset => _characterSize.y;

    private bool _isValidWarpPoint = false;
    private bool _isUpperWarp = false;

    /// <summary>
    /// セットアップ
    /// </summary>
    /// <param name="character_size">キャラクターサイズ</param>
    /// <param name="obstacle_layer">対象レイヤー</param>
    public void Setup(Vector2 character_size, LayerMask obstacle_layer)
    {
        _characterSize = character_size;
        _obstacleLayer = obstacle_layer;
    }

    /// <summary>
    /// ワープ先チェック
    /// </summary>
    /// <param name="origin">開始地点</param>
    /// <param name="target">ワープ先</param>
    public Vector2 GetWarpPoint(Vector2 origin, Vector2 target)
    {
        // キャラクター位置からワープ先までの方向と距離を計算
        Vector2 direction = (target - origin).normalized;
        float totalDistance = Vector2.Distance(origin, target);

        // 目的地がワープ可能かチェック
        RaycastHit2D directCheck = Physics2D.BoxCast(target, _characterSize, 0, Vector2.zero, 0f, _obstacleLayer);
        if (directCheck.collider == null)
        {
            return target; // 直接ワープ可能
        }

        int step_count = Mathf.CeilToInt(totalDistance / step_interval);
        step_count = Mathf.Min(step_count, max_steps);

        for(int i = 0; i <= step_count; i++)
        {
            Vector3 check_pos = Vector3.Lerp(target, origin, (float)i / step_count);

            // 障害物との衝突をチェック
            var is_warp_point = IsValidWarpPoint(check_pos);
            if(is_warp_point.HasValue)
            {
                _isUpperWarp = false;
                return is_warp_point.Value; // ワープ可能な位置を返す
            }

            /*// 少し上にずらしてチェック
            if (_isEnableUpperCheck) {
                check_pos.y += _upperOffset;
                is_warp_point = IsValidWarpPoint(check_pos);
                if (is_warp_point.HasValue) {
                    _isUpperWarp = true;
                    return is_warp_point.Value; // ワープ可能な位置を返す
                }
            }*/
        }

        return origin; // どの方向にもワープできない場合は元の位置を返す
    }

    /// <summary>
    /// ワープ地点チェック
    /// </summary>
    /// <param name="point">チェック地点</param>
    /// <returns></returns>
    private Vector2? IsValidWarpPoint(Vector2 point)
    {
        RaycastHit2D warpCheck = Physics2D.BoxCast(point, _characterSize, 0, Vector2.zero, 0f, _obstacleLayer);
        _isValidWarpPoint = warpCheck.collider == null;
        return _isValidWarpPoint ? point : (Vector2?)null;
    }

    #region デバッグ用
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = _isValidWarpPoint && !_isUpperWarp
    //        ? UnityEngine.Color.green : UnityEngine.Color.cyan;
    //    Gizmos.DrawWireCube(transform.position, _characterSize);

    //    Gizmos.color = _isValidWarpPoint && _isUpperWarp
    //        ? UnityEngine.Color.green : UnityEngine.Color.gray;
    //    Gizmos.DrawWireCube(transform.position + Vector3.up * _upperOffset, _characterSize);
    //}
    #endregion
}
