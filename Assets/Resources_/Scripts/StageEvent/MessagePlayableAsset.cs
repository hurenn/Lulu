using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class MessagePlayableAsset : PlayableAsset {
    [SerializeField] private MessageData[] messageDatas;  //メッセージデータ配列
    [Header("ウィンドウ振動")]
    [SerializeField] private bool shakeWindow = false;
    [SerializeField] private float shakeIntensity = 20f;
    [SerializeField] private float shakeDuration = 0.4f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
        var playable = ScriptPlayable<MessagePlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        if (playable.GetGraph().GetResolver() is PlayableDirector director) {
            foreach (var data in messageDatas) {
                data.playableDirector = director;
            }
            behaviour.director = director;
        }
        behaviour.messageDatas = messageDatas;
        behaviour.shakeWindow = shakeWindow;
        behaviour.shakeIntensity = shakeIntensity;
        behaviour.shakeDuration = shakeDuration;
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
