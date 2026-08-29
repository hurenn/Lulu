using UnityEngine;

public class RecoverItem_Object : StageObject_Base
{
    // 回復量
    [SerializeField] private int _recoverValue = 1;
    // 回復エフェクトのプレハブ
    [SerializeField] private GameObject _pickupEffectPrefab;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);

        // プレイヤーの体力を回復する
        player.RecoverHP(_recoverValue);

        // 回復エフェクトを生成する
        if (_pickupEffectPrefab != null) {
            Instantiate(_pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        // アイテムを消す
        Destroy(gameObject);
    }
}
