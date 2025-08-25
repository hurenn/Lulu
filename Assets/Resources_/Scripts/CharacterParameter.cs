using UnityEngine;

public class CharacterParameter : MonoBehaviour
{
    public float maxHP = 10.0f;
    public float MP = 100.0f;
    public float attackPower = 1.0f;
    public float damageInvincibilityTime = 0.1f; // ダメージ無敵時間

    private float _currentHP = 10;
    private float _currentInvincibilityTimer = 0;
    public bool isInvincible => _currentInvincibilityTimer > 0;

    // キャラクター表示
    [SerializeField] private SpriteRenderer _rend;

    private void Start()
    {
        _currentHP = maxHP;
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
}
