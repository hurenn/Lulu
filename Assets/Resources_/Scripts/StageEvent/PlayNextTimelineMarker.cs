using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Timeline終了間際に置き、自然終了時に次のTimelineを再生させるためのマーカー(分岐後の共通イベントへの合流用)
public class PlayNextTimelineMarker : Marker, INotification {
    public PlayableDirector next;

    public PropertyName id => new PropertyName();
}

// PlayNextTimelineMarkerを受け取ったら次のTimelineを再生するだけの汎用プラグ
public class PlayNextTimelineMarkerReceiver : MonoBehaviour, INotificationReceiver {
    public void OnNotify(Playable origin, INotification notification, object context) {
        if (notification is PlayNextTimelineMarker marker && marker.next != null) {
            marker.next.Play();
        }
    }
}
