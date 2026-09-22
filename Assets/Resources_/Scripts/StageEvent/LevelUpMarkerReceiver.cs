using UnityEngine;
using UnityEngine.Playables;

// LevelUpMarkerを受け取ったら、指定量の経験値を与えるだけの汎用プラグ
public class LevelUpMarkerReceiver : MonoBehaviour, INotificationReceiver {
    public void OnNotify(Playable origin, INotification notification, object context) {
        if (notification is not LevelUpMarker marker) return;

        var player = PlayerCharacterManager.Current as Player_Character;
        player?.AddExp(marker.expAmount);
    }
}
