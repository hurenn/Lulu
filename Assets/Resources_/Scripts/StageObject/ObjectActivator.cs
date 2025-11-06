using UnityEngine;

/// <summary>
/// 指定したオブジェクトを有効化するステージオブジェクト
/// </summary>
public class ObjectActivator : StageObject_Base {
    [SerializeField] private GameObject[] _targetObjects;

    protected override void _HitPlayer(Player_Character player) {
        foreach (var obj in _targetObjects) {
            if (obj != null) {
                obj.SetActive(true);
            }
        }
    }
}
