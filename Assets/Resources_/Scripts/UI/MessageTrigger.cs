using System.Collections;
using UnityEngine;

/// <summary>
/// メッセージを表示させるトリガークラス
/// </summary>
public class MessageTrigger : MonoBehaviour {
    [SerializeField] private MessageList _messageListScript; // メッセージリスト管理
    [SerializeField] private MessageData[] _messageDatas;  //メッセージデータ配列

    // メッセージ表示待機フラグ(他のメッセージ表示中にトリガーオブジェクトを抜けた時、メッセージ表示を行わないようにするためのフラグ)
    private bool _messageAddWait = false;

    // メッセージを追加する
    private void _AddMessage() {
        foreach (MessageData message in _messageDatas) {
            _messageListScript.Enqueue(message);
        }
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.tag == "Player") {
            _AddMessage();
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.tag == "Player") {
            //_messageAddWait = false;  //プレイヤーが離れた時（他のメッセージ表示中に、このトリガーオブジェクトを抜けた時）開始フラグを戻す
        }
    }

    //IEnumerator endMessage() {
    //    _messageListScript.isForceMessageViewing = true;
    //    yield return new WaitForSeconds(0.2f);
    //    _messageListScript.isForceMessageViewing = false;
    //    _MessageStart();
    //}

    //// Update is called once per frame
    //void Update() {
    //    if (_messageListScript.isForceMessageViewing) {
    //        _messageStartWait = false;
    //    }

    //    if (_messageStartWait == true)  //メッセージ開始フラグが設定されているとき
    //    {
    //        if (!_messageListScript.isMessageViewing)  //かつ、他のメッセージが表示されていないとき
    //        {
    //            _MessageStart();
    //        }
    //    }
    //}

    ////メッセージ開始処理
    //private void _MessageStart() {
    //    messageList.SetActive(true);  //メッセージリストをアクティブ化

    //    gameObject.SetActive(false);
    //    //Destroy(gameObject, 1f);  //このトリガーオブジェクトを消す
    //}
}
