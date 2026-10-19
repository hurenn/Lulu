using UnityEngine;

// 攻撃で破壊できるブロック。プレイヤーは乗れるが、押して動かすことはできない
public class DestructibleBlock : MonoBehaviour, IDamageable, IDamageSourceFilter {
    [SerializeField] private int _hp = 3; // 体力(UI表示はしない)
    [SerializeField] private bool _isIgnoreFireBullet = false; // trueならFireBulletが当たらない(燃えている物など)
    [SerializeField] private Collider2D _col;
    [SerializeField] private Animator _anim;
    [SerializeField] private AudioClip _seWaterHit; // WaterBulletが当たった時の効果音
    [SerializeField] private float _seWaterHitVolume = 1f;
    [SerializeField] private AudioClip _seBreak; // 破壊された時の効果音
    [SerializeField] private float _seBreakVolume = 1f;
    [SerializeField] private int _seBreakMaxCount = 2;        // 同時に鳴らす破壊音の上限(まとめて壊れた時に音が重なりすぎないようにする)
    [SerializeField] private float _seBreakCountWindow = 0.15f; // この時間内に鳴らした破壊音を「同時」とみなす

    // 最近鳴らした破壊音の時刻(全ブロックで共有)
    private static readonly System.Collections.Generic.List<float> _seBreakPlayTimes = new System.Collections.Generic.List<float>();

    [Header("破壊時のドロップ(未設定なら何も出さない)")]
    [SerializeField] private int _dropCoinCount = 0; // ドロップするコイン数(合計価値。50を超える場合は価値5のコインにまとめて生成される)
    [SerializeField] private GameObject[] _dropItems; // ドロップするアイテムのプレハブ

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

    /// <summary>
    /// WaterBulletのヒット時に呼ばれ、効果音を鳴らす
    /// （OnTriggerEnter2Dで判定すると、子オブジェクトのColliderもこのRigidbody2Dに属するため1発で複数回鳴ってしまう。
    ///  実際にダメージを与えた時のみ鳴らすため、WaterBulletのDamageZoneのヒットコールバックから呼ぶ）
    /// </summary>
    public void PlayWaterHitSE() {
        if (_isBroken) return;

        // 破壊時はDestroyで自身のAudioSourceも消えるため、PlayClipAtPointで独立に再生する
        if (_seWaterHit != null) {
            AudioUtil.PlayClipAtPointUnclamped(_seWaterHit, transform.position, _seWaterHitVolume);
        }
    }

    public bool IsIgnoredDamageSource(DamageZone source) {
        return _isIgnoreFireBullet && source.GetComponentInParent<FireBullet>() != null;
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

        _PlayBreakSE();

        _Drop();

        float delay = 0f;
        // Breakステートが設定されていれば再生し、その長さ分待ってから消える。
        // 未設定の場合はそのまま即座に消える
        if (_anim != null && _anim.HasState(0, Animator.StringToHash("Break"))) {
            _anim.Play("Break");
            delay = _anim.GetCurrentAnimatorStateInfo(0).length;
        }
        Destroy(gameObject, delay);
    }

    // 破壊音を鳴らす。まとめて壊れた時は、同時に鳴らす数を上限までに絞る
    private void _PlayBreakSE() {
        if (_seBreak == null) return;
        float now = Time.time;
        _seBreakPlayTimes.RemoveAll(time => now - time > _seBreakCountWindow || time > now); // 古い記録(シーンを読み直した時の記録も)を消す
        if (_seBreakPlayTimes.Count >= _seBreakMaxCount) return;
        _seBreakPlayTimes.Add(now);
        // 自身はすぐ削除されるため、PlayClipAtPointで独立に再生する
        AudioUtil.PlayClipAtPointUnclamped(_seBreak, transform.position, _seBreakVolume);
    }

    // 破壊時にコイン・アイテムをドロップする
    private void _Drop() {
        // 自身はすぐ削除されるため、一時的なCoinSpawnerで生成する
        CoinSpawner.SpawnCoinAt(transform.position, _dropCoinCount);

        if (_dropItems != null) {
            foreach (var item in _dropItems) {
                if (item != null) {
                    Instantiate(item, transform.position, Quaternion.identity);
                }
            }
        }
    }
}
