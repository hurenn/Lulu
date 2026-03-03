using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メッセージ表示クラス
/// </summary>
public class MessageViewer : MonoBehaviour {
    // キャラクター名の辞書
    private Dictionary<string, string> _CHARACTER_NAME_DIC = new Dictionary<string, string>() {
        {"Lulu", "ルル"},
        {"Marlica", "マルリカ"},
        {"Node", "ノード"},
        {"Pepe", "ペペ"},
        {"Milly", "ミリー"},
    };
    private const float _BASE_SHOW_TIME = 2.0f; // 基本表示時間
    private const float _ADD_SHOW_TIME = 0.1f;  // 1文字あたりの追加表示時間
    private const float _ADD_ENG_SHOW_TIME = 0.05f;  // 1文字あたりの追加表示時間
    private const float _AUTO_MESSAGE_SHOW_TIME = 0.01f; // メッセージ表示の早さ
    private const float _AUTO_ENG_MESSAGE_SHOW_TIME = 0.001f; // 英語メッセージ表示の早さ
    private const float _COOL_TIME = 0.5f;  // メッセージ表示クールタイム
    private const float _FORCE_COOL_TIME = 0.1f; // 強制メッセージ表示クールタイム

    [SerializeField] private PlayerController _playerController; // プレイヤーコントローラー
    [SerializeField] private MessageList _messageListScript;    // メッセージリスト管理
    [SerializeField] private TMP_Text _messageText;             // メッセージ表示用テキスト
    [SerializeField] private GameObject _namePanel;             // キャラクター名パネル
    [SerializeField] private TMP_Text _characterText;           // キャラクター名表示用テキスト
    [SerializeField] private Image _iconImage;                  // キャラクターアイコン表示用イメージ
    [SerializeField] private GameObject _messagePanel;          // メッセージパネル
    [SerializeField] private Image _nextIcon;                   // 次のメッセージを促すアイコン
    [SerializeField] private Image _nextButtonIcon;             // 次のメッセージを促すボタンアイコン
    [SerializeField] private Image[] _messageWindows;         // メッセージパネルのCanvasGroup
    [SerializeField] private float _fadeAlpha = 0.3f;      // フェード時の透明度
    [SerializeField] private float _normalAlpha = 0.9f;    // 通常時の透明度
    private RectTransform _messagePanelRect; // メッセージパネルのRectTransform
    private RectTransform _iconRect;         // キャラクターアイコンのRectTransform

    private string _currentText = string.Empty;
    private MessageData _currentMessage;  // 現在表示中のメッセージ
    private float _currentShowTime; // 現在の表示時間
    private bool _isShowing;        // メッセージ表示中フラグ
    public bool IsShowing => _isShowing; // メッセージ表示中フラグの公開用
    private bool _isSeries;         // 一連のメッセージフラグ
    private bool _isEventMessage;    // イベントメッセージフラグ
    private float _currentCoolTime; // 次のメッセージを表示するまでのクールタイム
    private IEnumerator _typingCoroutine; // 文字を1つずつ表示するコルーチン

    private bool _isStopMessage = false;    // メッセージ表示停止フラグ
    public void SetIsStopMessage(bool enable) {
        _isStopMessage = enable;
    }

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSpeak;
    [SerializeField] private AudioClip _seSpeakOne;

    private PlayerParameter _playerParameter;

    private void OnEnable() {
        _messagePanel.SetActive(false); // パネルを非表示
        if(_playerController == null) {
            _playerController = FindAnyObjectByType<PlayerController>();
        }
        _playerParameter = PlayerParameter.Instance;
    }

    private void Update() {
        UpdateWindowAlpha();
        // ポーズUIが開いている間はメッセージ表示時間経過を止める
        if (Pause_UI.IsOpen == true) {
            return;
        }
        if (_isStopMessage) {
            return;
        }

        if (_currentCoolTime > 0) {
            _currentCoolTime -= Time.deltaTime;
            return;
        }

        if (!_isShowing && _isEventMessage && !_messageListScript.HasMessages()) {
            _isEventMessage = false;
        }

        // 次のメッセージを表示
        if (!_isShowing && _messageListScript.HasMessages()) {
            _ShowNext();
        }
        if (!_isShowing) return;

        // ボタン表示切替
        var is_event_message = _currentMessage.playableDirector != null && !_currentMessage.isAutoForce;
        _nextButtonIcon.gameObject.SetActive(is_event_message);
        _nextIcon.gameObject.SetActive(!is_event_message);

        if (is_event_message) {
            // イベントメッセージの場合、ユーザー入力待ち
            if (_isShowing && _playerController.Input.messageNextPressed) {
                _HideOrNext();
            }
        } else if (_currentShowTime > 0) {
            // メッセージ表示の残り時間表示
            _currentShowTime -= Time.deltaTime;
            _nextIcon.fillAmount = _currentShowTime / (_BASE_SHOW_TIME + (_currentText.Length *
                (_playerParameter.language == PlayerParameter.eLanguage.English ? _ADD_ENG_SHOW_TIME : _ADD_SHOW_TIME)));
            if (_currentShowTime <= 0f) {
                // 表示時間終了
                _HideOrNext();
            }
        }
    }

    private void _ShowNext() {
        _messagePanel.SetActive(true);                  // パネルを表示
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);            // 表示中のコルーチンを停止

