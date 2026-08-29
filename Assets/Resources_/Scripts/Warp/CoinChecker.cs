using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// コインの位置を管理する
/// </summary>
public class CoinChecker : MonoBehaviour {
    [SerializeField] private List<Coin_Object> coin_Objects = new List<Coin_Object>();

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Coin")) {
            Coin_Object coin = collision.GetComponent<Coin_Object>();
            if (coin != null && !coin_Objects.Contains(coin)) {
                coin_Objects.Add(coin);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Coin")) {
            Coin_Object coin = collision.GetComponent<Coin_Object>();
            if (coin != null && coin_Objects.Contains(coin)) {
                coin_Objects.Remove(coin);
            }
        }
    }

    /// <summary>
    /// 一番近いコインの位置を取得
    /// </summary>
    /// <param name="playerPos"></param>
    /// <returns></returns>
    public Vector2 GetNearestCoinPos(Vector3 playerPos) {
        // リストからmissingなオブジェクトを削除
        coin_Objects.RemoveAll(coin => coin == null);

        Vector2 nearestPos = Vector2.zero;
        float nearestDistance = float.MaxValue;
        foreach (var coin in coin_Objects) {
            if (coin != null) {
                Vector3 coinPos = coin.transform.position;
                float distance = Vector3.Distance(playerPos, coinPos);
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestPos = new Vector2(coinPos.x, coinPos.z);
                }
            }
        }
        return nearestPos;
    }
}