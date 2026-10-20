// 特定の攻撃(DamageZone)をシールド等で防ぐIDamageableが実装するインターフェース
public interface IDamageSourceBlocker {
    // trueを返した攻撃はダメージ・ロックオン・ヒットエフェクトを発生させず、ヒットで消える攻撃(_isHitDestroy)なら消える
    // sourceがnullの時は、DamageZoneを通さない攻撃(炎の必殺技の範囲攻撃など)
    bool IsBlockedDamageSource(DamageZone source);
}
