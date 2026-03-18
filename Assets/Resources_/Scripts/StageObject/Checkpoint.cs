using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour {
    [SerializeField] private GameObject[] _unableObjects; // チェックポイントが有効なときに非表示にするオブジェクト
    [SerializeField] private GameObject[] _enableObjects; // チェックポイントが有効なときに表示にするオブジェクト

    // 事前に取得したパスリスト
    private List<string> _unableObjectPaths = new List<string>();
    private List<string> _enableObjectPaths = new List<string>();

    private void Awake() {
        // パスを事前に取得
        _CacheObjectPaths();
    }

    /// <summary>
    /// GameObjectのパスを事前にキャッシュ
    /// </summary>
    private void _CacheObjectPaths() {
        _unableObjectPaths.Clear();
        _enableObjectPaths.Clear();

        // 非表示対象オブジェクトのパスを取得
        if (_unableObjects != null) {
            foreach (var obj in _unableObjects) {
                if (obj != null) {
                    _unableObjectPaths.Add(_GetGameObjectPath(obj));
                }
            }
        }

        // 表示対象オブジェクトのパスを取得
        if (_enableObjects != null) {
            foreach (var obj in _enableObjects) {
                if (obj != null) {
                    _enableObjectPaths.Add(_GetGameObjectPath(obj));
                }
            }
        }
    }

    /// <summary>
    /// GameObjectのヒエラルキーパスを取得
    /// </summary>
    private string _GetGameObjectPath(GameObject obj) {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null) {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        var player = other.GetComponentInChildren<Player_Character>();
        if (player != null) {
            // パスのリストをCheckpointManagerに渡す
            CheckpointManager.Instance.SaveCheckpoint(
                transform.position, 
                player.GetParameterSnapshot(), 
                _unableObjectPaths, 
                _enableObjectPaths
            );

            player.SaveAbilitySlot();   // 能力保存
        }
    }
}