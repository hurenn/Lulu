using System.Collections;
using UnityEngine;

/// <summary>
/// キャラクターのパラメーター管理
/// </summary>
public class CharacterParameter : MonoBehaviour {
    // HP
    [SerializeField] private int _defaultMaxHP = 3;
    public int defaultMaxHP => _defaultMaxHP;

    private int _maxHP = 3;
    public int MaxHP => _maxHP;
    public void SetPlusMaxHp(int plus_hp, bool recover = true) {
        _maxHP = _defaultMaxHP + plus_hp;
        StartCoroutine(_waitMaxHPChange());

        if (recover) {
            CurrentHP = _maxHP;
        }
    }
    // HPゲージセットアップができるまで待つ
    private IEnumerator _waitMaxHPChange() {
        while (OnMaxHPChanged == null) {
            yield return null;
        }
        OnMaxHPChanged.Invoke(_maxHP);
    }

    private int _currentHP = 3;
    public int CurrentHP {
        get => _currentHP;
        private set {
            _currentHP = Mathf.Clamp(value, 0, _maxHP);
            OnHPChanged?.Invoke(_currentHP);
        }
    }
    public System.Action<int> OnHPChanged;
    public System.Action<int> OnMaxHPChanged;

    // HPの下限(必殺技の演出中など、演出が終わるまで倒れないようにする)。0なら下限なし
    public int minHP { get; set; } = 0;

    // 無敵時間
    protected float _currentInvincibilityTimer = 0;
    public float currentInvincibilityTimer { set => _currentInvincibilityTimer = value; }
    public bool isInvincible => _currentInvincibilityTimer > 0;

    // 攻撃力
    public int attackPower = 1;
    public int defaultAttackPower { get; private set; } = 1;

    // キャラクター表示
    [SerializeField] private SpriteRenderer _rend;
    private Color _originalColor;

    public void Setup() {
        _originalColor = _rend.color;
        _maxHP = _defaultMaxHP;
        CurrentHP = _maxHP;
    }

    private void Update() {
        // ダメージ無敵時間の更新
        if (_currentInvincibilityTimer > 0) {
            _currentInvincibilityTimer -= Time.deltaTime;
            // 無敵時間中はキャラクターを点滅させる
            float alpha = Mathf.PingPong(Time.time * 5, 1);
            var set_color = _rend.color;
            set_color.a = alpha;
            _rend.color = set_color;
        } else if (_rend.color != _originalColor) {
            _rend.color = _originalColor;
        }

        _Update();
    }

    protected virtual void _Update() { }

    /// <summary>
    /// ダメージ発生
    /// </summary>
    public void ExecuteDamage(int damage, float invincibility_time, ref bool is_die) {
        // 下限より少なくはしない(既に下限を下回っている時は、それ以上減らさない)
        CurrentHP = Mathf.Max(CurrentHP - damage, Mathf.Min(minHP, CurrentHP));
        _currentInvincibilityTimer = invincibility_time;

        is_die = CurrentHP <= 0;
    }

    /// <summary>
    /// 回復発生
    /// </summary>
    public void RecoverHP(int recover_amount) {
        CurrentHP += recover_amount;
    }
}
