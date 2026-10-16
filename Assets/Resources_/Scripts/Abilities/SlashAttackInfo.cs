using UnityEngine;

/// <summary>
/// 氷の能力のコンボ攻撃(Slash1～3)の攻撃エフェクトに付け、何段目の攻撃かを判別できるようにする
/// （敵の弾を跳ね返す・止める判定などに使う）
/// </summary>
public class SlashAttackInfo : MonoBehaviour {
    [SerializeField] private int _comboStep = 1; // コンボの段数(1～3)

    public int ComboStep => _comboStep;
}
