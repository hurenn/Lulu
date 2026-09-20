using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class CinemachineManager : SceneSingleton<CinemachineManager> {
    [SerializeField] private CinemachineCamera _playerCam;
    [SerializeField] private CinemachineCamera _zoomCam;

    [SerializeField] private CinemachineImpulseSource _impulseSource;

    private void Reset() {
        _playerCam = GameObject.Find("FollowCamera").GetComponent<CinemachineCamera>();
        _zoomCam = GameObject.Find("ZoomCamera").GetComponent<CinemachineCamera>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void ZoomOnTarget(Transform target) {
        _zoomCam.Follow = target;
        // 休眠中だった直前の位置からの補間(ダンピング)を無効化し、切り替わった瞬間に正しい位置へ即座にスナップさせる
        // (これが無いと、非アクティブだった間の古い位置から遠くを飛んできたようにカメラがブレて見える)
        _zoomCam.PreviousStateIsValid = false;
        _zoomCam.Priority = 20; // プレイヤーカメラより高く
    }

    public void ReturnToPlayer() {
        _zoomCam.Priority = -1;
    }

    public static event System.Action<float, float> OnShake;

    public void ShakeCamera(float intensity = 1.0f, float duration = 0.1f) {
        _impulseSource.ImpulseDefinition.AmplitudeGain = intensity;
        _impulseSource.DefaultVelocity = Vector3.down * intensity;
        _impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        _impulseSource.GenerateImpulse();
        OnShake?.Invoke(intensity, duration);
    }

}
