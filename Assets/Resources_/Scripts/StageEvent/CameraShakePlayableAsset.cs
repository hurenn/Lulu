using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class CameraShakePlayableAsset : PlayableAsset
{
    [Range(0f, 5f)] public float intensity = 1f;
    [Range(0f, 1f)] public float duration = 0.1f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CameraShakePlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.intensity = intensity;
        behaviour.duration = duration;
        return playable;
    }
}

public class CameraShakePlayableBehaviour : PlayableBehaviour
{
    public float intensity;
    public float duration;
    private bool _fired;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (_fired) return;
        _fired = true;
        CinemachineManager.Instance?.ShakeCamera(intensity, duration);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        _fired = false;
    }
}
