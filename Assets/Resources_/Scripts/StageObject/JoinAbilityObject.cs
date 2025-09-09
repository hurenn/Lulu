using UnityEngine;

/// <summary>
/// 仲間の能力を取得するオブジェクト
/// </summary>
public class JoinAbilityObject : StageObject_Base
{
    /// <summary>
    /// 取得する能力の種類
    /// </summary>
    [SerializeField] private eAbilityType _abilityType = eAbilityType.None;

    protected override void _HitPlayer(Player_Character player)
    {
        base._HitPlayer(player);
        if (_abilityType == eAbilityType.None) {
            Debug.LogError("取得能力不明");
            return;
        }
        if (player == null)
        {
            Debug.LogError("プレイヤー不明");
            return;
        }

        // 設定先のスロットを決定
        eAbilitySlot ability_slot = _abilityType switch
        {
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
