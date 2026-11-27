using UnityEngine;

// Wave戦闘の終了条件となるエリア。プレイヤーが一度でも触れたら IsTouched が立つ
// WaveBattleManager の _clearTrigger に設定すると、触れるまでWaveの敵が何度でも復活する（チュートリアル用）
[RequireComponent(typeof(Collider2D))]
public class WaveClearTrigger : MonoBehaviour
{
    public bool IsTouched { get; private set; } = false;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (IsTouched) return;
        if (collision.CompareTag("Player")) {
            IsTouched = true;
        }
    }
}
