using UnityEngine;

/// <summary>
/// 指定したオブジェクトを有効化するステージオブジェクト
/// </summary>
public class ObjectActivator : StageObject_Base {
    [SerializeField] private GameObject[] _targetObjects;

    [SerializeField] private BuildConfig.eBuildTypeMask _disableOnBuildTypes; // 無効化するビルドタイプ

    protected override void _HitPlayer(Player_Character player) {
        // ビルドタイプによる無効化チェック
        if (BuildConfig.Instance != null && BuildConfig.Instance.Matches(_disableOnBuildTypes)) {
            return;
        }

        foreach (var obj in _targetObjects) {
            if (obj != null) {
                obj.SetActive(true);
            }
        }
    }
}
