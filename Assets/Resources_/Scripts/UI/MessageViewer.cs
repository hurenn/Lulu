using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// メッセージ表示クラス
/// </summary>
public class MessageViewer : MonoBehaviour {
    private const float _BASE_SHOW_TIME = 2.0f; // 基本表示時間
    private const float _AUTO_MESSAGE_SHOW_TIME = 0.05f; // 1文字あたりの追加表示時間
    private const float _EVENT_MESSAGE_SHOW_TIME = 0.01f; // 1文字あたりの追加表示時間
    private const float _COOL_TIME = 0.5f;  // メッセージ表示クールタイム
    private const float _FORCE_COOL_TIME = 0.1f; // 強制メッセージ表示クールタイム

    [SerializeField] private PlayerController _playerController; // プレイヤーコントローラー
    [SerializeField] private MessageList _messageListScript;    // メッセージリスト管理
    [SerializeField] private TMP_Text _messageText;                 // メッセージ表示用テキスト
    [SerializeField] private TMP_Text _characterText;               // キャラクター名表示用テキスト
    [SerializeField] private Image _iconImage;                  // キャラクターアイコン表示用イメージ
    [SerializeField] private GameObject _messagePanel;          // メッセージパネル
    [SerializeField] private Image _nextIcon;                   // 次のメッセージを促すアイコン

    private MessageData _currentMessage;  // 現在表示中のメッセージ
    private float _currentShowTime; // 現在の表示時間
    private bool _isShowing;        // メッセージ表示中フラグ
    public bool IsShowing => _isShowing; // メッセージ表示中フラグの公開用
    private bool _isSeries;         // 一連のメッセージフラグ
    private bool _isEventMessage;    // イベントメッセージフラグ
    private float _currentCoolTime; // 次のメッセージを表示するまでのクールタイム

    private void OnEnable() {
        _messagePanel.SetActive(false); // パネルを非表示
        if(_playerController == null) {
            _playerController = FindAnyObjectByType<PlayerController>();
        }
    }

    private void Update() {
        if (_currentCoolTime > 0) {
            _currentCoolTime -= Time.deltaTime;
            return;
        }

        if (!_isShowing && _isEventMessage && !_messageListScript.HasMessages()) {
            // イベントメッセージが終わったらキャラクター操作を有効化
            _playerController.isEnabledCharacterInput = true;
            _isEventMessage = false;
        }

        // 次のメッセージを表示
        if (!_isShowing && _messageListScript.HasMessages()) {
            _ShowNext();
        }
        if (!_isShowing) return;

        if (_currentMessage.isEventMessage) {
            // イベントメッセージの場合、ユーザー入力待ち
            if (_isShowing && _playerController.Input.messageNextPressed) {
                _HideOrNext();
            }
        } else if (_currentShowTime > 0) {
            // メッセージ表示の残り時間表示
            _currentShowTime -= Time.deltaTime;
            _nextIcon.fillAmount = _currentShowTime / (_BASE_SHOW_TIME + (_currentMessage.text.Length * _AUTO_MESSAGE_SHOW_TIME));
            if (_currentShowTime <= 0f) {
                // 表示時間終了
                _HideOrNext();
            }
        }
    }

    private void _ShowNext() {
        _messagePanel.SetActive(true);                  // パネルを表示

        _currentMessage = _messageListScript.Dequeue(); // 次のメッセージを取得
        if (_currentMessage.isEventMessage) {
            _playerController.isEnabledCharacterInput = false; // キャラクター操作無効化
            _isEventMessage = true;
            // メッセージを一気に表示
            StartCoroutine(_TypeText(_currentMessage.text, _EVENT_MESSAGE_SHOW_TIME));
        } else {
            // メッセージを1文字ずつ表示するコルーチン開始
            StartCoroutine(_TypeText(_currentMessage.text, _AUTO_MESSAGE_SHOW_TIME));
        }
        _characterText.text = _currentMessage.characterName; // キャラクター名をセット
        _iconImage.sprite = _currentMessage.characterIcon;   // キャラクターアイコンをセット
        _isSeries = _messageListScript.HasMessages();   // 次のメッセージがあるかどうか
        _nextIcon.fillAmount = 1.0f; // 次のメッセージアイコンをリセット

        _currentShowTime = _BASE_SHOW_TIME + (_currentMessage.text.Length * _AUTO_MESSAGE_SHOW_TIME); // 基本3秒 + 文字数に応じた追加時間
        _isShowing = true;
    }

    // 1文字ずつ表示する場合のコルーチン
    private IEnumerator _TypeText(string message, float message_show_time) {
        _messageText.text = "";
        foreach (char c in message) {
            _messageText.text += c;
            yield return new WaitForSeconds(message_show_time); // 文字表示速度
        }
    }

    private void _HideOrNext() {
        _isShowing = false;

        // 一連のメッセージ表示中で無ければ一旦パネルを消す
        if (!_isSeries) {
            _messagePanel.SetActive(false); // パネルを非表示
            _currentCoolTime = _COOL_TIME;  // クールタイム設定
        }
    }

    public void ForceReset() {
        // 強制メッセージが来たら即座に表示をリセット
        _messagePanel.SetActive(false);
        _isShowing = false;
        _currentCoolTime = _FORCE_COOL_TIME;
    }
}
