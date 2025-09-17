using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// メッセージ表示クラス
/// </summary>
public class MessageViewer : MonoBehaviour {
    private const float _BASE_SHOW_TIME = 2.0f; // 基本表示時間
    private const float _CHARACTER_SHOW_TIME = 0.05f; // 1文字あたりの追加表示時間
    private const float _COOL_TIME = 0.5f;  // メッセージ表示クールタイム

    [SerializeField] private MessageList _messageListScript;    // メッセージリスト管理
    [SerializeField] private TMP_Text _messageText;                 // メッセージ表示用テキスト
    [SerializeField] private TMP_Text _characterText;               // キャラクター名表示用テキスト
    [SerializeField] private Image _iconImage;                  // キャラクターアイコン表示用イメージ
    [SerializeField] private GameObject _messagePanel;          // メッセージパネル

    private MessageData _currentMessage;  // 現在表示中のメッセージ
    private float _currentShowTime;   // 現在の表示時間
    private bool _isShowing;    // メッセージ表示中フラグ
    private bool _isSeries;     // 一連のメッセージフラグ
    private float _currentCoolTime; // 次のメッセージを表示するまでのクールタイム

    private void Update() {
        if (_currentCoolTime > 0) {
            _currentCoolTime -= Time.deltaTime;
            return;
        }

        // 次のメッセージを表示
        if(!_isShowing && _messageListScript.HasMessages()) {
            _ShowNext();
        }

        // メッセージ表示中なら時間をカウントダウン
        if (_isShowing && _currentShowTime > 0) {
            _currentShowTime -= Time.deltaTime;
            if(_currentShowTime <= 0f) {
                // 表示時間終了
                _HideOrNext();
            }
        }
    }

    private void _ShowNext() {
        _currentMessage = _messageListScript.Dequeue(); // 次のメッセージを取得
        _messageText.text = _currentMessage.text;       // メッセージをセット
        _characterText.text = _currentMessage.characterName; // キャラクター名をセット
        _iconImage.sprite = _currentMessage.characterIcon;   // キャラクターアイコンをセット
        _isSeries = _currentMessage.isSeries;   // 一連メッセージフラグをセット

        _messagePanel.SetActive(true);                  // パネルを表示
        _currentShowTime = _BASE_SHOW_TIME + (_currentMessage.text.Length * _CHARACTER_SHOW_TIME); // 基本3秒 + 文字数に応じた追加時間
        _isShowing = true;
    }

    private void _HideOrNext() {
        _isShowing = false;

        // 一連のメッセージ表示中で無ければ一旦パネルを消す
        if (!_isSeries) {
            _messagePanel.SetActive(false); // パネルを非表示
            _currentCoolTime = _COOL_TIME;  // クールタイム設定
        }
    }

    //public List<string> messageList = new List<string>();   // メッセージリスト
    //private float _wordSpeed = 0.02f;                       // 1文字当たりの表示速度

    //[SerializeField] private Text _messageText;             // メッセージ表示用テキスト
    //[SerializeField] private GameObject _textWindow;        // メッセージウィンドウ
    //[SerializeField] private GameObject _iconNextTapObject; // タップを促す画像

    //private int _messageListIndex = 0;  // 表示メッセージの配列番号
    //private int _wordCount = 0;         // 1メッセージ当たりの文字の総数
    //private bool _isTapped = false;     // 全文表示後にタップを待つフラグ
    //private bool _isDisplayedAllMessage = false;    // 全メッセージ表示完了のフラグ

    //private IEnumerator _waitCoroutine; // 全文表示までの待機時間メソッド代入用 Stop出来るようにしておく
    //private Tween _tween;               // DoTween再生用  Kill出来るように代入して使用する

    //public bool auto;           // メッセージオート進行フラグ
    //private bool _startFlag;    // メッセージ開始フラグ
    //private bool _endFlag;      // メッセージ終了フラグ

    //public Sprite charaIcon;        // キャラクターアイコン
    //private string _characterName;  // キャラクター名

    //[SerializeField] private GameObject _mask;  // アイコン表示用
    //public float plusTime;      // ウィンドウ表示時間の追加

    //// Start is called before the first frame update
    //void Start() {
    //    if (messageList.Count == 0)
    //        messageEnd();

