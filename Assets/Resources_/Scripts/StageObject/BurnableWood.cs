using UnityEngine;

/// <summary>
/// FireBulletが当たると燃える木
/// 消滅して、全く同じ位置にBurningWoodを配置する。FireBullet以外の攻撃は当たらない
/// </summary>
public class BurnableWood : MonoBehaviour, IDamageable, IDamageSourceFilter {
    [SerializeField] private GameObject _burningWoodPrefab; // 燃えた後に配置するプレハブ

    private bool _isBurnt = false;

    public bool isInvincible => _isBurnt;

    private void Awake() {
        // FireBulletもRigidbody2Dを持たないため、双方ともRigidbody2Dが無いとトリガー判定自体が発生しない。
        // DestructibleBlockと同様に、動かされないKinematicで追加する
        if (GetComponent<Rigidbody2D>() == null) {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // FireBullet以外の攻撃は当たらない
    public bool IsIgnoredDamageSource(DamageZone source) {
        return source.GetComponentInParent<FireBullet>() == null;
    }

    public bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        if (_isBurnt) {
            return false;
        }
        _isBurnt = true;

        if (_burningWoodPrefab != null) {
            Instantiate(_burningWoodPrefab, transform.position, transform.rotation, transform.parent);
        }
        Destroy(gameObject);
        return true;
    }
}
