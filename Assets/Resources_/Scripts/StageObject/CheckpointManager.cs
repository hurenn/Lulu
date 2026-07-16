using System.Collections.Generic;
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
    
    // GameObjectのパスを保存
    private List<string> _unableObjectPaths = new List<string>();
    private List<string> _enableObjectPaths = new List<string>();

    /// <summary>
    /// チェックポイントを保存（パスリストを直接受け取る）
    /// </summary>
    public void SaveCheckpoint(Vector3 position, PlayerParameterSnapshot snapshot,
        List<string> unableObjectPaths, List<string> enableObjectPaths)
    {
        _respawnPosition = position;
        _playerSnapshot = snapshot;
        _respawnSceneName = SceneManager.GetActiveScene().name;
        
        // パスリストをコピー
        _unableObjectPaths.Clear();
        _enableObjectPaths.Clear();
        
        if (unableObjectPaths != null) {
            _unableObjectPaths.AddRange(unableObjectPaths);
        }
        
        if (enableObjectPaths != null) {
            _enableObjectPaths.AddRange(enableObjectPaths);
        }
    }

    public void RespawnPlayer(Player_Character player)
    {
        player.transform.position = _respawnPosition;
        player.RestoreParameter(_playerSnapshot);
        
        // 保存されたパスからGameObjectを再取得して非表示化
        foreach (var path in _unableObjectPaths) {
            GameObject obj = _FindGameObjectByPath(path);
            if (obj != null) {
                obj.SetActive(false); // 再開時に無効化
            }
        }
        
        // 保存されたパスからGameObjectを再取得して表示化
        foreach (var path in _enableObjectPaths) {
            GameObject obj = _FindGameObjectByPath(path);
            if (obj != null) {
                obj.SetActive(true); // 再開時に有効化
            }
        }
    }

    public bool ShouldRespawnInCurrentScene()
    {
        return _respawnSceneName == SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// チェックポイントをクリア
    /// </summary>
    public void ClearCheckpoint() {
        _respawnSceneName = null;
        _respawnPosition = Vector3.zero;
        _playerSnapshot = null;
        _unableObjectPaths.Clear();
        _enableObjectPaths.Clear();
    }

    /// <summary>
    /// ヒエラルキーパスからGameObjectを検索
    /// </summary>
    private GameObject _FindGameObjectByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) {
            return null;
        }

        string[] pathParts = path.Split('/');
        GameObject current = null;

        // ルートオブジェクトを探す
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects) {
            if (root.name == pathParts[0]) {
                current = root;
                break;
            }
        }

        if (current == null) {
            return null;
        }

        // 子オブジェクトを辿る
        for (int i = 1; i < pathParts.Length; i++) {
            Transform child = current.transform.Find(pathParts[i]);
            if (child == null) {
                return null;
            }
            current = child.gameObject;
        }

        return current;
    }
}
