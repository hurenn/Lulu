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

        // Waterはスロットを持たない特殊状態：Fire能力の付与＋Water切り替えとして扱う
        // Fireの付与時はWater切り替えを解除し、Fireに戻す
        eAbilityType join_type = _abilityType;
        if (_abilityType == eAbilityType.Water) {
            player.SetWaterMode(true);
            join_type = eAbilityType.Fire;
        } else if (_abilityType == eAbilityType.Fire) {
            player.SetWaterMode(false);
        }

        // 未装備ならプレイヤーに能力を追加（装備済みの場合、Fire/Waterの差し替えはSetWaterModeで済んでいる）
        if (!player.GetCurrentSlot(join_type).HasValue) {
            // 設定先のスロットを決定（事前に入れ替えられていた場合はそちらが優先される）
            eAbilitySlot ability_slot = PlayerParameter.Instance.GetAssignedSlot(join_type);
            player.SetAbilitySlot(join_type, ability_slot);
        }

        // オブジェクトを削除
        Destroy(gameObject);
    }
}