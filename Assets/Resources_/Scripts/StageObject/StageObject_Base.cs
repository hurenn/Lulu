using UnityEngine;

public class StageObject_Base : MonoBehaviour
{
    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) {
            var player = collision.gameObject.GetComponent<Player_Character>();
            _HitPlayer(player);
        }
    }
    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<Player_Character>();
            _HitPlayer(player);
        }
    }

    protected virtual void _HitPlayer(Player_Character player)
    {
        Debug.Log("StageObject_Base: HitPlayer called on " + gameObject.name);
    }
}
