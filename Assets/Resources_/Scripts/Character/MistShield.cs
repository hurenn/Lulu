using UnityEngine;

// Zelxの防御シールド。FireBulletが当たると徐々に小さくなり、時間経過で最大サイズまで回復する
public class MistShield : MonoBehaviour {
    [SerializeField] private float _shrinkPerHit = 0.1f; // 1発あたりの縮小量(初期サイズに対する割合)
    [SerializeField] private float _minScaleRate = 0.2f; // 最小サイズ(初期サイズに対する割合)
    [SerializeField] private float _regenSpeed = 0.05f;  // 1秒あたりの回復量(初期サイズに対する割合)
    [SerializeField] private GameObject _mistBurstPrefab; // 被弾時に発生させるパーティクル
    [SerializeField] private AudioClip _seHit;             // 被弾時の効果音
    [SerializeField] private float _seHitVolume = 1f;

    [SerializeField] private Collider2D _collider; // 壁反射判定用にサイズを取得するためのCollider

    private Vector3 _maxScale;
    private float _currentScaleRate = 1f; // 初期サイズに対する現在の比率(0~1)
    private bool _isInvincible = false; // true中は弾を消すだけで縮小・エフェクトなし

    private void Awake() {
        _maxScale = transform.localScale;
        if (_collider == null) _collider = GetComponent<Collider2D>();
    }

    // 爆発のたびに最大サイズを恒久的に拡大する
    public void GrowMaxSize(float multiplier) {
        _maxScale *= multiplier;
        transform.localScale = _maxScale * _currentScaleRate;
    }

    // 現在の実際のワールド空間でのサイズ(移動範囲の壁反射判定で使用)
    public Vector2 GetWorldSize() {
        return _collider != null ? (Vector2)_collider.bounds.size : Vector2.zero;
    }

    // 無敵モードの切り替え(true中はFireBulletが当たっても縮小・エフェクトなしで弾だけ消す)
    public void SetInvincible(bool enable) {
        _isInvincible = enable;
    }

    private void Update() {
        if (_currentScaleRate < 1f) {
            _currentScaleRate = Mathf.Min(1f, _currentScaleRate + _regenSpeed * Time.deltaTime);
            transform.localScale = _maxScale * _currentScaleRate;
        }
    }

    // サイズ・縮小状態を完全に最大まで戻す(スタンからの復活時などに使用)
    public void ResetToFull() {
        _currentScaleRate = 1f;
        transform.localScale = _maxScale;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // FireBulletのColliderは本体ではなく子オブジェクト(Damage)にあるため、親から探す
        var bullet = other.GetComponentInParent<FireBullet>();
        if (bullet == null) return;

        if (_seHit != null) {
            AudioUtil.PlayClipAtPointUnclamped(_seHit, transform.position, _seHitVolume);
        }

        if (_isInvincible) {
            // 無敵中は縮小・エフェクトなしで弾だけ消す
            Destroy(bullet.gameObject);
            return;
        }

        _currentScaleRate = Mathf.Max(_minScaleRate, _currentScaleRate - _shrinkPerHit);
        transform.localScale = _maxScale * _currentScaleRate;

        if (_mistBurstPrefab != null) {
            Instantiate(_mistBurstPrefab, other.transform.position, Quaternion.identity);
        }

        Destroy(bullet.gameObject);
    }
}
