using UnityEngine;

public class Ability_Warp : Ability_Base
{
    private Player_Character _player;

    private Player_Character Player {
        get {
            if (_player == null && _playerTransform != null) {
                _player = _playerTransform.GetComponent<Player_Character>();
            }
            return _player;
        }
    }

    public override eAbilityResult ExecuteSimple()
    {
        return eAbilityResult.Jump;
    }

    public override eAbilityResult ExecuteRelease()
    {
        return eAbilityResult.JumpRelease;
    }
}
