using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Fungus;

public class MessageViewer : MonoBehaviour
{
    //実際にメッセージを表示させるスクリプト

    public List<string> messageList = new List<string>();  //メッセージリスト
    float wordSpeed = 0.02f;  //1文字当たりの表示速度

    Text txtMessage;         //メッセージ表示用
    GameObject iconNextTap;  //タップを促す画像表示

    private int messageListIndex = 0;  //表示メッセージの配列番号
    private int wordCount;          //1メッセージ当たりの文字の総数
    private bool isTapped = false;  //全文表示後にタップを待つフラグ
    private bool isDisplayedAllMessage = false;  //全メッセージ表示完了のフラグ

    private IEnumerator waitCoroutine;  //全文表示までの待機時間メソッド代入用 Stop出来るようにしておく
    private Tween tween;                //DoTween再生用  Kill出来るように代入して使用する

    public bool auto;  //メッセージオート進行フラグ
    GameObject textWindow;  //メッセージウィンドウ
    bool startFlag;  //メッセージ開始フラグ
    bool endFlag;    //メッセージ終了フラグ

    public Sprite chara;     //キャラクターアイコン
     string characterName;  //キャラクター名

    GameObject Mask;  //アイコン表示用
    public float plusTime;//ウィンドウ表示時間の追加

// Start is called before the first frame update
void Start()
    {
        if (messageList.Count == 0)
            messageEnd();

        //キャラクター名自動設定
        string[] a;  //一時保存用
        if (chara)
            a = chara.name.Split();  //キャラ名+状態 分割
        else
        {
            a = new string[1];
            a[0] = " ";
        }
        characterName = a[0];   //キャラクター名だけ取得 設定
        
        Mask =GameObject.Find("Message Window").transform.GetChild(0).gameObject.
              transform.GetChild(0).gameObject;  //アイコン表示用オブジェクト取得

        SE.playnum = 15;  //メッセージ表示中効果音 発生
        textWindow = GameObject.Find("Message Window").transform.GetChild(0).gameObject;  //テキストウィンドウ取得
        txtMessage = GameObject.Find("Message Window").transform.GetChild(0).gameObject.  //テキスト取得
            transform.GetChild(1).gameObject.GetComponent<Text>();
        iconNextTap = GameObject.Find("Message Window").transform.GetChild(0).gameObject.  //タップアイコン取得
            transform.GetChild(2).gameObject;
        iconNextTap.SetActive(true);  //タップアイコン表示
    }

