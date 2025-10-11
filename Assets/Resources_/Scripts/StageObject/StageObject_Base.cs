using UnityEngine;

public class StageObject_Base : MonoBehaviour
{
    [SerializeField] protected bool _isAlwaysAnimated = false; // 常にアニメーションするかどうか
    [SerializeField] protected Animator _animator;

    private void Reset() {
        _animator = GetComponent<Animator>();
        if (_animator != null) _animator.enabled = false;
    }

    private void OnBecameInvisible() {
        if (!_isAlwaysAnimated && _animator != null) {
            _animator.enabled = false;
        }
    }
    private void OnBecameVisible() {
        if (_animator != null) {
            _animator.enabled = true;
        }
    }

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

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            var player = collision.gameObject.GetComponent<Player_Character>();
            _ExitPlayer(player);
        }
    }

    protected virtual void _ExitPlayer(Player_Character player) {
        Debug.Log("StageObject_Base: ExitPlayer called on " + gameObject.name);
    }
}
