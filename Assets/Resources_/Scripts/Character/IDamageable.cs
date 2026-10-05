using UnityEngine;

// DamageZoneからダメージを受け取れるオブジェクトが実装するインターフェース
public interface IDamageable {
    bool isInvincible { get; }
    bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false);
}
