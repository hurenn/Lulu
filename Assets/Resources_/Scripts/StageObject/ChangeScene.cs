using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

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
            StartCoroutine(_ChangeSceneCoroutine());
        } else {
            Debug.LogWarning("SceneAsset is not assigned.");
        }
    }

    private IEnumerator _ChangeSceneCoroutine() {
        characterController.isEnabledCharacterInput = false;

        // フェードアウト
        yield return new WaitForSeconds(1.0f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(_sceneName);
    }
}
