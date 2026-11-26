using UnityEngine;

/// <summary>
/// メインメニューの「はなす」。登録した会話の中からランダムに1つを選び、メッセージウィンドウで再生する。
/// 会話はボタンで送り(waitForButton)、最後まで読んだらメインメニューへ戻る。
/// 直前と同じ会話は続けて選ばない(会話が2つ以上ある時)
/// </summary>
public class RandomTalkScreen : MainMenuSubScreen {
    [System.Serializable]
    private class Talk {
        public MessageData[] messages;
    }

    [SerializeField] private Talk[] _talks;
    [SerializeField] private MessageList _messageList;
    [SerializeField] private MessageViewer _messageViewer;

    private System.Action _onCancel;
    private bool _isPlaying;
    private bool _hasStarted;        // 1行目が表示されたか(表示前に「終わった」と判定しないため)
    private int _lastTalkIndex = -1;

    public override void Open(System.Action onCancel) {
        _onCancel = onCancel;
        gameObject.SetActive(true);
        if (_messageList == null) _messageList = FindAnyObjectByType<MessageList>();
        if (_messageViewer == null) _messageViewer = FindAnyObjectByType<MessageViewer>(FindObjectsInactive.Include);
        if (_talks == null || _talks.Length == 0 || _messageList == null || _messageViewer == null) {
            Debug.LogWarning("[RandomTalk] 会話かメッセージ表示が設定されていません");
            _Finish();
            return;
        }

        // メインメニューではメッセージウィンドウを普段隠しているので、会話の間だけ出す
        _messageViewer.gameObject.SetActive(true);

        int index = Random.Range(0, _talks.Length);
        if (_talks.Length > 1 && index == _lastTalkIndex) {
            index = (index + Random.Range(1, _talks.Length)) % _talks.Length;  // 直前と同じなら別の会話にする
        }
        _lastTalkIndex = index;

        _messageList.Clear();
        foreach (var message in _talks[index].messages) {
            message.waitForButton = true;  // 自動で流さず、ボタンで送る
            message.isConversationStyle = true;  // 左右にアイコンを出す会話形式で表示する
            _messageList.Enqueue(message);
        }
        _isPlaying = true;
        _hasStarted = false;
    }

    public override void Close() {
        _isPlaying = false;
        gameObject.SetActive(false);
    }

    private void Update() {
        if (!_isPlaying) return;
        if (_messageViewer.IsShowing) {
            _hasStarted = true;
            return;
        }
        // 最後の行まで読み終わった
        if (_hasStarted && !_messageList.HasMessages()) {
            _Finish();
        }
    }

    private void _Finish() {
        Close();
        _onCancel?.Invoke();
    }
}
