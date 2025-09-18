using System.Collections;
using UnityEngine;

/// <summary>
/// メッセージを表示させるトリガークラス
/// </summary>
public class MessageTrigger : MonoBehaviour {
    [SerializeField] private MessageList _messageListScript; // メッセージリスト管理
    [SerializeField] private MessageDataList _messageDatas;  //メッセージデータ配列

    private MessageViewer _messageViewer; // メッセージビューア
    private bool _isPlayerInside = false; // プレイヤーがトリガー内にいるかどうか

    // メッセージを追加する
    private IEnumerator _AddMessage() {

        // メッセージ表示機能を探す (WIP)
        if(_messageViewer == null)
            _messageViewer = FindAnyObjectByType<MessageViewer>();

        // メッセージ表示中は待機
        if (!_messageDatas.isForced) {
            while (_messageListScript.HasMessages() || _messageViewer.IsShowing) {
                yield return null;

                if (!_isPlayerInside) {
                    // プレイヤーがトリガー外に出た場合、メッセージ追加を中止
                    yield break;
                }
            }
        }

        if(_messageDatas.isForced) {
            // 強制メッセージの場合、他のメッセージをクリア
            _messageListScript.Clear();
            _messageViewer.ForceReset();
        }

        // メッセージを追加
        foreach (MessageData message in _messageDatas.messageDatas) {
            _messageListScript.Enqueue(message);
        }
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.tag == "Player") {
            _isPlayerInside = true;
            StartCoroutine(_AddMessage());
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.tag == "Player") {
            _isPlayerInside = false;
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
