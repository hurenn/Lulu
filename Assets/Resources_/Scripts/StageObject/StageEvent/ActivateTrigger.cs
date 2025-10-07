using UnityEngine;

public class ActivateTrigger : StageObject_Base
{
    [SerializeField] private GameObject[] targetObject;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);

        foreach (var obj in targetObject) {
            if (obj != null) {
                obj.SetActive(true);
            }
        }
    }
}
