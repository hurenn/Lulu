using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerParameter : MonoBehaviour {
    private static PlayerParameter _instance;
    public static PlayerParameter Instance {
        get {
            if (_instance == null) {
                _CreateInstance();
            }
            return _instance;
        }
    }

    /// <summary>
    /// インスタンスを強制的に生成/初期化
    /// </summary>
    public static void CreateNewInstance() {
        if (_instance != null) {
            Destroy(_instance.gameObject);
            _instance = null;
        }
        _CreateInstance();
    }
    private static void _CreateInstance() {
        GameObject obj = new GameObject("PlayerParameter");
        _instance = obj.AddComponent<PlayerParameter>();
        DontDestroyOnLoad(obj);
    }

    [SerializeField]
    private int _level = 1; // レベル
    [SerializeField]
    private int _exp = 0; // 経験値
    public int currentExp => _exp; // 現在の経験値
    private int _expToNextLevel = 200; // 次のレベルまでの経験値
    public int nextExp { get => _expToNextLevel; set => _expToNextLevel = value; }
    public System.Action<int> OnExpChanged; // 経験値変更時のコールバック
    public enum eLevelType
    {
        HP,
        MP,
        Attack,
        All,
    }
    [Serializable]
    public class LevelParameter
    {
        public int hpLevel = 0;
        public int mpLevel = 0;
        public int attackLevel = 0;
    }
    public LevelParameter levelParameter = new LevelParameter();

    private Dictionary<eAbilityType, eAbilitySlot> _abilities = new Dictionary<eAbilityType, eAbilitySlot>(); // 取得済みの能力
    public Dictionary<eAbilityType, eAbilitySlot> Abilities => _abilities;
    public void AddAbility(eAbilityType ability_type, eAbilitySlot ability_slot) {
        if (!_abilities.ContainsKey(ability_type)) {
            _abilities.Add(ability_type, ability_slot);
        }
    }

    public enum eLanguage {
        Japanese,
        English,
    }
    public eLanguage language = eLanguage.Japanese; // 言語設定

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddExp(int amount, System.Action apply_status_callback) {
        _exp += amount;
        OnExpChanged?.Invoke(_exp);
        while (_exp >= _expToNextLevel) {
            LevelUp();
            apply_status_callback?.Invoke();
        }
    }
    private void LevelUp(eLevelType level_type = eLevelType.All) {
        _exp -= _expToNextLevel;
        _level++;

        switch (level_type) {
            case eLevelType.HP:
                levelParameter.hpLevel++;
                break;
            case eLevelType.MP:
                levelParameter.mpLevel++;
                break;
            case eLevelType.Attack:
                levelParameter.attackLevel++;
                break;
            case eLevelType.All:
                levelParameter.hpLevel++;
                levelParameter.mpLevel++;
                levelParameter.attackLevel++;
                break;
        }
    }

    public int GetLevel() {
        return _level;
    }
    public int GetExp() {
        return _exp;
    }
}
