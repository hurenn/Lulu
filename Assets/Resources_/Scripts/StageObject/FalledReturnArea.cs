using UnityEngine;

/// <summary>
/// プレイヤーが落下したときに、指定された位置に戻すエリア。
/// </summary>
public class FalledReturnArea : StageObject_Base {
    [SerializeField] private Transform _returnPoint; // デフォルトの復帰地点（FallReturnPointが設定されていない場合に使用）

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        
        // FallReturnPointManagerから最後の復帰地点を取得
        Vector3? lastReturnPoint = FallReturnPointManager.Instance.GetLastReturnPoint();
        
        if (lastReturnPoint.HasValue) {
            // 最後に通過したFallReturnPointがある場合、そこに戻す
            player.transform.position = lastReturnPoint.Value;
        } else if (_returnPoint != null) {
            // FallReturnPointが設定されていない場合、デフォルトの復帰地点に戻す
            player.transform.position = _returnPoint.position;
        } else {
            Debug.LogWarning("FalledReturnArea: No return point is set.");
        }
    }
}
