using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour {
    // プレイヤーに与えるダメージ
    [SerializeField] private int _damageToPlayer = 0;
    // 敵に与えるダメージ
    [SerializeField] private int _damageToEnemy = 0;

    // 無敵時間
    [SerializeField] private int _invincibleTime = 0;

    // 吹っ飛ばす力（右向き）
    [SerializeField] private Vector2 _blowPowerRight = Vector2.zero;

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
    // 攻撃可能かどうか
    private bool _isAttakable = true;

    // ダメージ判定の有効無効
    private bool _isEnable = true;

    // ヒット時のコールバック
    private System.Action<Character_Base> _hitCallback = default;

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

        character.Damage(damage, blow_power, _invincibleTime, _damageReactionTime);
        _currentDelayTimer = _delayTime;
        _isAttakable = false;
        if (_isHitDestroy && _destroyObject != null) {
            Destroy(_destroyObject);
        }
    }
}
