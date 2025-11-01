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
        var coin_pool = CoinPool.Instance;
        for (int i = 0; i < value; i++) {
            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            coin_pool.SpawnCoin(spawnPosition);
            yield return new WaitForSeconds(interval);
        }
    }
}
