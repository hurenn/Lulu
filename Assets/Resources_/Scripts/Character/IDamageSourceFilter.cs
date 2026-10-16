// 特定の攻撃(DamageZone)からだけダメージを受ける/受けないを選べるIDamageableが実装するインターフェース
public interface IDamageSourceFilter {
    // trueを返した攻撃からはヒットしない(ヒット記録・コールバック・ダメージ処理を全て行わない)
    bool IsIgnoredDamageSource(DamageZone source);
}
