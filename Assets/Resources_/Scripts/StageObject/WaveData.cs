using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Lulu/WaveData")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class EnemyInfo {
        public GameObject enemyPrefab;
        public int count;
    }
    public EnemyInfo[] enemies;
    public float spawnInterval;
}
