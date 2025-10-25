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
            LoadScene(_sceneName, characterController);
        } else {
            Debug.LogWarning("SceneAsset is not assigned.");
        }
    }

    public static void LoadScene(string sceneName = null, PlayerController characterController = null) {
        if (characterController != null) {
            characterController.isEnabledCharacterInput = false;
        }

        // 切り替えシーンの読み込み
        if (sceneName == null) {
            sceneName = SceneManager.GetActiveScene().name;
        }
        FadeManager.Instance.LoadScene(sceneName, 0.5f);
    }
}
