using System.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class AutoDestroy : MonoBehaviour {
    [SerializeField] protected float _lifetime = 0f;
    [SerializeField] private Animator _animator;

    // 消滅時のコールバック
    private System.Action _destroyedCallback = null;
    public void SetCallback(System.Action callback) {
        _destroyedCallback = callback;
    }

    private void Start() {
        StartCoroutine(_DestroyCoroutine());
    }

    /// <summary>
    /// 自動削除までのコルーチン
    /// </summary>
    private IEnumerator _DestroyCoroutine() {
        float current_time = 0;
        float add_time = 0;
        if (_animator != null) {
            add_time = _animator.GetCurrentAnimatorStateInfo(0).length;
        }

        while(current_time < _lifetime + add_time) {
            current_time += Time.deltaTime;
            yield return null;
        }

        if (_destroyedCallback != null) {
            _destroyedCallback.Invoke();
        }
        Destroy(gameObject);
    }
}