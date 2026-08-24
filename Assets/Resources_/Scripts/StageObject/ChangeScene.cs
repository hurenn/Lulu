using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : StageObject_Base
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset _sceneAsset; // シーンアセット
#endif
    [SerializeField] private string _sceneName;

    [SerializeField]
    private PlayerController characterController = null;

    private void Reset() {
        characterController = FindAnyObjectByType<PlayerController>();
    }

#if UNITY_EDITOR
    private void OnValidate() {
      if(_sceneAsset != null) {
          _sceneName = _sceneAsset.name;
        }
    }
#endif

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        if (!string.IsNullOrEmpty(_sceneName)) {
            LoadScene(true, _sceneName, characterController);
        } else {
            Debug.LogWarning("SceneAsset is not assigned.");
        }
    }

    public static void LoadScene(bool is_save_ability, string sceneName = null, PlayerController characterController = null) {
        // 入力を無効化
        if (characterController != null) {
            characterController.isEnabledCharacterInput = false;
        }
        // プレイヤーの状態を保存
        if (is_save_ability) {
            var player = PlayerCharacterManager.Current as Player_Character;
            if (player != null) {
                player.SavePlayerState();
            } else {
                Debug.LogError("Player_Character not found for saving status.");
            }
        }

        // シーンの切り替え
        if (sceneName == null) {
            sceneName = SceneManager.GetActiveScene().name;
        }
        FadeManager.Instance.LoadScene(sceneName, 0.5f);
    }
}
