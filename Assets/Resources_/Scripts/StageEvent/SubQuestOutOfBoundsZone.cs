using UnityEngine;

// 移動可能範囲を囲む複数の当たり判定(Collider2D, isTrigger)を1つのオブジェクトにまとめて付け、
// プレイヤーが場外に出たら強制的に時間切れにする
public class SubQuestOutOfBoundsZone : MonoBehaviour {
    [SerializeField] private SubQuest_Balloon _quest;

    private void Reset() {
        _quest = GetComponentInParent<SubQuest_Balloon>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (_quest == null || !_quest.IsRunning || !collision.CompareTag("Player")) return;
        _quest.ForceTimeUp();
    }
}
