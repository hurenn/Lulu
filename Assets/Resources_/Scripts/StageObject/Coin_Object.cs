using UnityEngine;

public class Coin_Object : StageObject_Base
{
    [SerializeField] private int _coinValue = 1; // コインの価値
    [SerializeField] private AudioClip _collectSound; // コイン取得音
    protected override void _HitPlayer(Player_Character player)
    {
        base._HitPlayer(player);
        if (player != null)
        {
            PlayerParameter.Instance.AddScore(_coinValue);
            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound, transform.position);
            }
            Destroy(gameObject); // コインオブジェクトを削除
        }
    }
}
