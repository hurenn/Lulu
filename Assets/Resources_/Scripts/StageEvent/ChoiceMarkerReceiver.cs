using UnityEngine;
using UnityEngine.Playables;

// ChoiceMarkerを受け取ったら、その場でTimelineを一時停止しMessageViewerに選択肢を渡すだけの汎用プラグ
[RequireComponent(typeof(PlayableDirector))]
public class ChoiceMarkerReceiver : MonoBehaviour, INotificationReceiver {
    private PlayableDirector _director;
    private MessageList _messageListScript;
    private MessageViewer _messageViewer;

    private void Awake() {
        _director = GetComponent<PlayableDirector>();
    }

    public void OnNotify(Playable origin, INotification notification, object context) {
        if (notification is not ChoiceMarker marker) return;

        if (_messageListScript == null) _messageListScript = FindAnyObjectByType<MessageList>();
        if (_messageViewer == null) _messageViewer = FindAnyObjectByType<MessageViewer>();

        _director.Pause();

        if (marker.prompt != null && !string.IsNullOrEmpty(marker.prompt.key)) {
            // 質問文を通常のメッセージとして表示し、表示完了後に選択肢モードへ移行させる
            marker.prompt.playableDirector = _director;
            marker.prompt.choices = marker.choices;
            _messageListScript.Enqueue(marker.prompt);
        } else {
            // 質問文なし、直前の表示を維持したまま選択肢のみ出す
            _messageViewer.EnterChoiceMode(marker.choices, _director);
        }
    }
}
