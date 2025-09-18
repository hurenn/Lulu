using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MessageDataList {
    public MessageData[] messageDatas;
    public bool isForced;         // 強制メッセージかどうか
}

[System.Serializable]
public class MessageData {
    public string text;   // メッセージ
    public Sprite characterIcon;    // キャラクターアイコン
    public string characterName;    // キャラクター名
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

    //private bool _isForceMessageViewing;    // 強制メッセージが流れているか判定
    //public bool isForceMessageViewing {
    //    get { return _isForceMessageViewing; }
    //    set { _isForceMessageViewing = value; }
    //}
    //private bool _isMessageViewing;         // なんらかのメッセージが表示中か判定
    //public bool isMessageViewing {
    //    get { return _isMessageViewing; }
    //    set { _isMessageViewing = value; }
    //}

    //private void _Reset() {
    //    _isMessageViewing = false;
    //    GameObject.Find("Message Window").transform.GetChild(0).gameObject.SetActive(false);
    //}

    // Update is called once per frame
    //void Update() {
        ////強制メッセージが流れたとき
        //if (_isForceMessageViewing) {
        //    gameObject.SetActive(false);
        //    _Reset();
        //    return;
        //}

        //if (!_isMessageViewing)//現在進行中の他メッセージがあるかをチェック
        //{
        //    _messageCount = this.transform.childCount;  //メッセージの数を取得
        //    _activate();
        //}
    //}

    //IEnumerator _ViewMessageWait() {
    //    _isMessageViewing = true;   //メッセージ進行中フラグ設定
    //    _currentMessage++;          //次のメッセージへ移行
    //    transform.GetChild(_currentMessage).gameObject.SetActive(true); //順番にメッセージ表示

    //    yield return new WaitForSeconds(1f);
    //    _activate();         //残りメッセージ判定
    //}

    //private void _activate() {
    //    if (_currentMessage < _messageCount)  // 全メッセージ表示までループ
    //    {
    //        StartCoroutine(_ViewMessageWait());
    //    }
    //}
}
