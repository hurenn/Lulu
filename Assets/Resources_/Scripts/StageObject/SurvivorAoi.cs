using UnityEngine;
using UnityEngine.Playables;

// イベントキャラ「アオイ」の救出。触れたらイベントTimelineを流し、終了後に救出カウントして同行フラグを立てる
// （同行フラグが立っている間にAoiGoalTriggerへたどり着くと、Water付与イベントが発生する）
public class SurvivorAoi : Survivor {
    [SerializeField] private PlayableDirector _eventTimeline; // 触れた時に流すTimeline（未設定の場合はイベント無しで即救出）

    private bool _isEventPlaying = false;

    // アオイ救出時は救出メッセージを出さない（メッセージの順番にも数えない）
    protected override bool _ShowsRescueMessage => false;

    protected override void _HitPlayer(Player_Character player) {
        if (_isRescued || _isEventPlaying) return;

        // イベント無し
        if (_eventTimeline == null) {
            _OnEventEnd(player);
            return;
        }

        _isEventPlaying = true;
        _eventTimeline.stopped += _OnEventTimelineStopped;
        _eventTimeline.time = 0;
        _eventTimeline.Evaluate();
        _eventTimeline.Play();

        void _OnEventTimelineStopped(PlayableDirector director) {
            director.stopped -= _OnEventTimelineStopped;
            // シーン破棄に伴う停止では救出扱いにしない
            if (this == null || !gameObject.scene.isLoaded) return;
            _isEventPlaying = false;
            _OnEventEnd(player);
        }
    }

    /// <summary>
    /// イベント終了：同行フラグを立てて救出カウントする
    /// </summary>
    private void _OnEventEnd(Player_Character player) {
        CheckpointManager.Instance.isAoiAccompanying = true;
        _Rescue(player);
    }
}
