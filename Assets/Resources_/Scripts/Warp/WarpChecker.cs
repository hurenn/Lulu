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
    public float characterWidth => _characterSize.x;

    [SerializeField]
    private LayerMask _obstacleLayer = default;
    [SerializeField]
    private LayerMask _enemyLayer = default;
    [SerializeField]
    private LayerMask _damageZoneLayer = default;

    // 個別のワープチェック用のオフセット
    [SerializeField]
    private float _upperCheckRate = 0.0f;

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
        RaycastHit2D obstacleCheck = Physics2D.BoxCast(target, _characterSize, 0, Vector2.zero, 0f, _obstacleLayer);
        RaycastHit2D enemyCheck = Physics2D.BoxCast(target, _characterSize, 0, Vector2.zero, 0f, _enemyLayer);
        if (obstacleCheck.collider == null && enemyCheck.collider == null) {
            _isUpperWarp = false;
            return target; // 直接ワープ可能
        }

        // 上方向・斜め上方向のワープで最大距離地点が地形に埋まっている場合、少し上を追加でチェック
        // プレイヤー側になるべく近い地点（オフセットが小さい方）から順にチェックする
        if (_upperCheckRate > 0f && direction.y > 0f) {
            float max_upper_offset = _characterSize.y * _upperCheckRate;
            int upper_step_count = Mathf.Max(1, Mathf.CeilToInt(max_upper_offset / step_interval));
            for (int i = 1; i <= upper_step_count; i++) {
                float upper_offset = Mathf.Min(step_interval * i, max_upper_offset);
                Vector2 upper_check_pos = target + Vector2.up * upper_offset;
                var upper_warp_point = GetWarpPoint(upper_check_pos);
                if (upper_warp_point.HasValue) {
                    _isUpperWarp = true;
                    return upper_warp_point.Value; // プレイヤーに最も近い上方向の位置をワープ先に設定
                }
            }
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
