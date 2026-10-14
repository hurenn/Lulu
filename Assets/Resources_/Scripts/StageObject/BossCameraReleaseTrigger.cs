using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーが触れたら、ボス用カメラから通常のカメラ(FollowCamera)に戻すトリガー
/// ボス部屋から抜け出した地点に配置する
/// </summary>
public class BossCameraReleaseTrigger : StageObject_Base {
    [SerializeField] private CinemachineCamera _bossCamera; // 解除するボス用カメラ
    [SerializeField] private int _releasedPriority = -1;    // 解除後の優先度(FollowCameraより低くする)

    protected override void _HitPlayer(Player_Character player) {
        if (_bossCamera == null) return;
        _bossCamera.Priority = _releasedPriority;
    }

    protected override void _ExitPlayer(Player_Character player) {
        // 抜けた時は何もしない
    }
}
