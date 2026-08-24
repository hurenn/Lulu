using System.Collections;
using UnityEngine;

public class CoinSpawner : MonoBehaviour {
    [SerializeField] private float _spawnInterval = 0.01f;

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

        // 大量のコインを一度に生成する場合、価値の高いコインに変換して生成数を減らす
        if (value > 50) {
            coin_value = 5;
            coin_count = value / coin_value;
        }

        var coin_pool = CoinPool.Instance;
        for (int i = 0; i < coin_count; i++) {
            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            var coin = coin_pool.SpawnCoin(spawnPosition);
            coin.SetCoinValue(coin_value);
            yield return new WaitForSeconds(interval);
        }
    }
}
