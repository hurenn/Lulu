using UnityEngine;

// 落下(外れ扱いの当たり判定)に触れたらPlatformSequenceGimmickの進行をリセットする
public class PlatformSequenceFailZone : MonoBehaviour {
    [SerializeField] private PlatformSequenceGimmick _gimmick;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.CompareTag("Player")) return;
        _gimmick.ResetSequence();
    }
}
