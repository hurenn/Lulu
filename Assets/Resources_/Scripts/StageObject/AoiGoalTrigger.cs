using UnityEngine;

// アオイ同行中にたどり着くとTimelineを流す目的地。
// Timeline終了後、同行フラグを解除し、能力（Water）を付与してチェックポイントを更新する
public class AoiGoalTrigger : JoinAbilityObject {
    private bool _isTriggered = false;

    protected override void _HitPlayer(Player_Character player) {
        if (_isTriggered || !CheckpointManager.Instance.isAoiAccompanying) return;
        _isTriggered = true;

        base._HitPlayer(player);
    }

    protected override void _JoinAbility(Player_Character player) {
        // シーン破棄に伴うTimeline停止では処理しない
        if (this == null || !gameObject.scene.isLoaded) return;

        base._JoinAbility(player);

        var checkpoint_manager = CheckpointManager.Instance;
        checkpoint_manager.isAoiAccompanying = false;

        // チェックポイントと同様の処理（リトライ時はこの場所から復帰。付与した能力も保存する）
        if (player != null) {
            checkpoint_manager.SaveCheckpoint(transform.position, player.GetParameterSnapshot());
            player.SaveAbilitySlot();
        }
    }
}
