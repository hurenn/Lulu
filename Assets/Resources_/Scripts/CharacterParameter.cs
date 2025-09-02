using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class CharacterParameter : MonoBehaviour
{
    public enum eAbilityType
    {
        None,
        Ice,
        Fire,
        Light,
        Warp,
    }
    private const float _warpCost = 30.0f; // ワープに必要なMP
    private const float _iceCost = 10.0f; // 氷の能力に必要なMP
    private const float _fireCost = 5.0f; // 炎の能力に必要なMP
    private const float _lightCost = 30.0f; // 光の能力に必要なMP

    private float _maxHP = 3.0f;
    private float _currentMP = 100.0f;
    [SerializeField]
    private float _maxMP = 100.0f;
    // MPが最大かどうか
    public bool isMaxMP => _currentMP >= _maxMP;
    // オーバーヒートからの回復時間
    private float _overheatRecoverTime => _maxMP / 100.0f * 3.0f;
    private float _currentOverheatTimer = 0.0f;
    // オーバーヒート中かどうか
    public bool isOverheat => _currentOverheatTimer > 0;

    public float attackPower = 1.0f;
    public float damageInvincibilityTime = 0.1f; // ダメージ無敵時間

    private float _currentHP = 10;
    private float _currentInvincibilityTimer = 0;
    public bool isInvincible => _currentInvincibilityTimer > 0;

    // MP回復不可タイマー
    private float _currentUnRecoverableTime_MP = 0.0f;
    public void AddUnRecoverableTime_MP(float time) {
        _currentUnRecoverableTime_MP += time;
    }

    // キャラクター表示
    [SerializeField] private SpriteRenderer _rend;
    // MP背景
    [SerializeField] private GameObject _mpBackground;
    // MP表示UI
    [SerializeField] private Image _mpImage;
    // MPゲージアニメーション
    [SerializeField] private Animator _mpFilled;
    private IEnumerator _mpHideCoroutine = null;

    private void Start()
    {
        _currentHP = _maxHP;
    }

    private void Update() {
        // ダメージ無敵時間の更新
        if (_currentInvincibilityTimer > 0) {
            _currentInvincibilityTimer -= Time.deltaTime;
            // 無敵時間中はキャラクターを点滅させる
            float alpha = Mathf.PingPong(Time.time * 5, 1);
            _rend.color = new Color(1, 1, 1, alpha);
        } else if (_rend.color != Color.white) {
            _rend.color = Color.white;
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
    public void ExecuteDamage(float damage)
    {
        _currentHP -= damage;
        _currentInvincibilityTimer = damageInvincibilityTime;
        if (_currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // キャラクターが死亡したときの処理
        Debug.Log("Character died.");
        // 例: ゲームオブジェクトを非アクティブにする
        gameObject.SetActive(false);
    }

    /// <summary>
    /// MPが足りているか確認
    /// </summary>
    /// <param name="ability_type"></param>
    /// <returns></returns>
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
    public void RecoverMP(float amount) {
        if (isOverheat || _currentUnRecoverableTime_MP > 0) {
            // 回復不可判定
            return;
        }
        _currentMP += amount;
        if (_currentMP > _maxMP) _currentMP = _maxMP; // Assuming 100 is the max MP

        // MPゲージの更新
        _UpdateMPUI();
    }
    public void RecoverMP() {
        RecoverMP(_maxMP);
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
}
