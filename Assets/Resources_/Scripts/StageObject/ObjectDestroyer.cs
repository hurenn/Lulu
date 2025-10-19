using UnityEngine;

public class ObjectDestroyer : StageObject_Base {
    [SerializeField] private GameObject[] _targetObjects;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        foreach (var obj in _targetObjects) {
            if (obj != null) {
                Destroy(obj);
            }
        }
    }
}
