using System.Collections;
using UnityEditor;
using UnityEngine;

public class ChangeScene : StageObject_Base
{
    [SerializeField] private SceneAsset _sceneAsset; // シーンアセット
    private string _sceneName => _sceneAsset != null ? _sceneAsset.name : string.Empty;

    [SerializeField]
    private PlayerController characterController = null;

    private void Reset() {
        characterController = FindAnyObjectByType<PlayerController>();
    }

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
