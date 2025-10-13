using UnityEngine;

/// <summary>
/// コインオブジェクト
/// </summary>
public class Coin_Object : StageObject_Base
{
    [SerializeField] private int _MpRecoverAmount = 5; // コイン取得で回復するMP量
    [SerializeField] private int _coinValue = 1; // コインの価値
    [SerializeField] private AudioClip _collectSound; // コイン取得音
    protected override void _HitPlayer(Player_Character player)
    {
        base._HitPlayer(player);
        if (player != null)
        {
            player.RecoverMP(_MpRecoverAmount, true); // コイン取得でMPを回復
            player.AddExp(_coinValue);
            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound, transform.position);
            }
            Destroy(gameObject); // コインオブジェクトを削除
        }
    }
}
