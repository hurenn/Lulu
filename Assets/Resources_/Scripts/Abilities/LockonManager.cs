using UnityEngine;

/// <summary>
/// ロックオン管理
/// </summary>
public class LockonManager : MonoBehaviour
{
    private static LockonManager _instance;
    
    public static LockonManager Instance {
        get {
            // インスタンスがない場合は生成
            _Instantiate();
            return _instance;
        }
    }
    
    public static bool HasInstance => _instance != null;

    // ロックオンターゲット
    private Enemy_Base _currentTarget;
    public Transform targetTransform => _currentTarget?.transform;
    // ターゲットのワープチェッカー取得
    public WarpChecker? GetTargetWarpPos(WarpControl.eWarpDirection direction) {
        if (_currentTarget == null) return null;

        var warpPos = _currentTarget.GetWarpChecker(direction);
        return warpPos;
    }

    public bool HasTarget => _currentTarget != null;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Debug.LogWarning("二重生成防止のため、LockonManager削除");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (_instance == this) {
            _instance = null;
        }
    }

    /// <summary>
    /// インスタンス生成
    /// </summary>
    private static void _Instantiate() {
        if (!HasInstance) {
            GameObject obj = new GameObject("LockonManager");
            obj.AddComponent<LockonManager>();
        }
    }

    /// <summary>
    /// 外部からターゲット設定
    /// </summary>
    /// <param name="target">ロックオン対象</param>
    public static void SetTargetStatic(Enemy_Base target) 
    {
        // インスタンスがない場合は生成
        _Instantiate();

        if (HasInstance) {
            Instance._SetTarget(target);
        }
    }

    /// <summary>
    /// ターゲット設定
    /// </summary>
    private void _SetTarget(Enemy_Base target) 
    {
        // 既にターゲットがいる場合は解除
        ClearTarget();

        _currentTarget = target;
        if (_currentTarget != null)
        {
            _currentTarget.EnableLockOnMarker(true);
        }
    }

    /// <summary>
    /// ターゲット解除
    /// </summary>
    public void ClearTarget() {
        if (_currentTarget != null) {
            _currentTarget.EnableLockOnMarker(false);
            _currentTarget = null;
        }
    }

    private void Update() {
        if (_currentTarget == null) return;

        // ターゲットが死んだらロックオン解除
        if (_currentTarget.isDead) {
            ClearTarget();
            return;
        }

        // ターゲットが画面外に出たらロックオン解除
        if (_IsTargetOffScreen()) {
            ClearTarget();
        }
    }

    /// <summary>
    /// カメラの画面外にターゲットがいるか
    /// </summary>
    private bool _IsTargetOffScreen()
    {
        if (Camera.main == null || _currentTarget == null) return false;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentTarget.transform.position);
        return screenPos.x < 0 || screenPos.x > Screen.width || 
               screenPos.y < 0 || screenPos.y > Screen.height;
    }
}
