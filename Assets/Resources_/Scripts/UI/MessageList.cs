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
    [TextArea(1, 5)]
    public string text;   // メッセージ
    [TextArea(1, 5)]
    public string englishText; // 英語メッセージ
    public Sprite characterIcon;    // キャラクターアイコン
    public float addShowTime;      // 追加表示時間

    private PlayableDirector _playableDirector = null;  // イベントメッセージ用のタイムライン
    public PlayableDirector playableDirector {
        get { return _playableDirector; }
        set { _playableDirector = value; }
    }
    public bool isAutoForce = false;
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

    public void Clear() => _messageQueue.Clear();

    // 表示待ちメッセージがあるか確認する
    public bool HasMessages() {
        return _messageQueue.Count > 0;
    }
}
