using TMPro;
using UnityEngine;

// 風船を制限時間内に取りに行くサブクエストの管理
public class SubQuest_Balloon : MonoBehaviour {
    [SerializeField] private float _timeLimit = 20f; // 制限時間(秒、可変)
    [SerializeField] private TMP_Text _timerText;      // 残り時間表示用UI

    [Header("残り時間による色変化")]
    [SerializeField] private float _warningTime = 10f; // この秒数以下でオレンジ色にする
    [SerializeField] private float _dangerTime = 5f;   // この秒数以下で薄い赤色にする
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = new Color(1f, 0.55f, 0f);   // オレンジ
    [SerializeField] private Color _dangerColor = new Color(1f, 0.4f, 0.4f);   // 薄い赤

    [SerializeField] private MessageList _messageListScript;
    [SerializeField] private MessageViewer _messageViewer;
    [SerializeField] private MessageData[] _clearMessages;   // 目標到達時に表示するメッセージ
    [SerializeField] private MessageData[] _timeUpMessages;  // 時間切れ時に表示するメッセージ
    [SerializeField] private TalkTrigger _addFlagTarget;     // 設定されていれば、クリア時にこのTriggerのフラグを増加させる

    private bool _isRunning = false;
    private bool _isCleared = false;
    private float _remainingTime;

    public bool IsRunning => _isRunning;
    public bool IsCleared => _isCleared;

    private void Awake() {
        if (_messageListScript == null) _messageListScript = FindAnyObjectByType<MessageList>();
        if (_messageViewer == null) _messageViewer = FindAnyObjectByType<MessageViewer>();

        if (_timerText != null) {
            _timerText.gameObject.SetActive(false);
        }
    }

    // Timelineから呼び出してサブクエストを開始する
    public void StartQuest() {
        if (_isRunning) return;

        _isRunning = true;
        _isCleared = false;
        _remainingTime = _timeLimit;

        if (_timerText != null) {
            _timerText.gameObject.SetActive(true);
        }
        _UpdateTimerText();
    }

    private void Update() {
        if (!_isRunning || Pause_UI.IsOpen) return;

        _remainingTime -= Time.deltaTime;
        if (_remainingTime <= 0f) {
            _remainingTime = 0f;
            _UpdateTimerText();
            _OnTimeUp();
            return;
        }
        _UpdateTimerText();
    }

    // 目標の風船に触れた際に呼び出す(SubQuestBalloonTargetから呼ばれる)
    public void OnBalloonReached() {
        if (!_isRunning) return;

        _isRunning = false;
        _isCleared = true;
        if (_timerText != null) {
            _timerText.gameObject.SetActive(false);
        }

        foreach (var message in _clearMessages) {
            _messageListScript.Enqueue(message);
        }

        _addFlagTarget?.AdvanceFlag();
    }

    private void _OnTimeUp() {
        _isRunning = false;
        if (_timerText != null) {
            _timerText.gameObject.SetActive(false);
        }

        // 次にステージがリトライされる(ポーズからのやり直し=シーン再読み込み)までプレイヤー操作を戻さない
        var playerController = PlayerCharacterManager.Controller;
        if (playerController != null) {
            playerController.isEnabledCharacterInput = false;
        }

        foreach (var message in _timeUpMessages) {
            _messageListScript.Enqueue(message);
        }
    }

    private void _UpdateTimerText() {
        if (_timerText == null) return;
        _timerText.text = "Time:" + _remainingTime.ToString("F3");

        if (_remainingTime <= _dangerTime) {
            _timerText.color = _dangerColor;
        } else if (_remainingTime <= _warningTime) {
            _timerText.color = _warningColor;
        } else {
            _timerText.color = _normalColor;
        }
    }
}
