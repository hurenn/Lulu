using UnityEngine;

// 足場1つ分。プレイヤーが乗ったことをPlatformSequenceGimmickへ伝える
public class PlatformSequenceStep : MonoBehaviour {
    [SerializeField] private PlatformSequenceGimmick _gimmick;
    [SerializeField] private int _stepIndex; // この足場の順番(0始まり)

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.CompareTag("Player")) return;
        _gimmick.OnPlatformLanded(_stepIndex);
    }
}
