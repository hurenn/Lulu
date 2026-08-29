using UnityEngine;

/// <summary>
/// 落下復帰地点を管理するマネージャークラス
/// </summary>
public class FallReturnPointManager : MonoBehaviour {
    private static FallReturnPointManager _instance;
    public static FallReturnPointManager Instance {
        get {
            if (_instance == null) {
                GameObject obj = new GameObject("FallReturnPointManager");
                _instance = obj.AddComponent<FallReturnPointManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private Vector3 _lastReturnPoint = Vector3.zero;
    private bool _hasReturnPoint = false;

    private void Awake() {
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        } else if (_instance != this) {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 復帰地点を設定
    /// </summary>
    public void SetReturnPoint(Vector3 position) {
        _lastReturnPoint = position;
        _hasReturnPoint = true;
    }

    /// <summary>
    /// 最後の復帰地点を取得
    /// </summary>
    public Vector3? GetLastReturnPoint() {
        if (_hasReturnPoint) {
            return _lastReturnPoint;
        }
        return null;
    }

    /// <summary>
    /// 復帰地点をクリア
    /// </summary>
    public void ClearReturnPoint() {
        _hasReturnPoint = false;
        _lastReturnPoint = Vector3.zero;
    }
}
