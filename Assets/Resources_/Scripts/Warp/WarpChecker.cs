using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class WarpChecker : MonoBehaviour
{
    [SerializeField]
    float step_interval = 0.25f;
    [SerializeField]
    int max_steps = 30;

    public Vector2 GetWarpPoint(Vector2 origin, Vector2 target, Vector2 characterSize, LayerMask obstacleLayer, float buffer = 0.05f)
    {
        // キャラクター位置からワープ先までの方向と距離を計算
        Vector2 direction = (target - origin).normalized;
        float totalDistance = Vector2.Distance(origin, target);

        // 目的地がワープ可能かチェック
        RaycastHit2D directCheck = Physics2D.BoxCast(target, characterSize, 0, Vector2.zero, 0f, obstacleLayer);
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
            var is_warp_point = IsValidWarpPoint(check_pos, characterSize, obstacleLayer);
            if(is_warp_point.HasValue)
            {
                return is_warp_point.Value; // ワープ可能な位置を返す
            }

            // 少し上にずらしてチェック
            check_pos.y += characterSize.y;
            is_warp_point = IsValidWarpPoint(check_pos, characterSize, obstacleLayer);
            if (is_warp_point.HasValue)
            {
                return is_warp_point.Value; // ワープ可能な位置を返す
            }
        }

        return origin; // どの方向にもワープできない場合は元の位置を返す
    }

    private Vector2? IsValidWarpPoint(Vector2 point, Vector2 characterSize, LayerMask obstacleLayer)
    {
        RaycastHit2D warpCheck = Physics2D.BoxCast(point, characterSize, 0, Vector2.zero, 0f, obstacleLayer);
        return warpCheck.collider == null ? point : (Vector2?)null;
    }
}