    ///<summary>
    ///メッセージ開始処理
    ///</summary>
    IEnumerator startWait()  //メッセージ開始処理
    {
        yield return new WaitForSeconds(0.1f);  //一瞬待機
        messageListIndex = 0;  //メッセージ番号初期化
        startFlag = true;  //メッセージ開始フラグ設定
        textWindow.SetActive(true);  //ウィンドウ表示実行

        if (chara)  //キャラクターアイコンが設定されているとき
        {
            Mask.transform.GetChild(0).gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.7f);  //アイコン表示
            Mask.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = chara;  //画像設定
        }
        else  //アイコンが設定されていない時
        {
            Mask.transform.GetChild(0).gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0);  //アイコン非表示
        }

        if (!characterName.Equals(""))
        {
            Mask.transform.GetChild(1).gameObject.GetComponent<Text>().text = characterName;  //キャラクター名表示設定
        }
        StartCoroutine(DisplayMessage());  //メッセージ表示処理
    }

    ///<summary>
    ///一番上のメッセージまで遡って非表示にする処理
    ///</summary>
    public void messageEnd()
    {
        if (transform.parent.gameObject.GetComponent<MessageViewer>())
        {
            transform.parent.gameObject.GetComponent<MessageViewer>().messageEnd();  //親オブジェクトがViewerを持っていれば参照する
        }
        else if (transform.parent.gameObject.GetComponent<MessageList>())  //メッセージリストまで遡った時
        {
            transform.parent.gameObject.SetActive(false);  //メッセージを非表示にする
            MessageList.MessageNow = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(gameObject.name);
        //強制終了処理
        if (MessageList.MessageNow == false)
        {
            messageEnd();
        }

        if (!startFlag && !endFlag)  //メッセージ開始前
        {
            if (!textWindow)  //バグ防止
                return;
            if (textWindow.activeSelf == true)  //メッセージウィンドウが非表示になるまで待機
            {
                return;
            }
            StartCoroutine("startWait");  //メッセージ開始処理
        }

        if (isDisplayedAllMessage)  //メッセージ表示終了後
        {
            isDisplayedAllMessage = false;  //一度のみ実行
            endFlag = true;  //表示終了フラグ
            textWindow.SetActive(false);  //ウィンドウ非表示

            if (transform.childCount == 0)  //次のメッセージが無い場合
            {
                messageEnd();  //全メッセージ非表示処理
            }
            else  //次のメッセージがある場合
            {
                transform.GetChild(0).gameObject.SetActive(true);  //次のメッセージをアクティブ化
            }
            return;
        }

        if (!auto && Input.GetKeyDown(KeyCode.Z) && tween != null)  //タップ処理（オートでない場合）
        {
            //文字送り中にタップした場合、文字送りを停止
            tween.Kill();
            tween = null;

            //文字送りのための待機時間も停止
            if (waitCoroutine != null)
            {
                StopCoroutine(waitCoroutine);
                waitCoroutine = null;
            }
            
            txtMessage.text = messageList[messageListIndex];  //全文まとめて表示
            
            StartCoroutine(NextTouch());  //タップするまで全文を表示したまま待機
        }

        if (!auto && Input.GetKeyDown(KeyCode.Z) && wordCount == messageList[messageListIndex].Length)  //全文表示中にタップ
        {
            isTapped = true;  //全文表示を終了
        }
    }

    ///<summary>
    ///1文字ずつメッセージ表示実行
    ///</summary>
    private IEnumerator DisplayMessage()
    {
        isTapped = false;  //タップ待ちフラグ初期化
         //表示テキストとTweenをリセット
        txtMessage.text = "";
        tween = null;
        if (waitCoroutine != null)  //文字送りの待機時間を初期化
        {
            StopCoroutine(waitCoroutine);  //Coroutineを止めて初期化
            waitCoroutine = null;
        }

        //1文字ずつの文字送り表示が終了するまでループ
        while (messageList[messageListIndex].Length > wordCount)
        {
            //wordSpeed秒ごとに文字を1文字ずつ表示。SetEase(Ease.Linear)をセットすることで一定の時間間隔で表示
            tween = txtMessage.DOText(messageList[messageListIndex], messageList[messageListIndex].Length * wordSpeed).
                SetEase(Ease.Linear).OnComplete(() =>
                {
                    //Debug.Log("全文表示完了");
                });

            //文字送り表示が終了するまでの待機時間を設定して待機を実行
            waitCoroutine = WaitTime();
            yield return StartCoroutine(waitCoroutine);
        }
        if (!auto)  //オート進行でない場合
        {
            //タップするまで全文を表示したまま待機
            StartCoroutine(NextTouch());
        }
    }

    ///<summary>
    ///メッセージ表示時間設定
    ///</summary>
    private IEnumerator WaitTime()
    {
        yield return new WaitForSeconds(messageList[messageListIndex].Length * wordSpeed);  //文字数×表示速度 の待機時間    (タップした場合は停止)
        wordCount = messageList[messageListIndex].Length;  //文字数取得

        if (auto)  //オート進行の場合   アイコンを徐々に消して残り時間を表示する処理
        {
            Mask.transform.parent.gameObject.transform.GetChild(2).gameObject.GetComponent<Image>().fillAmount = 1;  //アイコン表示初期化

            float messageTime = wordSpeed * messageList[messageListIndex].Length + 3.5f + plusTime; //メッセージ表示時間   （全文表示後3.5fの猶予と、plustimeでの調整）

            DOTween.To  //残り表示時間に応じてアイコンを消す
            (
           () => Mask.transform.parent.gameObject.transform.GetChild(2).gameObject.GetComponent<Image>().fillAmount,       //何に
           x => Mask.transform.parent.gameObject.transform.GetChild(2).gameObject.GetComponent<Image>().fillAmount = x,  //何を
           0,     //どこまで(最終的な値)
           messageTime//どれくらいの時間
            );
            yield return new WaitForSeconds(messageTime);   //アイコンが消えるまで待機

            isTapped = true;  //タップされたことを自動設定

            if (messageListIndex + 1 == messageList.Count)  //メッセージリストが全て表示されたとき
            {
                textWindow.SetActive(false);  //ウィンドウ非表示
            }
            StartCoroutine(NextTouch());
        }
    }

    ///<summary>
    ///タップするまで全文を表示したまま待機
    ///</summary>
    private IEnumerator NextTouch()
    {
        yield return null;
        //表示した文字の総数を更新
        wordCount = messageList[messageListIndex].Length;

        //タップを待つ
        yield return new WaitUntil(() => isTapped);

        //次のメッセージへ移行
        messageListIndex++;
        wordCount = 0;

        //リストに未表示のメッセージが残っている場合
        if (messageListIndex < messageList.Count)
        {
            SE.playnum = 15;    //メッセージ表示効果音  
            StartCoroutine(DisplayMessage());  //1文字ずつ表示する処理をスタート
        }
        else
        {
            //全メッセージ表示終了
            isDisplayedAllMessage = true;

            //次の処理へ
        }
    }
}