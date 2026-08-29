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
