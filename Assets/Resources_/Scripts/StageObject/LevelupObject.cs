using UnityEngine;

public class LevelupObject : StageObject_Base
{
    // レベルアップの種類
    public PlayerParameter.eLevelType levelType;

    protected override void _HitPlayer(Player_Character player)
    {
        Debug.Log($"Levelup:{player.gameObject.name}, Type:{levelType.ToString()}");
        player.Levelup(levelType);
    }
}
