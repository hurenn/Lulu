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
    private void _JoinAbility(Player_Character player) {
        if (_abilityType == eAbilityType.None) {
            Debug.LogError("取得能力不明");
            return;
        }
        if (player == null) {
            Debug.LogError("プレイヤー不明");
            return;
        }

        // 設定先のスロットを決定
        eAbilitySlot ability_slot = _abilityType switch {
            eAbilityType.Ice => eAbilitySlot.Y,
            eAbilityType.Fire => eAbilitySlot.A,
            eAbilityType.Light => eAbilitySlot.X,
            _ => throw new System.ArgumentOutOfRangeException()
        };

        // プレイヤーに能力を追加
        player.SetAbilitySlot(_abilityType, ability_slot);

        // オブジェクトを削除
        Destroy(gameObject);
    }
}