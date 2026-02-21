using UnityEngine;

/// <summary>
/// プレイヤーが落下したときに、指定された位置に戻すエリア。
/// </summary>
public class FalledReturnArea : StageObject_Base {
    [SerializeField] private Transform _returnPoint; // プレイヤーが戻される位置

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        if (_returnPoint != null) {
            player.transform.position = _returnPoint.position;
        } else {
            Debug.LogWarning("FalledReturnArea: Return point is not set.");
        }
    }
}
