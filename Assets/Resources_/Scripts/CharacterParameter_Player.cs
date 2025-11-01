using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーキャラクターのパラメーター管理
/// </summary>
public class CharacterParameter_Player : CharacterParameter {
    private const float _warpCost = 30.0f; // ワープに必要なMP
    private const float _iceCost = 10.0f; // 氷の能力に必要なMP
    private const float _lockonSlash = 30.0f; // ロックオン攻撃に必要なMP
    private const float _fireCost = 5.0f; // 炎の能力に必要なMP
    private const float _lightCost = 15.0f; // 光の能力に必要なMP
    private const float _lightAvoidCost = 5.0f; // 回避時に必要なMP
    private const float _overheatRecoveryTime = 3.0f; // オーバーヒート回復時間

    // 光能力による無敵フラグ
    public bool isLightInvincible = false;

    // MP
    private float _defaultMaxMP = 100.0f;
    private float _maxMP = 100.0f;
    private float _viewMP = 0.0f;

    // MP全快アニメ再生制御
    private float _waitViewMpGauge = 0.05f;
    private float _currentWaitViewMpGauge = 0.0f;

    private float _addMaxMP = 0.0f;
    public void SetPlusMaxMp(float plus_mp, bool recover = true) {
        var set_max_mp = _defaultMaxMP + plus_mp;
        _addMaxMP += set_max_mp - _maxMP;
        if (recover) {
            _currentMP = _maxMP;
        }
    }

    private bool _isViewMpFilled = false;
    // MPゲージが非表示になるまでの時間
    private float _mpHideTime = 1.0f;
    private float _currentMpHideTime = 0f;

    private float _currentMP { get; set; }
    // MPが最大かどうか
    public bool isMaxMP => _currentMP >= _maxMP;
    // オーバーヒートからの回復時間
    private float _overheatRecoverRate => (_defaultMaxMP / _overheatRecoveryTime) * Time.deltaTime;

    // オーバーヒート中かどうか
    public bool isOverheat { get; set; }
    // MP回復不可タイマー
    private float _currentUnRecoverableTime_MP = 0.0f;
    public void SetUnRecoverTime_MP(float time) {
        _currentUnRecoverableTime_MP = time;
    }

    // MP背景
    [SerializeField] private GameObject _mpBackground;
    // MP表示UI
    [SerializeField] private Image _mpImage;
    [SerializeField] private Image _mpImage2;
    [SerializeField] private Image _mpImage3;
    // MPゲージアニメーション
    [SerializeField] private Animator _mpFilled;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seMpRecover;

    protected override void _Update() {
        base._Update();

        // オーバーヒートタイマーの更新
        _UpdateOverheatTimer();
        // MP回復不可タイマーの更新
        if (_currentUnRecoverableTime_MP > 0) {
            _currentUnRecoverableTime_MP -= Time.deltaTime;
        }

        // MPゲージ表示待ち時間の更新
        if (_currentWaitViewMpGauge > 0) {
            _currentWaitViewMpGauge -= Time.deltaTime;
        }

        // レベルアップ時の最大MP上昇処理
        if (_addMaxMP > 0) {
            _maxMP += 1.0f;
            _currentMP = Mathf.Min(_currentMP + 1.0f, _maxMP);
            _addMaxMP = Mathf.Max(0, _addMaxMP - 1.0f);

            // MPゲージ表示
            _mpBackground.SetActive(true);

            // 非表示タイマーリセット
            _currentMpHideTime = 0;
            // MPゲージの更新
            _UpdateMPUI();
        }

        // MPゲージ非表示タイマーの更新
        if (_mpBackground != null && _mpBackground.activeSelf) {
            if (_isViewMpFilled) {
                if (_currentMpHideTime < _mpHideTime) {
                    _currentMpHideTime += Time.deltaTime;
                } else {
                    // MPゲージ非表示
                    _mpBackground.SetActive(false);
                }
            } else {
                // 非表示タイマーリセット
                _currentMpHideTime = 0;
            }
        }

        // 表示MPの更新
        if (_viewMP != _currentMP || _currentMP < _maxMP) {
            _UpdateMPUI();
        }
    }

