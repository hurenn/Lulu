using UnityEngine;

public class StageObject_Base : MonoBehaviour
{
    [SerializeField] protected bool _isAlwaysAnimated = false; // 常にアニメーションするかどうか
    [SerializeField] protected Animator _animator;
    [SerializeField] protected Renderer _renderer;

    private void Reset() {
        _animator = GetComponent<Animator>();
        _renderer = GetComponent<Renderer>();
    }

    private void Update() {
        if(_isAlwaysAnimated == true || _animator == null || _renderer == null) {
            return;
        }

        // オブジェクトが画面内にある場合のみアニメーションを有効にする
        if(_renderer.isVisible && !_animator.enabled) {
            _animator.enabled = true;
        } else if(!_renderer.isVisible && _animator.enabled) {
            _animator.enabled = false;
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
}
