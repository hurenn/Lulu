using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// ロックオン管理
/// </summary>
public class LockonManager : SceneSingleton<LockonManager>
{
    // インスタンスがない場合は生成してから返す
    public static new LockonManager Instance {
        get {
            InstantiateIfMissing();
            return _instance;
        }
    }

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

    /// <summary>
    /// 外部からターゲット設定
    /// </summary>
    /// <param name="target">ロックオン対象</param>
    public static void SetTargetStatic(Enemy_Base target)
    {
        // インスタンスがない場合は生成
        InstantiateIfMissing();

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
    private bool _IsTargetOffScreen() {
        if (Camera.main == null || _currentTarget == null) return false;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentTarget.transform.position);
        var heightOffset = (Screen.width - Screen.height); // 少し余裕を持たせる

        return screenPos.x < 0 || screenPos.x > Screen.width ||
               screenPos.y < 0 - heightOffset || screenPos.y > Screen.height + heightOffset;
    }
}
