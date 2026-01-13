using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    private static CheckpointManager _instance;
    public static CheckpointManager Instance {
        get {
            if (_instance == null) {
                GameObject obj = new GameObject("CheckpointManager");
                _instance = obj.AddComponent<CheckpointManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
        set {
            _instance = value;
        }
    }
    private Vector3 _respawnPosition;
    private PlayerParameterSnapshot _playerSnapshot;
    private string _respawnSceneName;

    public void SaveCheckpoint(Vector3 position, PlayerParameterSnapshot snapshot)
    {
        _respawnPosition = position;
        _playerSnapshot = snapshot;
        _respawnSceneName = SceneManager.GetActiveScene().name;
    }

    public void RespawnPlayer(Player_Character player)
    {
        player.transform.position = _respawnPosition;
        player.RestoreParameter(_playerSnapshot);
    }

    public bool ShouldRespawnInCurrentScene()
    {
        return _respawnSceneName == SceneManager.GetActiveScene().name;
    }
}
