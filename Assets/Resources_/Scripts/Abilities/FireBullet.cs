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
        // 当たった敵をロックオン(シールド等で防がれた時も、相手が許していればロックオンする)
        _damageZone.Setup(_Lockon);
        _damageZone.SetBlockedCallback(character => {
            if (character is IDamageSourceBlocker blocker && blocker.IsLockonOnBlocked) _Lockon(character);
        });
    }

    private static void _Lockon(IDamageable character) {
        Enemy_Base enemy = character as Enemy_Base;
        LockonManager.SetTargetStatic(enemy);
    }

    private void FixedUpdate() {
        // 右に直進
        transform.position += transform.right * _speed * Time.fixedDeltaTime * (_isRight ? 1 : -1);
    }
}
