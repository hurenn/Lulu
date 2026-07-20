using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour {
    private static EffectPool _instance;
    public static EffectPool Instance {
        get {
            if (_instance == null) {
                _instance = FindAnyObjectByType<EffectPool>();
                if (_instance == null) {
                    GameObject obj = new GameObject("EffectPool");
                    _instance = obj.AddComponent<EffectPool>();
                }
            }
            return _instance;
        }
    }

    private readonly Dictionary<GameObject, Queue<PooledEffect>> _pools = new();

    public PooledEffect Spawn(GameObject prefab, Vector3 position, bool is_reverse = false, Quaternion rotation = default) {
        if (!_pools.TryGetValue(prefab, out var pool)) {
            pool = new Queue<PooledEffect>();
            _pools[prefab] = pool;
        }

        PooledEffect effect;
        if (pool.Count > 0) {
            effect = pool.Dequeue();
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.gameObject.SetActive(true);
        } else {
            var obj = Instantiate(prefab, position, rotation);
            effect = obj.GetComponent<PooledEffect>();
            if (effect == null) {
                Debug.LogWarning($"[EffectPool] {prefab.name} に PooledEffect コンポーネントがありません");
                return null;
            }
            effect.SetPrefabKey(prefab);
        }

        // 反転処理
        var scale = effect.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (is_reverse ? -1 : 1);
        effect.transform.localScale = scale;

        return effect;
    }

    public void Release(PooledEffect effect) {
        effect.gameObject.SetActive(false);
        if (effect.PrefabKey == null) {
            Debug.LogWarning($"[EffectPool] {effect.name} の PrefabKey が設定されていません");
            Destroy(effect.gameObject);
            return;
        }
        if (!_pools.TryGetValue(effect.PrefabKey, out var pool)) {
            pool = new Queue<PooledEffect>();
            _pools[effect.PrefabKey] = pool;
        }
        pool.Enqueue(effect);
    }
}
