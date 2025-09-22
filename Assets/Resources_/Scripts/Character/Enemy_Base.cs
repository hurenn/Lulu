using System.Collections;
using UnityEngine;

public class Enemy_Base : Character_Base
{
    // 経験値
    [SerializeField] private int _exp = 1;

    // ワープチェック用のコンポーネント
    [SerializeField] private WarpChecker _leftWarpChecker;
    [SerializeField] private WarpChecker _rightWarpChecker;

    // ロックオンマーカー
    [SerializeField] private GameObject _lockonMarker;

    protected override IEnumerator Die() {
        // 経験値取得
        PlayerParameter.Instance.AddExp(_exp);

        return base.Die();
    }

    /// <summary>
    /// ワープ地点取得
    /// </summary>
    /// <param name="warp_direction">ワープの向き</param>
    public Vector2? GetWarpPoint(WarpControl.eWarpDirection warp_direction) {
        if (warp_direction == WarpControl.eWarpDirection.Left) {
            return _leftWarpChecker.GetWarpPoint(_leftWarpChecker.transform.position);
        } else if (warp_direction == WarpControl.eWarpDirection.Right) {
            return _rightWarpChecker.GetWarpPoint(_rightWarpChecker.transform.position);
        }
        return null;
    }

    /// <summary>
    /// ロックオン表示切替
    /// </summary>
    public void EnableLockOnMarker(bool enable) {
        _lockonMarker.SetActive(enable);
    }
}
