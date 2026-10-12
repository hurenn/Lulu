using System;
using UnityEngine;

/// <summary>
/// 炎のゼルクスが発射する弾。プールで再利用する
/// 指定方向へ直進し、画面外に出る・プレイヤーに当たる・WaterBulletを受けるとプールへ返却される
/// </summary>
public class ZelxFireBullet : MonoBehaviour {
    [SerializeField] private float _speed = 6.0f;
    [SerializeField] private DamageZone _damageZone;

    [Header("WaterBullet被弾時")]
    [SerializeField] private HitEffect _waterHitEffectPrefab;  // ダメージエフェクト
    [SerializeField] private HitEffect.eType _waterHitEffectType = HitEffect.eType.Normal;
    [SerializeField] private float _waterHitEffectSize = 1.0f;
    [SerializeField] private AudioClip _seWaterHit;            // 効果音
    [SerializeField] private float _seWaterHitVolume = 1.0f;

    private Vector2 _direction = Vector2.down;
    private Action<ZelxFireBullet> _onRelease;
    private bool _isReleased = false;

    private void Awake() {
        // プレイヤーに当たったら消える(DamageZoneの_isHitDestroyで判定するため、光の能力で回避された時は消えない)
        _damageZone?.SetHitDestroyCallback(_Release);
    }

    /// <summary>
    /// 発射（プールから取り出すたびに呼ぶ）
    /// </summary>
    /// <param name="direction">進行方向</param>
    /// <param name="on_release">返却時のコールバック</param>
    public void Fire(Vector2 direction, Action<ZelxFireBullet> on_release) {
        _direction = direction.normalized;
        _onRelease = on_release;
        _isReleased = false;
        _damageZone?.ResetHitCharacters();
    }

    private void Update() {
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        _CheckOffScreen();
    }

    // 画面外に出たらプールへ返却
    private void _CheckOffScreen() {
        var cam = Camera.main;
        if (cam == null) return;

        var viewport_pos = cam.WorldToViewportPoint(transform.position);
        if (viewport_pos.x < 0f || viewport_pos.x > 1f || viewport_pos.y < 0f || viewport_pos.y > 1f) {
            _Release();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (_isReleased) return;
        if (other.GetComponentInParent<WaterBullet>() == null) return;

        // WaterBulletを受けたらダメージエフェクトと効果音を出して消える
        if (_waterHitEffectPrefab != null) {
            var effect = EffectPool.Instance.Spawn(_waterHitEffectPrefab.gameObject, transform.position);
            if (effect != null) {
                foreach (var hit_effect in effect.GetComponentsInChildren<HitEffect>()) {
                    hit_effect.Setup(_waterHitEffectType, _waterHitEffectSize);
                }
            }
        }
        if (_seWaterHit != null) {
            AudioUtil.PlayClipAtPointUnclamped(_seWaterHit, transform.position, _seWaterHitVolume);
        }
        _Release();
    }

    private void _Release() {
        if (_isReleased) return;
        _isReleased = true;

        if (_onRelease != null) {
            _onRelease(this);
        } else {
            Destroy(gameObject);
        }
    }
}
