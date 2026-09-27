using UnityEngine;
using UnityEngine.Playables;

// ActivateMarkerを受け取ったら、対象のGameObjectが持つIActivatable実装コンポーネントの行動を開始させるだけの汎用プラグ
[RequireComponent(typeof(PlayableDirector))]
public class ActivateMarkerReceiver : MonoBehaviour, INotificationReceiver {
    private PlayableDirector _director;

    private void Awake() {
        _director = GetComponent<PlayableDirector>();
    }

    public void OnNotify(Playable origin, INotification notification, object context) {
        if (notification is not ActivateMarker marker) return;

        var targetObj = marker.target.Resolve(_director);
        var activatable = targetObj != null ? targetObj.GetComponent<IActivatable>() : null;
        activatable?.StartActing();
    }
}
