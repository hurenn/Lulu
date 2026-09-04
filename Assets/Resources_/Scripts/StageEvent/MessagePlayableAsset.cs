using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class MessagePlayableAsset : PlayableAsset {
    [SerializeField] private MessageSequenceAsset _messageSequence;  //メッセージデータアセット

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
        var playable = ScriptPlayable<MessagePlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        var messageDatas = new MessageData[_messageSequence.messages.Length];
        for (int i = 0; i < messageDatas.Length; i++) {
            var entry = _messageSequence.messages[i];
            messageDatas[i] = new MessageData {
                text = entry.text,
                characterIcon = entry.characterIcon,
                addShowTime = entry.addShowTime,
                isAutoForce = entry.isAutoForce,
                isUnScaledTime = entry.isUnScaledTime,
            };
        }

        if (playable.GetGraph().GetResolver() is PlayableDirector director) {
            foreach (var data in messageDatas) {
                data.playableDirector = director;
            }
            behaviour.director = director;
        }
        behaviour.messageDatas = messageDatas;
        behaviour.shakeWindow = _messageSequence.shakeWindow;
        behaviour.shakeIntensity = _messageSequence.shakeIntensity;
        behaviour.shakeDuration = _messageSequence.shakeDuration;
        return playable;
    }
}

public class MessagePlayableBehaviour : PlayableBehaviour {
    public MessageData[] messageDatas;
    public PlayableDirector director;
    public bool shakeWindow;
    public float shakeIntensity;
    public float shakeDuration;
    private bool shown = false;

    public override void OnBehaviourPlay(Playable playable, FrameData info) {
        if (shown) return;
        shown = true;

        // 手動メッセージの場合は再生を一時停止
        if (!messageDatas[0].isAutoForce) {
            director.Pause();
        }

        if (shakeWindow) {
            var viewer = GameObject.FindAnyObjectByType<MessageViewer>();
            viewer?.PrepareShakeOnShow(shakeIntensity, shakeDuration);
        }

        _AddMessage();
    }

    private void _AddMessage() {
        // メッセージ表示機能を探す (WIP)
        MessageViewer messageViewer = GameObject.FindAnyObjectByType<MessageViewer>(); ;
        MessageList _messageListScript = GameObject.FindAnyObjectByType<MessageList>();

        // 他のメッセージをクリア
        _messageListScript.Clear();
        messageViewer.ForceReset();

        // メッセージを追加
        foreach (MessageData message in messageDatas) {
            _messageListScript.Enqueue(message);
        }
    }
}
