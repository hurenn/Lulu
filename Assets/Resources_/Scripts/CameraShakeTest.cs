using Unity.Cinemachine;
using UnityEngine;

public class CameraShakeTest : StageObject_Base {
    [SerializeField] private CinemachineImpulseSource impulseSource;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        // ƒJƒƒ‰‚ğ—h‚ç‚·
        if (impulseSource != null) {
            impulseSource.GenerateImpulse();
            Debug.Log("Shake triggered on hit!");
        }
    }
}