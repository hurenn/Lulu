using System.Collections;
using UnityEngine;

public class PooledEffect : MonoBehaviour {
    [SerializeField] private Animator _animator;
    [SerializeField] private ParticleSystem _particleSystem;

    internal GameObject PrefabKey { get; private set; }
    internal void SetPrefabKey(GameObject key) => PrefabKey = key;

    private void OnEnable() {
        StartCoroutine(_ReturnToPool());
    }

    protected virtual IEnumerator _ReturnToPool() {
        yield return null;
        float length = 0f;
        if (_animator != null && _animator.runtimeAnimatorController != null) {
            length = _animator.GetCurrentAnimatorStateInfo(0).length;
        } else if (_particleSystem != null) {
            length = _particleSystem.main.duration;
        }
        yield return new WaitForSeconds(length);
        EffectPool.Instance.Release(this);
    }
}
