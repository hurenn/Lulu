using System.Collections;
using UnityEngine;

public class CoinSpawner : MonoBehaviour {
    [SerializeField] private float _spawnInterval = 0.01f;

    // 生成完了後に自身を削除するか（SpawnCoinAtで一時生成した場合）
    private bool _isDestroyOnFinish = false;

    /// <summary>
    /// 指定位置にコイン生成（生成元がすぐ消える場合用。一時的なCoinSpawnerを作り、生成完了後に削除する）
    /// </summary>
    public static void SpawnCoinAt(Vector3 position, int value) {
        if (value <= 0) return;

        var go = new GameObject("CoinSpawner(Temp)");
        go.transform.position = position;
        var spawner = go.AddComponent<CoinSpawner>();
        spawner._isDestroyOnFinish = true;
        spawner.SpawnCoin(value);
    }

    /// <summary>
    /// コイン生成
    /// </summary>
    public void SpawnCoin(int value) {
        StartCoroutine(SpawnCoinCoroutine(value, _spawnInterval));
    }

    /// <summary>
    /// コイン生成コルーチン
    /// </summary>
    private IEnumerator SpawnCoinCoroutine(int value, float interval) {
        int coin_value = 1;
        int coin_count = value;
        int remainder = 0;

        // 大量のコインを一度に生成する場合、価値の高いコインに変換して生成数を減らす
        if (value > 50) {
            coin_value = 5;
            coin_count = value / coin_value;
            remainder = value % coin_value; // 割り切れない端数（整数除算による価値の消失を防ぐ）
        }

        var coin_pool = CoinPool.Instance;
        for (int i = 0; i < coin_count; i++) {
            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            var coin = coin_pool.SpawnCoin(spawnPosition);
            coin.SetCoinValue(coin_value);
            yield return new WaitForSeconds(interval);
        }

        // 端数分を1枚のコインとして生成し、価値が静かに減るのを防ぐ
        if (remainder > 0) {
            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            var coin = coin_pool.SpawnCoin(spawnPosition);
            coin.SetCoinValue(remainder);
        }

        if (_isDestroyOnFinish) {
            Destroy(gameObject);
        }
    }
}
