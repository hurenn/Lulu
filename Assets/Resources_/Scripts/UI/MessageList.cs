using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class MessageDataList {
    public MessageData[] messageDatas;
    public bool isForced;        // 強制メッセージかどうか
}

[System.Serializable]
public class MessageData {
    public string key;   // 外部テキストテーブル(Assets/Resources/Localization/Messages.json)を引くキー
    public Sprite characterIcon;    // キャラクターアイコン
    public float addShowTime;      // 追加表示時間

    private PlayableDirector _playableDirector = null;  // イベントメッセージ用のタイムライン
    public PlayableDirector playableDirector {
        get { return _playableDirector; }
        set { _playableDirector = value; }
    }
    public bool isAutoForce = false;
    public bool isUnScaledTime = false;

    // trueならTimelineが無くてもボタン入力を待って手動送りにする(TalkTrigger等の会話イベント用)
    public bool waitForButton = false;

    // 非nullならこの行の表示完了後に選択肢を表示する。
    // ChoiceMarkerが実行時にセットするだけの値なのでInspectorでは編集させない
    // (MessageChoiceOption.branchMessagesがMessageData[]を持つため、Serializableのままだと
    //  MessageData<->MessageChoiceOptionの循環参照になりUnityのシリアライズ深度上限エラーになる)
    [System.NonSerialized] public MessageChoiceOption[] choices;
}

/// <summary>
/// メッセージリスト管理クラス
/// </summary>
public class MessageList : MonoBehaviour {
    public static MessageList Instance { get; private set; }

    public event System.Action OnForceMessage;  // 強制メッセージ開始イベント

    // メッセージキュー
    private Queue<MessageData> _messageQueue = new Queue<MessageData>();

    // メッセージを登録する
    public void Enqueue(MessageData messageData) => _messageQueue.Enqueue(messageData);
    // メッセージを取得して削除する
    public MessageData Dequeue() => _messageQueue.Dequeue();

    // 選択肢の分岐メッセージなど、キューの先頭に割り込ませて登録する
    public void InsertFront(MessageData[] items) {
        var new_queue = new Queue<MessageData>(items);
        foreach (var m in _messageQueue) {
            new_queue.Enqueue(m);
        }
        _messageQueue = new_queue;
    }

    public void Clear() => _messageQueue.Clear();

    // 表示待ちメッセージがあるか確認する
    public bool HasMessages() {
        return _messageQueue.Count > 0;
    }
}
