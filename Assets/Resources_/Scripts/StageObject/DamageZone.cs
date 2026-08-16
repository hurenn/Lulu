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

    // トラップなどの環境ダメージ扱いにするか（氷の能力によるワープ無敵を貫通してダメージを与える）
    [SerializeField] private bool _isTrapDamage = false;

    [Header("Self Damage Settings")]
    // このDamageZoneの所有者（設定した場合、ダメージを与えた時に自分にもダメージを与える）
    [SerializeField] private Character_Base _selfCharacter = null;
    // 自分自身に与えるダメージ量
    [SerializeField] private int _damageToSelf = 0;
    // 自傷ダメージの無敵時間
    [SerializeField] private float _invincibleTimeToSelf = 0.5f;
    // 自分自身への吹っ飛ばし力
    [SerializeField] private Vector2 _blowPowerToSelf = new Vector2(3.0f, 5.0f);
    // 自分自身にダメージを与えるかどうか
    [SerializeField] private bool _enableSelfDamage = false;

    // ヒット済みキャラの管理
    Dictionary<Character_Base, float> _hitCharacters = new Dictionary<Character_Base, float>();
    private const float _hitInterval = 0.5f; // 同じキャラに連続ヒットさせない時間

    // ヒットエフェクト生成用
    [SerializeField] private HitEffect _hitEffectPrefab = null;
    [SerializeField] private HitEffect.eType _hitEffectType = HitEffect.eType.Normal;
    [SerializeField] private float _hitEffectSize = 1.0f;

    // ヒットストップ設定
    [SerializeField] private List<LocalTimePause> _localTimePauses = new List<LocalTimePause>();
    [SerializeField] private float _hitStopTime = 0.1f;

    [SerializeField] private bool _manualCameraShake = false;
    [SerializeField] private float _manualShakeIntensity = 0.5f;
    [SerializeField] private float _manualShakeDuration = 0.5f;
    [SerializeField] private float _hitCameraShakeIntensity = 0f;

    [SerializeField] private GameObject[] _hitPlayerEvent;    // ヒット時に発生させるイベントオブジェクト
    [SerializeField] private GameObject[] _avoidPlayerEvent;  // 回避時に発生させるイベントオブジェクト

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

    /// <summary>
    /// 自分自身のキャラクターを設定
    /// </summary>
    /// <param name="character">このDamageZoneの所有者</param>
    public void SetSelfCharacter(Character_Base character) {
        _selfCharacter = character;
    }

    // Update is called once per frame
    void Update() {
        // 手動カメラシェイク
        if (_manualCameraShake && _manualShakeIntensity > 0f) {
            _manualCameraShake = false;
            var cinemachineManager = CinemachineManager.Instance;
            cinemachineManager.ShakeCamera(_manualShakeIntensity, _manualShakeDuration);
        }

        if (_hitCharacters.Count > 0) {
            var keys = new List<Character_Base>(_hitCharacters.Keys);
            foreach (var key in keys) {
                if (_hitCharacters[key] > 0) {
                    _hitCharacters[key] -= Time.deltaTime;
                    if (_hitCharacters[key] <= 0) {
                        _hitCharacters.Remove(key);
                    }
                } else if (_hitCharacters[key] < 0) {
                    // 一度だけヒットの場合はタイマーを更新しない
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other) {
        if (!_isEnable) {
            return;
        }
        _OnDamage(other);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        if (!_isEnable) {
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
        if (character.isInvincible || _hitCharacters.ContainsKey(character)) {
            return;
        }
        // ヒット済みキャラのタイマー更新
        _hitCharacters.Add(character, _isOnceHit ? -1 : _hitInterval);

        var blow_power = _blowPowerRight;
        if (other.transform.position.x < transform.position.x) {
            blow_power.x = -blow_power.x;
        }

        // ヒット時のコールバック実行
        _hitCallback?.Invoke(character);

        var damage_result = character.Damage(damage, blow_power, _invincibleTime, _damageReactionTime, _isTrapDamage);

        // ダメージ演出
        if (damage_result) {
            // ヒットエフェクト生成
            var hit_effect = _SpawnHitEffect(other.transform.position, _hitEffectType);
            var target_effect = hit_effect?.GetComponent<LocalTimePause>();

            // ヒットストップ
            LocalTimePause hit_target = other.GetComponent<LocalTimePause>();
            _HitStop(hit_target, target_effect);

            // 自分自身にもダメージを与える
            if (_enableSelfDamage && _selfCharacter != null && _damageToSelf > 0) {
                _ApplySelfDamage(other.transform.position);
            }
        }
                
        // プレイヤーヒット状態に応じたイベント発生
        bool isAvoid = false;
        Player_Character player = other.GetComponent<Player_Character>();
        if (player != null) {
            isAvoid = player.PlayerCharaParam.isLightInvincible || player.PlayerCharaParam.isAutoLightInvincible;
        }
        foreach (var obj in _avoidPlayerEvent) {
            if (obj != null) obj.SetActive(isAvoid);
        }
        foreach (var obj in _hitPlayerEvent) {
            if (obj != null) obj.SetActive(!isAvoid);
        }

        if (_isHitDestroy && _destroyObject != null) {
            Destroy(_destroyObject);
        }
    }

    /// <summary>
    /// 自分自身にダメージを与える
    /// </summary>
    /// <param name="hitPosition">ヒットした相手の位置</param>
    private void _ApplySelfDamage(Vector3 hitPosition) {
        if (_selfCharacter == null || _selfCharacter.isInvincible) {
            return;
        }

        // 相手の位置に応じて吹っ飛ばし方向を決定
        var blowPower = _blowPowerToSelf;
        if (hitPosition.x < _selfCharacter.transform.position.x) {
            // 相手が左側にいる場合、自分は右に吹っ飛ぶ
            blowPower.x = Mathf.Abs(blowPower.x);
        } else {
            // 相手が右側にいる場合、自分は左に吹っ飛ぶ
            blowPower.x = -Mathf.Abs(blowPower.x);
        }

        // 自分自身にダメージを与える
        _selfCharacter.Damage(_damageToSelf, blowPower, _invincibleTimeToSelf, _damageReactionTime);
    }

    private GameObject _SpawnHitEffect(Vector3 position, HitEffect.eType type) {
        if (_hitEffectPrefab == null) return null;

        var effect = EffectPool.Instance.Spawn(_hitEffectPrefab.gameObject, position);
        if (effect != null) {
            var hitEffects = effect.GetComponentsInChildren<HitEffect>();
            foreach (var hitEffect in hitEffects) {
                hitEffect.Setup(type, _hitEffectSize);
            }
        }
        return effect.gameObject;
    }

    private void _HitStop(LocalTimePause hit_target, LocalTimePause hit_effect) {
        if (hit_effect) _localTimePauses.Add(hit_effect);
        if (hit_target) _localTimePauses.Add(hit_target);
        // ヒットストップ実行
        foreach (var pause in _localTimePauses) {
            pause?.StartPause(_hitStopTime);
        }
        // カメラシェイク
        if (_hitCameraShakeIntensity > 0) {
            var cinemachineManager = CinemachineManager.Instance;
            cinemachineManager.ShakeCamera(_hitCameraShakeIntensity, 0.05f);
        }
        _localTimePauses.Remove(hit_effect);
        _localTimePauses.Remove(hit_target);
    }

    public void AddHitStopTarget(LocalTimePause target) {
        if (!_localTimePauses.Contains(target)) {
            _localTimePauses.Add(target);
        }
    }
}
