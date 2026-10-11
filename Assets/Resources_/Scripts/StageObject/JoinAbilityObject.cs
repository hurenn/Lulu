using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 仲間の能力を取得するオブジェクト
/// </summary>
public class JoinAbilityObject : StageObject_Base {
    /// <summary>
    /// 取得する能力の種類
    /// </summary>
    [SerializeField] private eAbilityType _abilityType = eAbilityType.None;
    /// <summary>
    /// 仲間加入イベント
    /// </summary>
    [SerializeField] private PlayableDirector playableDirector = null;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);

        // イベント無し
        if (playableDirector == null) {
            _JoinAbility(player);
            return;
        }

        playableDirector.time = 0;
        playableDirector.Play();
        playableDirector.stopped += (pd) => {
            // イベント終了時に仲間加入
            _JoinAbility(player);
        };
    }

    /// <summary>
    /// 仲間加入
    /// </summary>
    protected virtual void _JoinAbility(Player_Character player) {
        if (_abilityType == eAbilityType.None) {
            Debug.LogError("取得能力不明");
            return;
        }
        if (player == null) {
            Debug.LogError("プレイヤー不明");
            return;
        }

        // 設定先のスロットを決定（事前に入れ替えられていた場合はそちらが優先される）
        eAbilitySlot ability_slot = PlayerParameter.Instance.GetAssignedSlot(_abilityType);

        // ボタン未割り当ての能力（Water）は、コンフィグ上でFireが割り当てられているボタンに付与する
        if (ability_slot == eAbilitySlot.None) {
            ability_slot = _ReplaceFireSlot(player);
        }

        // 正式に取得したので、デバッグ付与扱い（シーン再読み込み時の解除対象）から外す
        if (_abilityType == eAbilityType.Water) {
            PlayerParameter.Instance.isWaterDebugEquipped = false;
        }

        // プレイヤーに能力を追加
        player.SetAbilitySlot(_abilityType, ability_slot);

        // オブジェクトを削除
        Destroy(gameObject);
    }

    /// <summary>
    /// Fireが割り当てられているボタンを取得能力に明け渡す（装備中のFireは外す）
    /// </summary>
    private eAbilitySlot _ReplaceFireSlot(Player_Character player) {
        var player_param = PlayerParameter.Instance;
        eAbilitySlot fire_slot = player_param.GetAssignedSlot(eAbilityType.Fire);
        if (fire_slot == eAbilitySlot.None) {
            // Fireが既にボタンから外れている場合は通常のAボタンに付与する
            fire_slot = eAbilitySlot.A;
        }

        // そのボタンに装備されている能力をボタンから外し、シーン上のAbilityも削除する
        eAbilityType current_type = player_param.GetAssignedAbilityType(fire_slot);
        if (current_type != eAbilityType.None) {
            if (player.GetCurrentSlot(current_type) == fire_slot) {
                player.RemoveAbility(fire_slot);
            }
            player_param.SetAssignedSlot(current_type, eAbilitySlot.None);
        }
        player_param.SetAssignedSlot(_abilityType, fire_slot);
        return fire_slot;
    }
}