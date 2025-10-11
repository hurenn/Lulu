using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("追従対象")]
    public Transform target;
    public bool isEnableFollow = true;

    [Header("カメラの位置オフセット")]
    public Vector2 offset = new Vector2(2f, 1f);

    [Header("カメラの位置が戻るまでの時間")]
    public float warpSmoothTime = 0.1f;
    private float _currentWarpSmoothTime = 0f;

    // ワープモードカメラフラグ
    private bool _isWarpMode = false;
    public void SetWarpMode(bool is_enable)
    {
        _isWarpMode = is_enable;
        _currentWarpSmoothTime = is_enable ? 0f : warpSmoothTime;   
    }

    [Header("カメラの制限範囲")]
    public Vector2 minPosition = new Vector2(-20f, -20f);
    public Vector2 maxPosition = new Vector2(20f, 20f);
    
    [SerializeField]
    private Camera _camera;

    void LateUpdate()
    {
        if (target == null || !isEnableFollow) return;

        // 追従位置の計算
        Vector2 targetPosition = (Vector2)target.position + offset;

        // カメラの位置をなめらかに更新
        Vector2 smoothPosition;
        if (_isWarpMode)
        {
            smoothPosition = Vector2.Lerp(
                transform.position, targetPosition,
                _currentWarpSmoothTime / warpSmoothTime);

            _currentWarpSmoothTime += Time.deltaTime;

            // カメラの位置がtargetPositionの位置まで到達したら、ワープモードを解除
            if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                SetWarpMode(false);
            }
        }
        else
        {
            smoothPosition = targetPosition;
        }

        // カメラサイズに応じた表示範囲の補正
        float cameraHeight = _camera.orthographicSize;
        float cameraWidth = cameraHeight * _camera.aspect;

        // カメラの位置を制限範囲内に収める
        smoothPosition.x = Mathf.Clamp(smoothPosition.x, minPosition.x + cameraWidth, maxPosition.x - cameraWidth);
        smoothPosition.y = Mathf.Clamp(smoothPosition.y, minPosition.y + cameraHeight, maxPosition.y - cameraHeight);

        transform.position = new Vector3(smoothPosition.x, smoothPosition.y, transform.position.z);
    }
}
