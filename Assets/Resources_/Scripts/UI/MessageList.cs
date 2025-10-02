using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MessageDataList {
    public MessageData[] messageDatas;
    public bool isForced;        // 強制メッセージかどうか
}

[System.Serializable]
public class MessageData {
    public string text;   // メッセージ
    public Sprite characterIcon;    // キャラクターアイコン
    public string characterName;    // キャラクター名
    public bool isEventMessage;  // イベントメッセージかどうか
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
