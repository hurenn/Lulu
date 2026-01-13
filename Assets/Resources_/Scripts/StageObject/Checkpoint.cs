using UnityEngine;

public class Checkpoint : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D other) {
        var player = other.GetComponentInChildren<Player_Character>();
        if (player != null) {
            CheckpointManager.Instance.SaveCheckpoint(transform.position, player.GetParameterSnapshot());
        }
    }
}