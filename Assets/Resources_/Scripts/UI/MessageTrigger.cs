using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// メッセージを表示させるトリガークラス
/// </summary>
public class MessageTrigger : MonoBehaviour {
    [SerializeField] private MessageList _messageListScript; // メッセージリスト管理
    [SerializeField] private MessageSequenceAsset _messageSequence;  //メッセージデータアセット

    private MessageViewer _messageViewer; // メッセージビューア
    private PlayerController _playerController; // プレイヤーコントローラー
    private bool _isPlayerInside = false; // プレイヤーがトリガー内にいるかどうか

    // メッセージを追加する
    private IEnumerator _AddMessage() {
        if(_messageListScript == null) {
            Debug.LogError("メッセージリストが設定されていません。自動取得しますが後程設定してください");
            _messageListScript = FindAnyObjectByType<MessageList>();
        }
        if (_messageSequence == null) {
            Debug.LogError(gameObject.name + ": _messageSequenceが設定されていません。");
            yield break;
        }

        // メッセージ表示機能を探す (WIP)
        while (_messageViewer == null) {
            _messageViewer = FindAnyObjectByType<MessageViewer>();
            yield return null;
        }
        // プレイヤーコントローラーを探す (WIP)
        while(_playerController == null) {
            _playerController = PlayerCharacterManager.Controller;
            yield return null;
        }

        // メッセージ表示中は待機
        if (!_messageSequence.isForced) {
            while (_messageListScript.HasMessages() || _messageViewer.IsShowing) {// || !_playerController.isEnabledCharacterInput) {
                yield return null;

                if (!_isPlayerInside) {
                    // プレイヤーがトリガー外に出た場合、メッセージ追加を中止
                    yield break;
                }
            }
        }

        if (_messageSequence.isForced) {
            // 強制メッセージの場合、他のメッセージをクリア
            _messageListScript.Clear();
            _messageViewer.ForceReset();
        }

        // メッセージを追加
        foreach (var entry in _messageSequence.messages) {
            _messageListScript.Enqueue(new MessageData {
                text = entry.text,
                characterIcon = entry.characterIcon,
                addShowTime = entry.addShowTime,
                isAutoForce = entry.isAutoForce,
                isUnScaledTime = entry.isUnScaledTime,
            });
        }
        Destroy(gameObject);
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
}
