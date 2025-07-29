using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageTrigger : MonoBehaviour
{
    //主人公が触れた時に特定のメッセージを表示させるためのトリガーオブジェクト

    public GameObject Message;  //表示させるメッセージリストを取得
    bool messageStart = false;  //メッセージ表示中フラグ
    public bool forced;         //メッセージ進行中に関わらず強制発言

    void Start()
    {

    }

    IEnumerator endMessage()
    {
        MessageList.nowForce = true;
        yield return new WaitForSeconds(0.2f);
        MessageList.nowForce = false;
        MessageStart();
    }
 
    //メッセージ開始条件1
    public void StartTrigger() 
    {
        if (forced && MessageList.nowForce == false)  //このメッセージが強制メッセージの場合
        {
            StartCoroutine("endMessage");
        }
        else
        {
            messageStart = true;  //メッセージ開始フラグ
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (MessageList.nowForce)
        {
            messageStart = false;
        }

        if (messageStart == true)  //メッセージ開始フラグが設定されているとき
        {
            if (!MessageList.MessageNow)  //かつ、他のメッセージが表示されていないとき
            {
                MessageStart();
            }
        }
    }

    //メッセージ開始処理
    void MessageStart()
    {
        Message.SetActive(true);  //メッセージリストをアクティブ化

        gameObject.SetActive(false);
        //Destroy(gameObject, 1f);  //このトリガーオブジェクトを消す
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            StartTrigger();  //プレイヤーに触れたらメッセージ開始処理
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            messageStart = false;  //プレイヤーが離れた時（他のメッセージ表示中に、このトリガーオブジェクトを抜けた時）開始フラグを戻す
        }
    }
}
