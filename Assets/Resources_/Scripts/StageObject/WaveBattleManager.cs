using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WaveBattleManager : MonoBehaviour
{
    [Header("Wave設定")]
    public WaveData[] waveData; // WaveのデータをScriptableObjectとして参照
    public Transform[] spawnPoints; // 敵の出現ポイント

    [Header("カメラ設定")]
    [SerializeField] private Transform cameraLockPos; // カメラをロックする位置
    [SerializeField] private CameraFollow cameraFollow; // カメラ追従スクリプトの参照

    [Header("道封鎖設定")]
    [SerializeField] private GameObject[] walls; // 道を封鎖するオブジェクト

    private GameObject _appearEffect;
    private bool hasTriggered = false;
    private float _spawnInterval = 0.3f;
    private void Start() {
        // 道を封鎖するオブジェクトを非表示にする
        foreach (var wall in walls) {
            if (wall.activeSelf) wall.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (hasTriggered) return; // 既にトリガーが発動している場合は無視
        
        if (collision.CompareTag("Player")) {
            hasTriggered = true;
            StartWaves();
        }
    }

    public void StartWaves() {
        if (_appearEffect == null) {
            _appearEffect = Resources.Load("Prefabs/Effects/AppearEffect") as GameObject;
        }
        StartCoroutine(_RunWaves());
    }

    // Waveの実行を管理するコルーチン
    private IEnumerator _RunWaves() {
        // 道を封鎖
        foreach (var wall in walls) {
            if (!wall.activeSelf) wall.SetActive(true);
        }
        // カメラをロック
        cameraFollow.CameraLock(cameraLockPos.position);

        // 出現させた敵を管理するリスト
        List<Enemy_Base> spawnedEnemies = new List<Enemy_Base>();
        int nextSpawnIndex = 0;
        // 各Waveを順番に実行
        foreach (var wave in waveData) {

            foreach (var enemyInfo in wave.enemies) {
                for (int i = 0; i < enemyInfo.count; i++) {
                    // 出現エフェクトを表示
                    if (_appearEffect != null) {
                        var effect = Instantiate(_appearEffect, spawnPoints[nextSpawnIndex].position, Quaternion.identity);
                        Destroy(effect, 1.0f); // エフェクトを1秒後に削除
                    }
                    yield return new WaitForSeconds(_spawnInterval + wave.spawnInterval);
                    // 出現ポイントを順番に選択
                    var spawnPoint = spawnPoints[nextSpawnIndex];
                    nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
                    // 敵を出現させる
                    var enemy_obj = Instantiate(enemyInfo.enemyPrefab, spawnPoint.position, Quaternion.identity);
                    var enemy_base = enemy_obj.GetComponent<Enemy_Base>();
                    spawnedEnemies.Add(enemy_base);
                    // 敵が倒されたときの処理
                    enemy_base.OnDied += () => {
                        spawnedEnemies.Remove(enemy_base);
                    };
                    // スポーン地点が空くまで待機
                    while (spawnedEnemies.Count >= spawnPoints.Length) {
                        yield return null;
                    }
                }
            }
            // 全ての敵が倒されるまで待機
            while (spawnedEnemies.Count > 0) {
                yield return null;
            }
        }

        // 道を解放
        foreach (var wall in walls) {
            if (wall.activeSelf) wall.SetActive(false);
        }
        // カメラの追従を元に戻す
        cameraFollow.ReleaseCameraLock();
        gameObject.SetActive(false);
    }
}
