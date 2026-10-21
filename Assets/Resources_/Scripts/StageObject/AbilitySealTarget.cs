using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 能力の攻撃(FireBullet・Slashなど)に付け、能力が使えないエリア(AbilitySealArea)に触れたら消す
/// </summary>
public class AbilitySealTarget : MonoBehaviour {
    private Collider2D[] _colliders;
    private ContactFilter2D _filter;
    private static readonly List<Collider2D> _results = new List<Collider2D>();

    private void Awake() {
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _filter = new ContactFilter2D { useTriggers = true };
        _filter.SetLayerMask(AbilitySealArea.LayerMask);
    }

    private bool _isSealed = false;

    private void FixedUpdate() {
        if (!_isSealed && _IsTouchingArea()) _Seal();
    }

    private bool _IsTouchingArea() {
        bool has_collider = false;
        foreach (var col in _colliders) {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
            has_collider = true;
            if (col.Overlap(_filter, _results) > 0) return true;
        }
        // 当たり判定の無い物(見た目だけの弾など)は位置で判定する
        return !has_collider && AbilitySealArea.Contains(transform.position);
    }

    private void _Seal() {
        _isSealed = true;
        var auto_destroy = GetComponent<AutoDestroy>();
        if (auto_destroy != null) {
            auto_destroy.DestroyNow();
        } else {
            Destroy(gameObject);
        }
    }
}
