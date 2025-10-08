using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineTrigger : StageObject_Base
{
    [SerializeField] private PlayableDirector playableDirector = null;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        if (playableDirector == null) {
            playableDirector = gameObject.AddComponent<PlayableDirector>();
        }

        playableDirector.time = 0;
        playableDirector.Play();
    }
}
