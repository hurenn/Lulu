using UnityEngine;

// 攻撃で破壊できるブロック。プレイヤーは乗れるが、押して動かすことはできない
public class DestructibleBlock : MonoBehaviour, IDamageable {
    [SerializeField] private int _hp = 3; // 体力(UI表示はしない)
    [SerializeField] private Collider2D _col;
    [SerializeField] private Animator _anim;

    private bool _isBroken = false;

    public bool isInvincible => _isBroken;

    private void Reset() {
        _col = GetComponent<Collider2D>();
        _anim = GetComponent<Animator>();
    }

    private void Awake() {
        // 攻撃側(FireBullet等)もRigidbody2Dを持たないため、双方ともRigidbody2Dが無いと
        // Unityの2D物理がトリガー判定自体を発生させない。
        // StaticはRigidbody2D無しと同じ扱いになり判定が発生しないため、Kinematicで追加する
        // (Kinematicは物理的な力の影響を受けない＝動かされないが、衝突・トリガー判定は正しく発生する)
        if (GetComponent<Rigidbody2D>() == null) {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        if (_isBroken) {
            return false;
        }

        _hp -= damage;
        if (_hp <= 0) {
            _Break();
        }
        return true;
    }

    private void _Break() {
        _isBroken = true;

        // 当たり判定を即座に外す(アニメ再生中はすり抜けられるように)
        if (_col != null) _col.enabled = false;

        float delay = 0f;
        // Breakステートが設定されていれば再生し、その長さ分待ってから消える。
        // 未設定の場合はそのまま即座に消える
        if (_anim != null && _anim.HasState(0, Animator.StringToHash("Break"))) {
            _anim.Play("Break");
            delay = _anim.GetCurrentAnimatorStateInfo(0).length;
        }
        Destroy(gameObject, delay);
    }
}
