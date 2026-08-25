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

    // 能力タイプごとの割り当てスロット（所有の有無に関わらず、常に全タイプ分存在する）
    private Dictionary<eAbilityType, eAbilitySlot> _abilitySlotAssignment = new Dictionary<eAbilityType, eAbilitySlot> {
        { eAbilityType.Ice, eAbilitySlot.Y },
        { eAbilityType.Light, eAbilitySlot.X },
        { eAbilityType.Fire, eAbilitySlot.A },
        { eAbilityType.Warp, eAbilitySlot.B },
    };
    // 所有済みの能力タイプ（Warpは最初から所有）
    private HashSet<eAbilityType> _ownedAbilities = new HashSet<eAbilityType> { eAbilityType.Warp };
    public IReadOnlyCollection<eAbilityType> OwnedAbilities => _ownedAbilities;

    /// <summary>
    /// 指定した能力タイプを所有しているか
    /// </summary>
    public bool IsOwned(eAbilityType ability_type) {
        return _ownedAbilities.Contains(ability_type);
    }

    /// <summary>
    /// 指定した能力タイプが現在割り当てられているスロットを取得（所有していなくても常に取得できる）
    /// </summary>
    public eAbilitySlot GetAssignedSlot(eAbilityType ability_type) {
        return _abilitySlotAssignment.TryGetValue(ability_type, out var slot) ? slot : default;
    }

    /// <summary>
    /// 指定したスロットに現在割り当てられている能力タイプを取得（所有していなくても常に取得できる）
    /// </summary>
    public eAbilityType GetAssignedAbilityType(eAbilitySlot slot) {
        foreach (var kvp in _abilitySlotAssignment) {
            if (kvp.Value == slot) {
                return kvp.Key;
            }
        }
        return eAbilityType.None;
    }

    /// <summary>
    /// 能力タイプを所有済みにする
    /// </summary>
    public void AddAbility(eAbilityType ability_type) {
        _ownedAbilities.Add(ability_type);
    }

    /// <summary>
    /// 指定スロットに割り当てられている能力タイプを所有解除する
    /// </summary>
    public void RemoveAbility(eAbilitySlot ability_slot) {
        foreach (var kvp in _abilitySlotAssignment) {
            if (kvp.Value == ability_slot) {
                _ownedAbilities.Remove(kvp.Key);
                break;
            }
        }
    }

    /// <summary>
    /// 2つのスロットの割り当てを入れ替える（所有状況に関わらず、割り当てテーブルそのものを更新する）
    /// </summary>
    public void SwapAssignedSlots(eAbilitySlot slotA, eAbilitySlot slotB) {
        eAbilityType? typeInA = null;
        eAbilityType? typeInB = null;
        foreach (var kvp in _abilitySlotAssignment) {
            if (kvp.Value == slotA) typeInA = kvp.Key;
            else if (kvp.Value == slotB) typeInB = kvp.Key;
        }
        if (typeInA.HasValue) _abilitySlotAssignment[typeInA.Value] = slotB;
        if (typeInB.HasValue) _abilitySlotAssignment[typeInB.Value] = slotA;
    }

    /// <summary>
    /// 能力の割り当てを初期状態に戻す
    /// </summary>
    public void ResetAbilitySlotAssignment() {
        _abilitySlotAssignment[eAbilityType.Ice] = eAbilitySlot.Y;
        _abilitySlotAssignment[eAbilityType.Light] = eAbilitySlot.X;
        _abilitySlotAssignment[eAbilityType.Fire] = eAbilitySlot.A;
        _abilitySlotAssignment[eAbilityType.Warp] = eAbilitySlot.B;
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

    public bool AddExp(int amount, System.Action apply_status_callback) {
        bool is_level_up = false;
        _exp += amount;
        OnExpChanged?.Invoke(_exp);
        while (_exp >= _expToNextLevel) {
            LevelUp();
            apply_status_callback?.Invoke();
            is_level_up = true;
        }
        return is_level_up;
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
