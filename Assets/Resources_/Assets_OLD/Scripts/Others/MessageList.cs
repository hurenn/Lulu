using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageList : MonoBehaviour
{
    //メッセージ群を管理するリスト

    int ObjCount;  //メッセージ数
    int timer = 0; //現在のメッセージ
    public static bool nowForce; //強制メッセージが流れているか判定
    public static bool MessageNow; //なんらかのメッセージが表示中か判定

    void Start()
    {
    }

    void activate()
    {
        if (timer < ObjCount)  //全メッセージ表示までループ
        {
            StartCoroutine("wait");
        }
    }

    void Init()
    {
        MessageNow = false;
        GameObject.Find("Message Window").transform.GetChild(0).gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //強制メッセージが流れたとき
        if (nowForce)
        {
            gameObject.SetActive(false);
            Init();
            return;
        }

        if (!MessageNow)//現在進行中の他メッセージがあるかをチェック
        {
            ObjCount = this.transform.childCount;  //メッセージの数を取得
            activate();
        }
    }
    IEnumerator wait()
    {
        MessageNow = true;  //メッセージ進行中フラグ設定
        transform.GetChild(timer).gameObject.SetActive(true);  //順番にメッセージ表示
        timer++;            //次のメッセージへ移行
        yield return new WaitForSeconds(1f);
        activate();         //残りメッセージ判定
    }
}
