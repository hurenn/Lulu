using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Timeline上に置くだけでプレイヤーに経験値を与えるマーカー
public class LevelUpMarker : Marker, INotification {
    public int expAmount = 200; // 付与する経験値

    public PropertyName id => new PropertyName();
}

// LevelUpMarkerを受け取ったら、指定量の経験値を与えるだけの汎用プラグ
public class LevelUpMarkerReceiver : MonoBehaviour, INotificationReceiver {
    public void OnNotify(Playable origin, INotification notification, object context) {
        if (notification is not LevelUpMarker marker) return;

        var player = PlayerCharacterManager.Current as Player_Character;
        player?.AddExp(marker.expAmount);
    }
}
