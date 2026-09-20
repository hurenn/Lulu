using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

// 話しかけるたびに進む1回分のメッセージセット
[System.Serializable]
public class TalkMessageSet {
    public MessageData[] messageDatas;   // 直接入力するメッセージ(timelineが未設定の場合に使用)
    public PlayableDirector timeline;    // 設定した場合はメッセージの代わりにこのTimelineを再生する
}

// 判定内で上入力すると登録済みのメッセージセットを1つずつ再生する汎用会話イベント。
// メッセージセットは話しかけるたびに次へ進み、最後まで到達したら以後は最後のセットをループ再生する。
public class TalkTrigger : MonoBehaviour {
    [SerializeField] private TalkMessageSet[] _messageSets;
    [SerializeField] private MessageList _messageListScript;
    [SerializeField] private MessageViewer _messageViewer;

    [Header("会話開始の合図")]
    [SerializeField] private CanvasGroup _talkSignal;         // 上入力で会話開始できることを示すオブジェクト
    [SerializeField] private float _signalFadeDuration = 0.2f; // 表示/非表示時のフェード時間

    private PlayerController _playerController;
    private bool _isPlayerInside = false;
    private bool _wasUpHeld = false;
    private bool _isWaitingForMessageEnd = false;
    private bool _isWaitingForTimeline = false;
    private int _currentSetIndex = 0;
    private Coroutine _signalFadeCoroutine;

    private void Awake() {
        if (_talkSignal != null) {
            _talkSignal.alpha = 0f;
            _talkSignal.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.CompareTag("Player")) return;
        _isPlayerInside = true;
        if (_messageListScript == null) _messageListScript = FindAnyObjectByType<MessageList>();
        if (_messageViewer == null) _messageViewer = FindAnyObjectByType<MessageViewer>();
        _playerController = PlayerCharacterManager.Controller;

        if (!_isWaitingForMessageEnd && !_isWaitingForTimeline) _ShowSignal();
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (!collision.CompareTag("Player")) return;
        _isPlayerInside = false;
        _wasUpHeld = false;
        _HideSignal();
    }

    private void Update() {
        if (Pause_UI.IsOpen) return;

        // メッセージ終了待ち: 終わるまでプレイヤー操作を止めたまま監視する
        if (_isWaitingForMessageEnd) {
            if (!_messageViewer.IsShowing && !_messageListScript.HasMessages()) {
                _playerController.isEnabledCharacterInput = true;
                _isWaitingForMessageEnd = false;
                if (_isPlayerInside) _ShowSignal(); // 判定内に留まっていれば合図を出し直す
            }
            return;
        }
        if (_isWaitingForTimeline) return; // 完了はTimelineのstoppedイベントで処理する

        if (!_isPlayerInside || _playerController == null) return;
        if (_messageSets == null || _messageSets.Length == 0) return;

        bool isUpHeld = _playerController.Input.move.y > 0.5f;
        bool isUpPressed = isUpHeld && !_wasUpHeld;
        _wasUpHeld = isUpHeld;

        if (!isUpPressed) return;

        // 地面に付いていない、またはスライディング中は会話イベントを発生させない
        var character = _playerController.Character;
        if (character == null || !character.isGrounded || character._motorStates.isSliding) return;

        // アンビエントな自動メッセージ表示中なら強制終了し、会話イベントを優先して開始する
        if (_messageListScript.HasMessages() || _messageViewer.IsShowing) {
            _messageViewer.ForceReset();
        }

        _PlayCurrentSet();
    }

    private void _PlayCurrentSet() {
        _HideSignal(); // 会話開始と同時に合図を消す

        var set = _messageSets[_currentSetIndex];
        _playerController.isEnabledCharacterInput = false; // 終了まで操作を止める

        if (set.timeline != null) {
            // メッセージの代わりにTimelineを再生する
            set.timeline.stopped += _OnTimelineStopped;
            _isWaitingForTimeline = true;
            set.timeline.Play();
        } else {
            foreach (var message in set.messageDatas) {
                message.waitForButton = true; // 会話中はタイマー自動送りにせず手動送りにする
                _messageListScript.Enqueue(message);
            }
            _isWaitingForMessageEnd = true;
        }

        // 最後のセットに到達したらそれ以降は進めず、最後のセットをループ再生する
        if (_currentSetIndex < _messageSets.Length - 1) {
            _currentSetIndex++;
        }
    }

    private void _OnTimelineStopped(PlayableDirector director) {
        director.stopped -= _OnTimelineStopped;
        _playerController.isEnabledCharacterInput = true;
        _isWaitingForTimeline = false;
        if (_isPlayerInside) _ShowSignal(); // 判定内に留まっていれば合図を出し直す
    }

    private void _ShowSignal() {
        if (_talkSignal == null) return;
        _talkSignal.gameObject.SetActive(true);
        _StartSignalFade(1f);
    }

    private void _HideSignal() {
        if (_talkSignal == null || !_talkSignal.gameObject.activeSelf) return;
        _StartSignalFade(0f);
    }

    private void _StartSignalFade(float target) {
        if (_signalFadeCoroutine != null) StopCoroutine(_signalFadeCoroutine);
        _signalFadeCoroutine = StartCoroutine(_FadeSignalCo(target));
    }

    // 合図オブジェクトのアルファを一瞬だけフェードさせる
    private IEnumerator _FadeSignalCo(float target) {
        float start = _talkSignal.alpha;
        float elapsed = 0f;
        while (elapsed < _signalFadeDuration) {
            elapsed += Time.deltaTime;
            _talkSignal.alpha = Mathf.Lerp(start, target, elapsed / _signalFadeDuration);
            yield return null;
        }
        _talkSignal.alpha = target;
        if (target <= 0f) {
            _talkSignal.gameObject.SetActive(false);
        }
        _signalFadeCoroutine = null;
    }
}
