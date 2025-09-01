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

    public float attackPower = 1.0f;
    public float damageInvincibilityTime = 0.1f; // ダメージ無敵時間

    private float _currentHP = 10;
    private float _currentInvincibilityTimer = 0;
    public bool isInvincible => _currentInvincibilityTimer > 0;

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
        switch (ability_type) {
            case eAbilityType.Warp:
                if (_currentMP >= _warpCost) {
                    DecreaseMP(_warpCost);
                    return true;
                }
                break;
            case eAbilityType.Ice:
                if (_currentMP >= _iceCost) {
                    DecreaseMP(_iceCost);
                    return true;
                }
                break;
            case eAbilityType.Fire:
                if (_currentMP >= _fireCost) {
                    DecreaseMP(_fireCost);
                    return true;
                }
                break;
            case eAbilityType.Light:
                if (_currentMP >= _lightCost) {
                    DecreaseMP(_lightCost);
                    return true;
                }
                break;
        }
        return false;
    }
    
    public void DecreaseMP(float amount)
    {
        _currentMP -= amount;
        if (_currentMP < 0) _currentMP = 0;

        // MPゲージの更新
        _UpdateMPUI();
    }
    public void IncreaseMP(float amount)
    {
        _currentMP += amount;
        if (_currentMP > _maxMP) _currentMP = _maxMP; // Assuming 100 is the max MP

        // MPゲージの更新
        _UpdateMPUI();
    }
    public void RecoverMP() {
        IncreaseMP(_maxMP);
    }
    public void AddMaxMP(float amount) {
        _maxMP += amount;
    }

    /// <summary>
    /// MP UIの更新
    /// </summary>
    private void _UpdateMPUI() {
        // MP非表示コルーチンを止める

        // MPが最大でない場合はゲージを表示
        if (_mpBackground != null && _currentMP < _maxMP) {
            _mpBackground.SetActive(true);
        }

        // MPゲージの更新
        if (_mpImage != null) {
            _mpImage.fillAmount = _currentMP / _maxMP;
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
