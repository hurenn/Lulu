using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// CinemachineManager経由でカメラを揺らすFeelフィードバック。
/// (FeelのCinemachineImpulseを直接使うとCinemachineManager.OnShakeが発火せず、背景の揺れと同期しなくなるため)
/// </summary>
[AddComponentMenu("")]
[FeedbackHelp("CinemachineManager.ShakeCamera を呼び出してカメラを揺らします。強さはフィードバックの強度(Intensity)で倍率がかかります。")]
[System.Serializable]
[FeedbackPath("Lulu/Camera Shake")]
public class MMF_LuluCameraShake : MMF_Feedback {
    public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
    public override Color FeedbackColor => MMFeedbacksInspectorColors.CameraColor;
#endif

    [MMFInspectorGroup("Camera Shake", true, 44)]
    [Tooltip("揺れの強さ")]
    public float Intensity = 0.1f;
    [Tooltip("揺れの時間")]
    public float Duration = 0.1f;

    public override float FeedbackDuration => Duration;

    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f) {
        if (!Active || !FeedbackTypeAuthorized || !CinemachineManager.HasInstance) {
            return;
        }
        CinemachineManager.Instance.ShakeCamera(Intensity * ComputeIntensity(feedbacksIntensity, position), Duration);
    }
}
