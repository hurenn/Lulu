using UnityEngine;

public class StageObject_AnimationPlayer : StageObject_Base {
    [SerializeField] private string _animationName;
    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        if (_animator != null && !string.IsNullOrEmpty(_animationName)) {
            _animator.Play(_animationName);
        }
    }
}
