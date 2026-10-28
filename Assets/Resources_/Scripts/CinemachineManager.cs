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

    // ズームカメラの元の設定(演出で映す範囲を変えた後に戻すため)
    private bool _isZoomDefaultSaved = false;
    private LensSettings _zoomDefaultLens;
    private Vector3 _zoomDefaultOffset;

    private CinemachinePositionComposer _ZoomComposer => _zoomCam.GetComponent<CinemachinePositionComposer>();

    private void _SaveZoomDefault() {
        if (_isZoomDefaultSaved) return;
        _isZoomDefaultSaved = true;
        _zoomDefaultLens = _zoomCam.Lens;
        var composer = _ZoomComposer;
        if (composer != null) _zoomDefaultOffset = composer.TargetOffset;
    }

    private void _RestoreZoomDefault() {
        if (!_isZoomDefaultSaved) return;
        _zoomCam.Lens = _zoomDefaultLens;
        var composer = _ZoomComposer;
        if (composer != null) composer.TargetOffset = _zoomDefaultOffset;
    }

    /// <summary>
    /// 指定した高さ(ワールド座標の長さ)の範囲が画面に映るように、targetを画面の中心に映す
    /// 毎フレーム呼んで、映す範囲を少しずつ変えることもできる。ReturnToPlayerで元の設定に戻る
    /// </summary>
    public void ZoomOnTarget(Transform target, float view_height) {
        _SaveZoomDefault();
        var lens = _zoomCam.Lens;
        var cam = Camera.main;
        if (cam != null && cam.orthographic) {
            lens.OrthographicSize = view_height * 0.5f;
        } else {
            var composer = _ZoomComposer;
            float distance = composer != null ? composer.CameraDistance : Mathf.Abs(_zoomCam.transform.position.z);
            lens.FieldOfView = 2f * Mathf.Atan(view_height * 0.5f / Mathf.Max(distance, 0.01f)) * Mathf.Rad2Deg;
        }
        _zoomCam.Lens = lens;
        var zoom_composer = _ZoomComposer;
        if (zoom_composer != null) zoom_composer.TargetOffset = Vector3.zero;
        _zoomCam.Follow = target;
        _zoomCam.Priority = 20;
    }

    public void ZoomOnTarget(Transform target) {
        _RestoreZoomDefault();
        _zoomCam.Follow = target;
        // 休眠中だった直前の位置からの補間(ダンピング)を無効化し、切り替わった瞬間に正しい位置へ即座にスナップさせる
        // (これが無いと、非アクティブだった間の古い位置から遠くを飛んできたようにカメラがブレて見える)
        _zoomCam.PreviousStateIsValid = false;
        _zoomCam.Priority = 20; // プレイヤーカメラより高く
    }

    public void ReturnToPlayer() {
        _zoomCam.Priority = -1;
        _RestoreZoomDefault();
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
