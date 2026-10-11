using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : PersistentSingleton<CheckpointManager>
{
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

    /// <summary>
    /// 復帰位置とプレイヤー状態のみ更新する（直前のチェックポイントで保存した表示/非表示オブジェクトは引き継ぐ）
    /// </summary>
    public void SaveCheckpoint(Vector3 position, PlayerParameterSnapshot snapshot)
    {
        // 別シーンのチェックポイントのオブジェクトパスは引き継がない
        if (!ShouldRespawnInCurrentScene()) {
            _unableObjectPaths.Clear();
            _enableObjectPaths.Clear();
        }
        _respawnPosition = position;
        _playerSnapshot = snapshot;
        _respawnSceneName = SceneManager.GetActiveScene().name;
    }

    // 救出ミッションで救出済みのSurvivor（リトライ時も救出状態を維持する。通常のチェックポイントで上書きされないよう別管理）
    private string _rescueSceneName;
    private HashSet<string> _rescuedSurvivorIds = new HashSet<string>();
    private HashSet<string> _rescuedNoMessageSurvivorIds = new HashSet<string>(); // 救出メッセージを出さないSurvivor（アオイ等のイベントキャラ）

    /// <summary>
    /// 救出済みのSurvivorを記録
    /// </summary>
    /// <param name="shows_message">救出メッセージの順番に数えるか</param>
    public void SaveRescuedSurvivor(string survivor_id, bool shows_message = true)
    {
        string scene_name = SceneManager.GetActiveScene().name;
        if (_rescueSceneName != scene_name) {
            _rescueSceneName = scene_name;
            _rescuedSurvivorIds.Clear();
            _rescuedNoMessageSurvivorIds.Clear();
        }
        _rescuedSurvivorIds.Add(survivor_id);
        if (!shows_message) {
            _rescuedNoMessageSurvivorIds.Add(survivor_id);
        }
    }

    /// <summary>
    /// 現在のシーンで救出済みのSurvivorか
    /// </summary>
    public bool IsRescuedSurvivor(string survivor_id)
    {
        return _rescueSceneName == SceneManager.GetActiveScene().name && _rescuedSurvivorIds.Contains(survivor_id);
    }

    /// <summary>
    /// アオイの同行フラグ（救出イベント後～目的地到着イベントまで。リトライでは消えない）
    /// </summary>
    public bool isAoiAccompanying = false;

    /// <summary>
    /// 現在のシーンで救出済みのSurvivorの人数
    /// </summary>
    public int RescuedSurvivorCount =>
        _rescueSceneName == SceneManager.GetActiveScene().name ? _rescuedSurvivorIds.Count : 0;

    /// <summary>
    /// 現在のシーンで救出済みのSurvivorのうち、救出メッセージの順番に数える人数
    /// </summary>
    public int RescuedMessageSurvivorCount =>
        _rescueSceneName == SceneManager.GetActiveScene().name ? _rescuedSurvivorIds.Count - _rescuedNoMessageSurvivorIds.Count : 0;

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
        _rescueSceneName = null;
        _rescuedSurvivorIds.Clear();
        _rescuedNoMessageSurvivorIds.Clear();
        isAoiAccompanying = false;
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