    //    //キャラクター名自動設定
    //    string[] a;  //一時保存用
    //    if (charaIcon)
    //        a = charaIcon.name.Split();  //キャラ名+状態 分割
    //    else {
    //        a = new string[1];
    //        a[0] = " ";
    //    }
    //    _characterName = a[0];   //キャラクター名だけ取得 設定

    //    //SE.playnum = 15;  //メッセージ表示中効果音 発生

    //    _messageListScript = MessageList.Instance;
    //}

    /////<summary>
    /////メッセージ開始処理
    /////</summary>
    //IEnumerator startWait()  //メッセージ開始処理
    //{
    //    yield return new WaitForSeconds(0.1f);  //一瞬待機
    //    _messageListIndex = 0;  //メッセージ番号初期化
    //    _startFlag = true;  //メッセージ開始フラグ設定
    //    _textWindow.SetActive(true);  //ウィンドウ表示実行

    //    if (charaIcon)  //キャラクターアイコンが設定されているとき
    //    {
    //        _mask.transform.GetChild(0).gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.7f);  //アイコン表示
    //        _mask.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = charaIcon;  //画像設定
    //    } else  //アイコンが設定されていない時
    //      {
    //        _mask.transform.GetChild(0).gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0);  //アイコン非表示
    //    }

    //    if (!_characterName.Equals("")) {
    //        _mask.transform.GetChild(1).gameObject.GetComponent<Text>().text = _characterName;  //キャラクター名表示設定
    //    }
    //    StartCoroutine(DisplayMessage());  //メッセージ表示処理
    //}

    /////<summary>
    /////一番上のメッセージまで遡って非表示にする処理
    /////</summary>
    //public void messageEnd() {
    //    if (transform.parent.gameObject.GetComponent<MessageViewer>()) {
    //        transform.parent.gameObject.GetComponent<MessageViewer>().messageEnd();  //親オブジェクトがViewerを持っていれば参照する
    //    } else if (transform.parent.gameObject.GetComponent<MessageList>())  //メッセージリストまで遡った時
    //      {
    //        transform.parent.gameObject.SetActive(false);  //メッセージを非表示にする
    //        _messageListScript.isMessageViewing = false;
    //    }
    //}

    //// Update is called once per frame
    //void Update() {
    //    //Debug.Log(gameObject.name);
    //    //強制終了処理
    //    if (_messageListScript.isMessageViewing == false) {
    //        messageEnd();
    //    }

    //    if (!_startFlag && !_endFlag)  //メッセージ開始前
    //    {
    //        if (!_textWindow)  //バグ防止
    //            return;
    //        if (_textWindow.activeSelf == true)  //メッセージウィンドウが非表示になるまで待機
    //        {
    //            return;
    //        }
    //        StartCoroutine("startWait");  //メッセージ開始処理
    //    }

    //    if (_isDisplayedAllMessage)  //メッセージ表示終了後
    //    {
    //        _isDisplayedAllMessage = false;  //一度のみ実行
    //        _endFlag = true;  //表示終了フラグ
    //        _textWindow.SetActive(false);  //ウィンドウ非表示

    //        if (transform.childCount == 0)  //次のメッセージが無い場合
    //        {
    //            messageEnd();  //全メッセージ非表示処理
    //        } else  //次のメッセージがある場合
    //          {
    //            transform.GetChild(0).gameObject.SetActive(true);  //次のメッセージをアクティブ化
    //        }
    //        return;
    //    }

    //    if (!auto && Input.GetKeyDown(KeyCode.Z) && _tween != null)  //タップ処理（オートでない場合）
    //    {
    //        //文字送り中にタップした場合、文字送りを停止
    //        _tween.Kill();
    //        _tween = null;

    //        //文字送りのための待機時間も停止
    //        if (_waitCoroutine != null) {
    //            StopCoroutine(_waitCoroutine);
    //            _waitCoroutine = null;
    //        }

    //        _messageText.text = messageList[_messageListIndex];  //全文まとめて表示

    //        StartCoroutine(NextTouch());  //タップするまで全文を表示したまま待機
    //    }

