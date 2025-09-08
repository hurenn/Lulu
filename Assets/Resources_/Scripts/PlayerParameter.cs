using System;
using UnityEngine;

public class PlayerParameter : MonoBehaviour
{    public static PlayerParameter Instance { get; private set; }
    [SerializeField]
    private int _level = 1; // レベル
    [SerializeField]
    private int _exp = 0; // 経験値
    [SerializeField]
    private int _expToNextLevel = 100; // 次のレベルまでの経験値

    public enum eLevelType
    {
        HP,
        MP,
        Attack,
        Max
    }
    [Serializable]
    public class LevelParameter
    {
        public int hpLevel = 0;
        public int mpLevel = 0;
        public int attackLevel = 0;
    }
    public LevelParameter levelParameter = new LevelParameter();

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    public void AddExp(int amount) {
        _exp += amount;
        Debug.Log($"Gained {amount} EXP. Total EXP: {_exp}/{_expToNextLevel}");
        while (_exp >= _expToNextLevel) {
            LevelUp();
        }
    }
    private void LevelUp() {
        _exp -= _expToNextLevel;
        _level++;
        Debug.Log($"Leveled up! New Level: {_level}. EXP for next level: {_expToNextLevel}");
        // レベルアップ時の処理（ステータスアップ、スキル取得など）をここに追加
    }
    public int GetLevel() {
        return _level;
    }
    public int GetExp() {
        return _exp;
    }
    public int GetExpToNextLevel() {
        return _expToNextLevel;
    }
}
