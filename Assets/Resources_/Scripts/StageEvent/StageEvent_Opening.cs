using UnityEngine;

// Note: ステージ1-0にて未使用
public class StageEvent_Opening : StageObject_Base
{
    [SerializeField] private PlayerController _controller;
    [SerializeField] private MessageViewer _messageViewer;
    [SerializeField] private MessageList _messageList;

    private bool _isTriggered = false;

    protected override void _HitPlayer(Player_Character player)
    {
        if(_isTriggered) return;

        if (_controller == null) {
            _controller = FindAnyObjectByType<PlayerController>();
        }
        if(_messageViewer == null) {
            _messageViewer = FindAnyObjectByType<MessageViewer>();
        }
        if(_messageList == null) {
            _messageList = FindAnyObjectByType<MessageList>();
        }

        _controller.isEnabledCharacterInput = false;
        _isTriggered = true;
    }

    private void Update() {
        if(_isTriggered && !_messageViewer.IsShowing && !_messageList.HasMessages()) {
            _controller.isEnabledCharacterInput = true;
            this.enabled = false;
        }
    }
}
