using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterParameter : MonoBehaviour {
    // 共通パラメータ
    protected CommonParameter _param;

    private const float _warpCost = 30.0f; // ワープに必要なMP
    private const float _iceCost = 10.0f; // 氷の能力に必要なMP
    private const float _fireCost = 5.0f; // 炎の能力に必要なMP
    private const float _lightCost = 15.0f; // 光の能力に必要なMP
    private const float _lightAvoidCost = 5.0f; // 回避時に必要なMP

    // HP
    public int defaultMaxHP = 3;
    private int _maxHP = 3;
    public int MaxHP => _maxHP;
    public void SetMaxHP(int max_hp, bool recover = true) {
        _maxHP = max_hp;
        OnMaxHPChanged?.Invoke(_maxHP);

        if (recover) {
            CurrentHP = _maxHP;
        }
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
    public System.Action<int> OnExpChanged;

    // 無敵時間
    private float _currentInvincibilityTimer = 0;
    // 光能力による無敵フラグ
    public bool isLightInvincible = false;
    public bool isInvincible => _currentInvincibilityTimer > 0;

    // MP
    public float defaultMaxMP = 100.0f;
    private float _maxMP = 100.0f;
    public void SetMaxMP(float max_mp, bool recover = true) {
        _maxMP = max_mp;
        if (recover) {
            _currentMP = _maxMP;
            _UpdateMPUI();
        }
    }
    private float _currentMP = 100.0f;
    // MPが最大かどうか
    public bool isMaxMP => _currentMP >= _maxMP;
    // オーバーヒートからの回復時間
    private float _overheatRecoverTime => _maxMP / 100.0f * 3.0f;
    private float _currentOverheatTimer = 0.0f;
    // オーバーヒート中かどうか
    public bool isOverheat => _currentOverheatTimer > 0;

    // MP回復不可タイマー
    private float _currentUnRecoverableTime_MP = 0.0f;
    public void SetUnRecoverTime_MP(float time) {
        _currentUnRecoverableTime_MP = time;
    }

    // 攻撃力
    public int attackPower = 1;
    public int defaultAttackPower { get; private set; } = 1;

    // キャラクター表示
    [SerializeField] private SpriteRenderer _rend;
    private Color _originalColor;
    // MP背景
    [SerializeField] private GameObject _mpBackground;
    // MP表示UI
    [SerializeField] private Image _mpImage;
    // MPゲージアニメーション
    [SerializeField] private Animator _mpFilled;
    // MPゲージ非表示コルーチン
    private IEnumerator _mpHideCoroutine = null;

    private int _currentExp = 0;
    public int currentExp {
        get => _currentExp;
        set {
            _currentExp = value;
            if (_currentExp < 0) _currentExp = 0;
            OnExpChanged?.Invoke(_currentExp);
        }
    }
    private int _nextLevelExp = 100;
    public int nextLevelExp {
        get => _nextLevelExp;
        set {
            _nextLevelExp = value;
            if (_nextLevelExp < 1) _nextLevelExp = 1;
        }
    }

    public void Setup(CommonParameter param) {
        _param = param;
        _originalColor = _rend.color;
    }

    private void Start() {
        _maxHP = defaultMaxHP;
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
        // オーバーヒートタイマーの更新
        _UpdateOverheatTimer();
        // MP回復不可タイマーの更新
        if (_currentUnRecoverableTime_MP > 0) {
            _currentUnRecoverableTime_MP -= Time.deltaTime;
        }
    }

    /// <summary>
    /// オーバーヒートタイマーの更新
    /// </summary>
    private void _UpdateOverheatTimer() {
        if (isOverheat) {
            _currentOverheatTimer -= Time.deltaTime;
            _currentMP = _maxMP * (1.0f - _currentOverheatTimer / _overheatRecoverTime);
            _UpdateMPUI();
        }
    }

    /// <summary>
    /// ダメージ発生
    /// </summary>
    public void ExecuteDamage(int damage, float invincibility_time, ref bool is_die) {
        CurrentHP -= damage;
        _currentInvincibilityTimer = invincibility_time;

        is_die = CurrentHP <= 0;
    }

    /// <summary>
    /// MP消費
    /// </summary>
    /// <param name="ability_type"></param>
    public bool ConsumeMP(eAbilityType ability_type) {
        if (isOverheat) {
            // オーバーヒート中は使用不可
            return false;
        }

        switch (ability_type) {
            case eAbilityType.Warp:
                DecreaseMP(_warpCost);
                break;
            case eAbilityType.Ice:
                DecreaseMP(_iceCost);
                break;
            case eAbilityType.Fire:
                DecreaseMP(_fireCost);
                break;
            case eAbilityType.Light:
                DecreaseMP(_lightCost);
                break;
            case eAbilityType.LightAvoid:
                DecreaseMP(_lightAvoidCost);
                break;
        }
        return true;
    }
    
    public void DecreaseMP(float amount)
    {
        _currentMP -= amount;
        if (_currentMP < 0) {
            // オーバーヒート処理
            _currentMP = 0;
            _currentOverheatTimer = _overheatRecoverTime;
        }

        // MPゲージの更新
        _UpdateMPUI();
    }

    /// <summary>
    /// MP回復
    /// </summary>
    /// <param name="amount">回復値</param>
    /// <param name="force">強制回復フラグ</param>
    public bool RecoverMP(float amount, bool force = false) {
        if (!force && (isOverheat || _currentUnRecoverableTime_MP > 0)) {
            // 回復不可判定
            return false;
        }
        _currentMP += amount;
        if (_currentMP > _maxMP) _currentMP = _maxMP; // Assuming 100 is the max MP

        // MPゲージの更新
        _UpdateMPUI();
        return true;
    }
    public bool RecoverMP() {
        return RecoverMP(_maxMP);
    }
    public void OnRecoverOverheat() {
        _currentOverheatTimer = 0;
    }
    public void AddMaxMP(float amount) {
        _maxMP += amount;
    }

    /// <summary>
    /// MP UIの更新
    /// </summary>
    private void _UpdateMPUI() {
        // MPが最大でない場合はゲージを表示
        if (_mpBackground != null && _currentMP < _maxMP) {
            _mpBackground.SetActive(true);
        }

        // MPゲージの更新
        if (_mpImage != null) {
            _mpImage.fillAmount = _currentMP / _maxMP;

            // ゲージの色変更（オーバーヒート中は赤、それ以外は白）
            if (isOverheat && _mpImage.color != Color.red) {
                _mpImage.color = Color.red;
            } else if (_currentOverheatTimer <= 0 && _mpImage.color != Color.white) {
                _mpImage.color = Color.white;
            }
        }
        // MPが最大になった場合のアニメーション再生
        if (_mpFilled != null) {
            if (_currentMP >= _maxMP) {
                _mpFilled.Play("MP_Filled", 0, 0f);
                // 一定時間後にゲージを非表示にする
                _mpHideCoroutine = _HideMPGageRoutine();
                StartCoroutine(_mpHideCoroutine);
            } else {
                // 非表示コルーチンを止める
                if (_mpHideCoroutine != null) {
                    StopCoroutine(_mpHideCoroutine);
                    _mpHideCoroutine = null;
                }
            }
        }
    }

    /// <summary>
    /// MPゲージ非表示コルーチン
    /// </summary>
    private IEnumerator _HideMPGageRoutine() {
        yield return new WaitForSeconds(1.0f);
        if (_mpBackground != null) {
            _mpBackground.SetActive(false);
        }
        _mpHideCoroutine = null;
    }

    /// <summary>
    /// 経験値追加
    /// </summary>
    public void AddExp(int value) {
        currentExp += value;
        if (currentExp >= nextLevelExp) {
            currentExp -= nextLevelExp;
            nextLevelExp = (int)(nextLevelExp * 1.5f);
            // レベルアップ処理
            Levelup();
        }
    }

    /// <summary>
    /// レベルアップ実行
    /// </summary>
    /// <param name="level_type"></param>
    public void Levelup(PlayerParameter.eLevelType level_type = PlayerParameter.eLevelType.All) {
        // 対応するレベルを上げる
        var player_param = PlayerParameter.Instance;
        if (player_param != null) {
            switch (level_type) {
                case PlayerParameter.eLevelType.HP:
                    player_param.levelParameter.hpLevel++;
                    break;
                case PlayerParameter.eLevelType.MP:
                    player_param.levelParameter.mpLevel++;
                    break;
                case PlayerParameter.eLevelType.Attack:
                    player_param.levelParameter.attackLevel++;
                    break;
                case PlayerParameter.eLevelType.All:
                    player_param.levelParameter.hpLevel++;
                    player_param.levelParameter.mpLevel++;
                    player_param.levelParameter.attackLevel++;
                    break;
                default:
                    break;
            }
        }
        Debug.Log($"Levelup:{level_type.ToString()}");

        // レベルに応じたパラメータを適用
        ApplyPlayerParameter();
    }

    /// <summary>
    /// レベルに応じたパラメータを適用
    /// </summary>
    public void ApplyPlayerParameter() {
        var player_param = PlayerParameter.Instance;
        if (player_param != null) {
            SetMaxHP(defaultMaxHP + player_param.levelParameter.hpLevel);
            SetMaxMP(defaultMaxMP + player_param.levelParameter.mpLevel * _param.mpUpPerLevel);
            attackPower = defaultAttackPower + player_param.levelParameter.attackLevel * _param.attackUpPerLevel;
        }
    }

}
