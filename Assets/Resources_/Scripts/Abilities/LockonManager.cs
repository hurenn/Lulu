using UnityEngine;

public class LockonManager : MonoBehaviour
{
    // インスタンス
    public static LockonManager Instance { get; private set; }

    // ロックオンターゲット
    private Enemy_Base _currentTarget;
    public Transform targetTransform => _currentTarget.transform;

    private void Awake() {
        // シングルトン設定
        if (Instance == null) {
            Instance = this;
        }
    }

    public void SetTarget(Enemy_Base target) {
        // 既にターゲットがいる場合は解除
        if(_currentTarget != null) {
            _currentTarget.EnableLockOnMarker(false);
        }

        _currentTarget = target;
        _currentTarget.EnableLockOnMarker(true);
    }

    private void Update() {
        if(_currentTarget == null) { return; }

        // ターゲットが死んだらロックオン解除
        if (_currentTarget.isDead) {
            _currentTarget.EnableLockOnMarker(false);
            _currentTarget = null;
        }

        // ターゲットが画面外に出たらロックオン解除
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentTarget.transform.position);
        if(screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height) {
            _currentTarget.EnableLockOnMarker(false);
            _currentTarget = null;
        }
    }
}
