using System.Collections;
using UnityEngine;

public class PooledEffect : MonoBehaviour {
    [SerializeField] private Animator _animator;

    internal GameObject PrefabKey { get; private set; }
    internal void SetPrefabKey(GameObject key) => PrefabKey = key;

    private void OnEnable() {
        StartCoroutine(_ReturnToPool());
    }

    private IEnumerator _ReturnToPool() {
        yield return null;
        float length = 0f;
        if (_animator != null && _animator.runtimeAnimatorController != null) {
            length = _animator.GetCurrentAnimatorStateInfo(0).length;
        }
        yield return new WaitForSeconds(length);
        EffectPool.Instance.Release(this);
    }
}
