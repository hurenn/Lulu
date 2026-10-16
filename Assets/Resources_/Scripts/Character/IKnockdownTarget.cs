using UnityEngine;

// 跳ね返された巨大な弾などで吹き飛ばされ、ダウンするキャラが実装するインターフェース
public interface IKnockdownTarget {
    // direction: 吹き飛ぶ方向
    void Knockdown(Vector2 direction);
}
