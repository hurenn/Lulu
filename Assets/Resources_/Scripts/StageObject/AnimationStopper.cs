using UnityEngine;

/// <summary>
/// 当たり判定外に出たときにアニメーションを停止するコンポーネント
/// </summary>
public class AnimationStopper : MonoBehaviour {
    [SerializeField] private string playerTag = "Player"; // プレイヤーのタグ指定
    [SerializeField] private Animator _animator;

    void Reset() {
        _animator = GetComponent<Animator>();
        _animator.enabled = false; // 最初は停止
    }

    void Start() {
        if (_animator == null && _animator.enabled) {
            _animator.enabled = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag(playerTag))
            _animator.enabled = true; // プレイヤーが触れたら再生
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag(playerTag))
            _animator.enabled = false; // プレイヤーが離れたら停止
    }
}