using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour {
    // プレイヤーに与えるダメージ
    [SerializeField] private int _damageToPlayer = 1;
    // 敵に与えるダメージ
    [SerializeField] private int _damageToEnemy = 100;

    // 無敵時間
    [SerializeField] private float _invincibleTime = 5;

    // 吹っ飛ばす力（右向き）
    [SerializeField] private Vector2 _blowPowerRight = new Vector2(10.0f, 5.0f);

    // 行動不能時間
    [SerializeField] private float _damageReactionTime = 0.2f;

    // ヒットしたら消えるかどうか
    [SerializeField] private bool _isHitDestroy = false;
    [SerializeField] private GameObject _destroyObject = null;

    // 一度だけダメージを与えるかどうか
    [SerializeField] private bool _isOnceHit = true;
    private List<GameObject> _hitObjects = new List<GameObject>();

    // 連続ダメージ判定のディレイ時間
    [SerializeField] private float _delayTime = 0.5f;
    private float _currentDelayTimer = 0;

    // ヒットエフェクト生成用
    [SerializeField] private HitEffect _hitEffectPrefab = null;
    [SerializeField] private HitEffect.eType _hitEffectType = HitEffect.eType.Normal;
    [SerializeField] private float _hitEffectSize = 1.0f;

    // ヒットストップ時間
    [SerializeField] private float _hitStopTime = 0.05f;
    // ヒットストップまでの遅延時間
    [SerializeField] private float _hitStopDelay = 0.01f;
    // ヒットストップの重さ（0.0f:完全停止、1.0f:通常速度）
    [SerializeField] private float _hitStop_Heavy = 0.0f;

    // 攻撃可能かどうか
    private bool _isAttakable = true;

    // ダメージ判定の有効無効
    private bool _isEnable = true;

    // ヒット時のコールバック
    private System.Action<Character_Base> _hitCallback = default;

    private void Reset() {
        _hitEffectPrefab = Resources.Load<HitEffect>("Prefabs/Effects/HitEffect");
    }

    /// <summary>
    /// セットアップ
    /// </summary>
    /// <param name="callback">ヒット時のコールバック設定</param>
    public void Setup(System.Action<Character_Base> callback) {
        _hitCallback = callback;
    }

    // Update is called once per frame
    void Update() {
        // 連続ダメージ判定のディレイ処理
        if (_currentDelayTimer > 0 && _isAttakable == false) {
            _currentDelayTimer -= Time.deltaTime;
        }
        if (_currentDelayTimer < 0 && _isAttakable == false) {
            _isAttakable = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other) {
        if (!_isEnable || !_isAttakable) {
            return;
        }
        _OnDamage(other);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        if (!_isEnable || !_isAttakable) {
            return;
        }

        _OnDamage(collision.collider);
    }

    private void _OnDamage(Collider2D other) {
        int damage = 0;

        // ダメージ量取得
        if (_damageToPlayer > 0 && other.gameObject.layer == LayerMask.NameToLayer("Player")) {
            damage = _damageToPlayer;
        }
        if (_damageToEnemy > 0 && other.gameObject.layer == LayerMask.NameToLayer("Enemy")) {
            damage = _damageToEnemy;
        }
        if (damage == 0) {
            return;
        }

        Character_Base character = other.GetComponent<Character_Base>();
        if (character == null) {
            return;
        }
        if (character.isInvincible || _hitObjects.Contains(other.gameObject)) {
            return;
        }

        if (_isOnceHit) {
            // ヒットした相手を記録しておく
            _hitObjects.Add(other.gameObject);
        }

        var blow_power = _blowPowerRight;
        if (other.transform.position.x < transform.position.x) {
            blow_power.x = -blow_power.x;
        }

        // ヒット時のコールバック実行
        _hitCallback?.Invoke(character);

        var damage_result = character.Damage(damage, blow_power, _invincibleTime, _damageReactionTime);

        // ダメージ演出
        if (damage_result) {
            // ヒットエフェクト生成
            _SpawnHitEffect(other.transform.position, _hitEffectType);
            // ヒットストップ
            StartCoroutine(_HitStopCoroutine());
        }

        _currentDelayTimer = _delayTime;
        _isAttakable = false;
        if (_isHitDestroy && _destroyObject != null) {
            Destroy(_destroyObject);
        }
    }

    private void _SpawnHitEffect(Vector3 position, HitEffect.eType type) {
        if (_hitEffectPrefab == null) return;

        var effect = Instantiate(_hitEffectPrefab, position, Quaternion.identity);
        effect.Setup(type, _hitEffectSize);
    }

    private IEnumerator _HitStopCoroutine() {
        yield return new WaitForSeconds(_hitStopDelay); // 遅延時間待つ

        float originalTimeScale = Time.timeScale;
        Time.timeScale = _hitStop_Heavy; // ストップ
        yield return new WaitForSecondsRealtime(_hitStopTime); // 実時間で待つ
        Time.timeScale = originalTimeScale; // 元に戻す
    }

}
