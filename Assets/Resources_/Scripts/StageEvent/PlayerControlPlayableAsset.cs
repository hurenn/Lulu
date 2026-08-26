using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class PlayerControlPlayableAsset : PlayableAsset {
    public bool enableControl = true;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
        var playable = ScriptPlayable<PlayerControlPlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.enableControl = enableControl;
        return playable;
    }
}

public class PlayerControlPlayableBehaviour : PlayableBehaviour {
    public bool enableControl = true;

    public override void OnBehaviourPlay(Playable playable, FrameData info) {

        // プレイヤーが着地するまで待つ
        var character = PlayerCharacterManager.Current as Player_Character;
        if (character != null) {
            character.StartCoroutine(_WaitPlayerStop(playable, character));
        }

        // プレイヤーコントローラーの有効/無効を切り替え
        var controller = PlayerCharacterManager.Controller;
        if (controller != null) {
            controller.isEnabledCharacterInput = enableControl;
        }
    }

    // プレイヤーが着地するまで待つコルーチン
    private IEnumerator _WaitPlayerStop(Playable playable, Player_Character character) {
        if (playable.GetGraph().GetResolver() is PlayableDirector director) {
            director.Pause();
            while (true) {
                if (character != null && character.isGrounded) {
                    break;
                }
                yield return null;
            }
            director.Resume();
        }
    }
}