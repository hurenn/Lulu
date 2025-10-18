using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineTrigger : StageObject_Base
{
    [SerializeField] private PlayableDirector playableDirector = null;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        if (playableDirector == null) {
            return;
        }

        playableDirector.time = 0;
        playableDirector.Evaluate();
        playableDirector.Play();

        // Ä¶Œã‚É–³Œø‰»‚µ‚ÄÄ“xƒgƒŠƒK[‚³‚ê‚È‚¢‚æ‚¤‚É‚·‚é
        this.enabled = false;
    }
}
