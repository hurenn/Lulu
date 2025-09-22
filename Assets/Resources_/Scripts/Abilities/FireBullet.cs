using UnityEngine;

public class FireBullet : AutoDestroy
{
    // 直進速度
    [SerializeField]
    private float _speed = 10.0f;

    // 当たり判定
    [SerializeField]
    private DamageZone _damageZone;

    // 進行方向
    private bool _isRight = true;
    public bool IsRight { set { _isRight = value; } }

    private void Awake() {
        _damageZone.Setup((character) => {
            // 当たった敵をロックオン
            Enemy_Base enemy = character as Enemy_Base;
            LockonManager.Instance.SetTarget(enemy);
        });
    }

    private void FixedUpdate() {
        // 右に直進
        transform.position += transform.right * _speed * (_isRight ? 1 : -1);
    }
}
