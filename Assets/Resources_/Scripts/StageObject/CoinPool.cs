using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinPool : MonoBehaviour {
    private static CoinPool _instance;
    public static CoinPool Instance {
        get {
            if (_instance == null) {
                _instance = FindAnyObjectByType<CoinPool>();
                if (_instance == null) {
                    GameObject obj = new GameObject("CoinPool");
                    _instance = obj.AddComponent<CoinPool>();
                }
            }
            return _instance;
        }
    }

    // コインのプレハブ
    [SerializeField] private Coin_Object _coinPrefab;
    // コインプール
    private Queue<Coin_Object> _coinPool = new Queue<Coin_Object>();

    // コインを生成またはプールから取得する
    public Coin_Object SpawnCoin(Vector3 position) {
        Coin_Object coin;
        if (_coinPool.Count > 0) {
            coin = _coinPool.Dequeue();
            coin.gameObject.SetActive(true);
        } else {
            coin = Instantiate(_coinPrefab);
        }
        coin.Spawn(position, Release);
        return coin;
    }

    public void Release(Coin_Object coin) {
        coin.gameObject.SetActive(false);
        _coinPool.Enqueue(coin);
    }
}