    //    if (!auto && Input.GetKeyDown(KeyCode.Z) && _wordCount == messageList[_messageListIndex].Length)  //全文表示中にタップ
    //    {
    //        _isTapped = true;  //全文表示を終了
    //    }
    //}

    /////<summary>
    /////1文字ずつメッセージ表示実行
    /////</summary>
    //private IEnumerator DisplayMessage() {
    //    _isTapped = false;  //タップ待ちフラグ初期化
    //                       //表示テキストとTweenをリセット
    //    _messageText.text = "";
    //    _tween = null;
    //    if (_waitCoroutine != null)  //文字送りの待機時間を初期化
    //    {
    //        StopCoroutine(_waitCoroutine);  //Coroutineを止めて初期化
    //        _waitCoroutine = null;
    //    }

    //    //1文字ずつの文字送り表示が終了するまでループ
    //    while (messageList[_messageListIndex].Length > _wordCount) {
    //        //wordSpeed秒ごとに文字を1文字ずつ表示。SetEase(Ease.Linear)をセットすることで一定の時間間隔で表示
    //        _tween = _messageText.DOText(messageList[_messageListIndex], messageList[_messageListIndex].Length * _wordSpeed).
    //            SetEase(Ease.Linear).OnComplete(() => {
    //                //Debug.Log("全文表示完了");
    //            });

    //        //文字送り表示が終了するまでの待機時間を設定して待機を実行
    //        _waitCoroutine = WaitTime();
    //        yield return StartCoroutine(_waitCoroutine);
    //    }
    //    if (!auto)  //オート進行でない場合
    //    {
    //        //タップするまで全文を表示したまま待機
    //        StartCoroutine(NextTouch());
    //    }
    //}

    /////<summary>
    /////メッセージ表示時間設定
    /////</summary>
    //private IEnumerator WaitTime() {
    //    yield return new WaitForSeconds(messageList[_messageListIndex].Length * _wordSpeed);  //文字数×表示速度 の待機時間    (タップした場合は停止)
    //    _wordCount = messageList[_messageListIndex].Length;  //文字数取得

    //    if (auto)  //オート進行の場合   アイコンを徐々に消して残り時間を表示する処理
    //    {
    //        _mask.transform.parent.gameObject.transform.GetChild(2).gameObject.GetComponent<Image>().fillAmount = 1;  //アイコン表示初期化

    //        float messageTime = _wordSpeed * messageList[_messageListIndex].Length + 3.5f + plusTime; //メッセージ表示時間   （全文表示後3.5fの猶予と、plustimeでの調整）

    //        DOTween.To  //残り表示時間に応じてアイコンを消す
    //        (
    //       () => _mask.transform.parent.gameObject.transform.GetChild(2).gameObject.GetComponent<Image>().fillAmount,       //何に
    //       x => _mask.transform.parent.gameObject.transform.GetChild(2).gameObject.GetComponent<Image>().fillAmount = x,  //何を
    //       0,     //どこまで(最終的な値)
    //       messageTime//どれくらいの時間
    //        );
    //        yield return new WaitForSeconds(messageTime);   //アイコンが消えるまで待機

    //        _isTapped = true;  //タップされたことを自動設定

    //        if (_messageListIndex + 1 == messageList.Count)  //メッセージリストが全て表示されたとき
    //        {
    //            _textWindow.SetActive(false);  //ウィンドウ非表示
    //        }
    //        StartCoroutine(NextTouch());
    //    }
    //}

    /////<summary>
    /////タップするまで全文を表示したまま待機
    /////</summary>
    //private IEnumerator NextTouch() {
    //    yield return null;
    //    //表示した文字の総数を更新
    //    _wordCount = messageList[_messageListIndex].Length;

    //    //タップを待つ
    //    yield return new WaitUntil(() => _isTapped);

    //    //次のメッセージへ移行
    //    _messageListIndex++;
    //    _wordCount = 0;

    //    //リストに未表示のメッセージが残っている場合
    //    if (_messageListIndex < messageList.Count) {
    //        //SE.playnum = 15;    //メッセージ表示効果音  
    //        StartCoroutine(DisplayMessage());  //1文字ずつ表示する処理をスタート
    //    } else {
    //        //全メッセージ表示終了
    //        _isDisplayedAllMessage = true;

    //        //次の処理へ
    //    }
    //}
}
