using System.Drawing;
using UnityEngine;

public class WarpChecker : MonoBehaviour
{
    [SerializeField]
    float step_interval = 0.25f;
    [SerializeField]
    int max_steps = 30;

    [SerializeField]
    private BoxCollider2D _col;
    private Vector2 _characterSize => _col.bounds.size;

    [SerializeField]
    private LayerMask _obstacleLayer = default;
    [SerializeField]
    private LayerMask _damageZoneLayer = default;

    // 個別のワープチェック用のオフセット
    [SerializeField]
    private bool _isEnableUpperCheck = true;

    private bool _isValidWarpPoint = false;
    private bool _isUpperWarp = false;

    /// <summary>
    /// ワープ可能な場所を取得
    /// </summary>
    /// <param name="origin">開始地点</param>
    /// <param name="target">ワープ先</param>
    public Vector2 GetWarpDestination(Vector2 origin, Vector2 target)
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

        // ワープ先との間で安全な場所を確認する回数
        int step_count = Mathf.CeilToInt(totalDistance / step_interval);
        step_count = Mathf.Min(step_count, max_steps);

        for(int i = 0; i <= step_count; i++)
        {
            Vector3 check_pos = Vector3.Lerp(target, origin, (float)i / step_count);

            // 障害物との衝突をチェック
            var is_warp_point = GetWarpPoint(check_pos);
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
    /// 位置指定してワープ地点チェック
    /// </summary>
    /// <param name="point">チェック地点</param>
    /// <returns></returns>
    public Vector2? GetWarpPoint(Vector2 point, LayerMask add_layer_mask = default)
    {
        RaycastHit2D warpCheck = Physics2D.BoxCast(point, _characterSize, 0, Vector2.zero, 0f, _obstacleLayer | add_layer_mask);

        // 衝突していなければワープ可能
        _isValidWarpPoint = warpCheck.collider == null;
        return _isValidWarpPoint ? point : null;
    }

    private void _WarpFailed(Vector2 target_pos)
    {
        // ワープ失敗時のエフェクトなどをここで実装
        Debug.Log($"{target_pos} へのワープが失敗しました");
    }

    /// <summary>
    /// ワープ地点チェック
    /// </summary>
    /// <param name="is_damage_avoid">ダメージを受ける場所を含めるか</param>
    /// <returns></returns>
    public Vector2? GetWarpPoint(bool is_damage_avoid = false)
    {
        return GetWarpPoint(transform.position, is_damage_avoid ? _damageZoneLayer : default);
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
