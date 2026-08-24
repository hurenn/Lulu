using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timeline上で時間の流れを制御するためのPlayableAssetクラス。
/// </summary>
[System.Serializable]
public class TimeScaleControlPlayableAsset : PlayableAsset
{
    [Tooltip("設定するTimeScale値（0.0〜1.0推奨）")]
    [Range(0f, 2f)]
    public float timeScale = 1f;

    [Tooltip("TimeScaleを元に戻すかどうか")]
    public bool restoreOnEnd = true;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TimeScaleControlBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.timeScale = timeScale;
        behaviour.restoreOnEnd = restoreOnEnd;
        return playable;
    }
}

/// <summary>
/// TimeScaleを制御するPlayableBehaviourクラス
/// </summary>
public class TimeScaleControlBehaviour : PlayableBehaviour
{
    public float timeScale = 1f;
    public bool restoreOnEnd = true;
    private float _originalTimeScale = 1f;
    private bool _hasStarted = false;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!_hasStarted)
        {
            // 元のTimeScaleを保存
            _originalTimeScale = Time.timeScale;
            _hasStarted = true;
        }

        // TimeScaleを設定
        Time.timeScale = timeScale;
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (restoreOnEnd && _hasStarted)
        {
            // TimeScaleを元に戻す
            Time.timeScale = _originalTimeScale;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (restoreOnEnd && _hasStarted)
        {
            // TimeScaleを元に戻す（念のため）
            Time.timeScale = _originalTimeScale;
        }
    }
}