        _currentMessage = _messageListScript.Dequeue(); // 次のメッセージを取得
        _currentText = _playerParameter.language == PlayerParameter.eLanguage.English ? _currentMessage.englishText : _currentMessage.text;
        if (_currentMessage.playableDirector != null && !_currentMessage.isAutoForce) {
            //_currentMessage.playableDirector.Pause(); // Timelineを一時停止

            _isEventMessage = true;
            // メッセージを一気に表示
            _typingCoroutine = _TypeText(_currentText, 0);
            StartCoroutine(_typingCoroutine);
        } else {
            // メッセージを1文字ずつ表示するコルーチン開始
            _typingCoroutine = _TypeText(_currentText, _playerParameter.language == PlayerParameter.eLanguage.Japanese ? _AUTO_MESSAGE_SHOW_TIME : _AUTO_ENG_MESSAGE_SHOW_TIME);
            StartCoroutine(_typingCoroutine);
        }

        // キャラクター名とアイコンの設定
        Sprite chara_icon = _currentMessage.characterIcon;
        var character_name = chara_icon ? chara_icon.name : string.Empty;

        // 最初のアンダーラインまでをキャラクター名として辞書から取得
        character_name = character_name.Split('_')[0];

        // キャラクター名をセット
        if(_playerParameter.language == PlayerParameter.eLanguage.English) {
            _characterText.text = character_name;
            _namePanel.SetActive(!string.IsNullOrEmpty(character_name));
        } else {
            if (_CHARACTER_NAME_DIC.ContainsKey(character_name)) {
                _characterText.text = _CHARACTER_NAME_DIC[character_name];
                _namePanel?.SetActive(true); // 名前パネルを表示
            } else {
                _characterText.text = string.Empty;
                _namePanel?.SetActive(false); // 名前パネルを非表示
            }
        }

        _iconImage.sprite = chara_icon;   // キャラクターアイコンをセット
        _isSeries = _messageListScript.HasMessages();   // 次のメッセージがあるかどうか

        _currentShowTime = _BASE_SHOW_TIME + (_currentText.Length *
            (_playerParameter.language == PlayerParameter.eLanguage.English ? _ADD_ENG_SHOW_TIME : _ADD_SHOW_TIME))
            + _currentMessage.addShowTime; // 基本3秒 + 文字数に応じた追加時間 + メッセージ固有の追加時間

        if (_currentShowTime < 0) {
            _nextIcon.gameObject.SetActive(false); // 次のメッセージアイコンを非表示
        } else {
            _nextIcon.gameObject.SetActive(true);  // 次のメッセージアイコンを表示
            _nextIcon.fillAmount = 1.0f; // 次のメッセージアイコンをリセット
        }

        _isShowing = true;
    }

    // 1文字ずつ表示する場合のコルーチン
    private IEnumerator _TypeText(string message, float message_show_time) {
        _audioSource.Stop();
        _messageText.text = "";
        _audioSource.PlayOneShot(_seSpeak);
        if (message_show_time <= 0) { // 一気に表示
            var view_message = message.Length / 3;
            _messageText.text = message.Substring(0, view_message);
            yield return new WaitForSecondsRealtime(0.01f);
            _messageText.text = message.Substring(0, view_message * 2);
            yield return new WaitForSecondsRealtime(0.01f);
            _messageText.text = message;
        } else {
            foreach (char c in message) { // 1文字ずつ表示
                if (_playerParameter.language == PlayerParameter.eLanguage.Japanese ||
                    (_playerParameter.language == PlayerParameter.eLanguage.English && _messageText.text.Length % 2 == 0)) {
                }
                _messageText.text += c;
                yield return new WaitForSecondsRealtime(message_show_time);
                while (_isStopMessage) {
                    yield return null;
                }
            }
        }
        _typingCoroutine = null;
    }

    private void _HideOrNext() {
        _isShowing = false;

        // 一連のメッセージ表示中で無ければ一旦パネルを消す
        if (!_isSeries) {
            if(_currentMessage.playableDirector != null) {
                _currentMessage.playableDirector.Resume(); // Timelineを再開
            }
            _messagePanel.SetActive(false); // パネルを非表示
            _currentCoolTime = _COOL_TIME;  // クールタイム設定
        }
    }

    public void ForceReset() {
        // 強制メッセージが来たら即座に表示をリセット
        _messagePanel.SetActive(false);
        _isShowing = false;
        if(_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);
        _currentCoolTime = _FORCE_COOL_TIME;
    }

    private void UpdateWindowAlpha() {
        if (_IsIconOverlapping()) {
            // プレイヤーがメッセージウィンドウと重なっている場合、透明度を下げる
            foreach (var window in _messageWindows) {
                if (window == null) continue;
                var color = window.color;
                color.a = Mathf.Lerp(color.a, _fadeAlpha, Time.deltaTime * 5f);
                window.color = color;
            }
        } else {
            // 通常の透明度に戻す
            foreach (var window in _messageWindows) {
                if (window == null) continue;
                var color = window.color;
                color.a = Mathf.Lerp(color.a, _normalAlpha, Time.deltaTime * 5f);
                window.color = color;
            }
        }
    }

    Camera mainCamera => Camera.main;
    Character_Base player => _playerController.Character;
    private bool _IsIconOverlapping() {
        // プレイヤーがメッセージウィンドウと重なっているかどうかを判定
        if (player == null) return false;
        var playerPos = mainCamera.WorldToScreenPoint(player.transform.position);

        // RectTransformの取得
        if (_iconRect == null) {
            _iconRect = _iconImage.GetComponent<RectTransform>();
        }
        if (_messagePanelRect == null) {
            _messagePanelRect = _messagePanel.GetComponent<RectTransform>();
        }

        var is_icon_overlap = RectTransformUtility.RectangleContainsScreenPoint(_iconRect, playerPos, null);
        var is_window_overlap = RectTransformUtility.RectangleContainsScreenPoint(_messagePanelRect, playerPos, null);

        return is_icon_overlap || is_window_overlap;
    }
}
