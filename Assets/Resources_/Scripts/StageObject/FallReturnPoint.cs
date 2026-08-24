using UnityEngine;

/// <summary>
/// 落下復帰地点を設定するトリガー
/// プレイヤーが通過すると、この位置を復帰地点として記録する
/// </summary>
public class FallReturnPoint : MonoBehaviour {
    [SerializeField] private Transform _returnPoint; // 復帰地点（未設定の場合は自身の位置）
    [SerializeField] private bool _setOnAwake = false; // Awake時に自動設定するか

    private void Awake() {
        if (_setOnAwake) {
            _SetReturnPoint();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        var player = other.GetComponentInChildren<Player_Character>();
        if (player != null) {
            _SetReturnPoint();
        }
    }

    private void _SetReturnPoint() {
        Vector3 returnPosition = _returnPoint != null ? _returnPoint.position : transform.position;
        FallReturnPointManager.Instance.SetReturnPoint(returnPosition);
    }
}
