using UnityEngine;

public class AutoDestroy : MonoBehaviour {
    [SerializeField] private float _lifetime = 0f;
    [SerializeField] private Animator _animator;
    private void Start() {
        float add_time = 0;
        if (_animator != null) {
            add_time = _animator.GetCurrentAnimatorStateInfo(0).length;
        }
        Destroy(gameObject, _lifetime + add_time);
    }
}