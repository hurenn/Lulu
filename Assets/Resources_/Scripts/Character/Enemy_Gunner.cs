using System.Collections;
using UnityEngine;

// 一定間隔でチャージしてから正面方向に直進弾を発射するAI
public class Enemy_Gunner : Enemy_Base {
    [Header("射撃AIパラメータ")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _muzzle;          // 弾の発射位置(未設定なら自身の位置)
    [SerializeField] private float _fireInterval = 3.0f;  // 発射間隔(チャージ開始までの待ち時間)
    [SerializeField] private float _chargeTime = 1.0f;    // 発射前のチャージ時間
    [SerializeField] private AudioClip _seCharge;          // 射撃準備開始時の効果音
    [SerializeField] private AudioClip _seShoot;           // 発射時の効果音

    private float _fireTimer;
    private bool _isCharging;
    private Transform _playerTransform;

    protected override void _Setup() {
        base._Setup();
        _fireTimer = _fireInterval;
        _playerTransform = (PlayerCharacterManager.Current as Player_Character)?.transform;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();

        if (_isDead || _damageReactionTimer > 0) {
            return;
        }

        // チャージ中は狙いを固定するため、それ以外の間は近くのプレイヤーの方に振り向く
        if (!_isCharging && _playerTransform != null) {
            _isRight = _playerTransform.position.x > transform.position.x;
        }

        if (_isCharging) {
            return;
        }

        _fireTimer -= Time.deltaTime;
        if (_fireTimer <= 0f) {
            _fireTimer = _fireInterval;
            StartCoroutine(_ChargeAndFire());
        }
    }

    private IEnumerator _ChargeAndFire() {
        _isCharging = true;
        _anim.Play("Shoot_Charge");
        if (_seCharge != null) _audioSource.PlayOneShot(_seCharge);
        yield return new WaitForSeconds(_chargeTime);

        // チャージ中に倒された場合は発射しない
        if (_isDead) {
            _isCharging = false;
            yield break;
        }

        _anim.Play("Shoot");
        if (_seShoot != null) _audioSource.PlayOneShot(_seShoot);
        _FireBullet();

        _isCharging = false;
    }

    private void _FireBullet() {
        if (_bulletPrefab == null) return;

        Vector3 spawnPos = _muzzle != null ? _muzzle.position : transform.position;
        var bulletObj = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);
        var bullet = bulletObj.GetComponent<EnemyBullet>();
        bullet?.SetDirection(_isRight ? Vector2.right : Vector2.left);
    }
}
