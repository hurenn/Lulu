using UnityEngine;

// 防いだ攻撃(IDamageSourceBlocker)を、弾いたように見せるIDamageableが実装するインターフェース
// 実装すると、防いだ攻撃が当たった時にヒットストップ(カメラは揺らさない)がかかり、OnDeflectedが呼ばれる
public interface IDamageSourceDeflector {
    // 防いだ攻撃が当たった時に呼ばれる(positionは当たった位置。弾いたエフェクトを出す等)
    void OnDeflected(DamageZone source, Vector3 position);
}