    /// <summary>
    /// オーバーヒートタイマーの更新
    /// </summary>
    private void _UpdateOverheatTimer() {
        if (isOverheat) {
            _currentMP = Mathf.Clamp(_currentMP + _overheatRecoverRate, 0, _maxMP);
            if (_currentMP >= _defaultMaxMP) {
                // オーバーヒート解除
                isOverheat = false;
                _currentMP = _maxMP;
            }
            _UpdateMPUI();
        }
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
            case eAbilityType.LockonSlash:
                DecreaseMP(_lockonSlash);
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

    public void DecreaseMP(float amount) {
        if (_currentMP >= _maxMP) {
            _currentWaitViewMpGauge = _waitViewMpGauge;
        }

        _currentMP -= amount;
        if (_currentMP < 0) {
            // オーバーヒート処理
            _currentMP = 0;
            isOverheat = true;
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
        if (_currentMP > _maxMP) {
            _currentMP = _maxMP;
        }

        // MPゲージの更新
        _UpdateMPUI();
        return true;
    }
    public bool RecoverMP() {
        return RecoverMP(_maxMP);
    }
    public void OnRecoverOverheat() {
        isOverheat = false;
        _currentMP = _maxMP;
        _UpdateMPUI();
    }
    public void AddMaxMP(float amount) {
        _maxMP += amount;
    }

    /// <summary>
    /// MP UIの更新
    /// </summary>
    private void _UpdateMPUI() {
        if (_currentWaitViewMpGauge > 0) {
            // MP全快アニメ待ち中は更新しない
            return;
        }

        // 表示用MPの更新
        if (_viewMP != _currentMP) {
            _viewMP = Mathf.MoveTowards(_viewMP, _currentMP, 600.0f * Time.deltaTime);
            if (_viewMP < _maxMP) {
                _isViewMpFilled = false;
            }
        }

        // MPが最大でない場合はゲージを表示
        if (_mpBackground != null && _viewMP < _maxMP) {
            _mpBackground.SetActive(true);
        }

        // MPゲージの更新

        // 第1段階: 基本MP (0 - _defaultMaxMP)
        if (_mpImage != null) {
            _mpImage.fillAmount = Mathf.Clamp01(_viewMP / _defaultMaxMP);
        }

        // 第2段階: 拡張MP1 (_defaultMaxMP - _defaultMaxMP*2)
        if (_mpImage2 != null) {
            if (_viewMP > _defaultMaxMP) {
                float excess1 = _viewMP - _defaultMaxMP;
                _mpImage2.fillAmount = Mathf.Clamp01(excess1 / _defaultMaxMP);
            } else {
                _mpImage2.fillAmount = 0f;
            }
        }

        // 第3段階: 拡張MP2 (_defaultMaxMP*2 - _defaultMaxMP*3)
        if (_mpImage3 != null) {
            if (_viewMP > _defaultMaxMP * 2) {
                float excess2 = _viewMP - (_defaultMaxMP * 2);
                _mpImage3.fillAmount = Mathf.Clamp01(excess2 / _defaultMaxMP);
            } else {
                _mpImage3.fillAmount = 0f;
            }
        }

        // ゲージの色変更（オーバーヒート中は赤、それ以外は白）
        Color targetColor = isOverheat ? Color.red : Color.white;
        if (_mpImage.color != targetColor) {
            _mpImage.color = targetColor;
        }
        if (_mpImage2 != null && _mpImage2.color != targetColor) {
            _mpImage2.color = targetColor;
        }
        if (_mpImage3 != null && _mpImage3.color != targetColor) {
            _mpImage3.color = targetColor;
        }

        // MP全快アニメーション判定
        if (_viewMP >= _maxMP && !_isViewMpFilled && _addMaxMP <= 0) {
            _isViewMpFilled = true;
            if (_currentWaitViewMpGauge <= 0) {
                // MP全快アニメ
                _mpFilled.Play("MP_Filled", 0, 0f);
                // 回復SE再生
                if (_audioSource != null && _seMpRecover != null) {
                    _audioSource.PlayOneShot(_seMpRecover);
                }
            }
        }
    }
}
