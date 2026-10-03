using UnityEngine;

public class WaterBullet : AutoDestroy
{
    // 初速
    [SerializeField]
    private float _speed = 15.0f;
    // 発射角度(度、水平から上向きが正)
    [SerializeField]
    private float _launchAngle = 37.5f;
    // 落下の重力加速度
    [SerializeField]
    private float _gravity = 25.0f;

    // 当たり判定
    [SerializeField]
    private DamageZone _damageZone;

    // 進行方向
    private bool _isRight = true;
    public bool IsRight { set { _isRight = value; } }

    // 発射者の移動速度(発射時に乗せる分)
    private Vector2 _inheritedVelocity = Vector2.zero;
    public void SetInheritedVelocity(Vector2 velocity) { _inheritedVelocity = velocity; }

    private Vector2 _velocity;

    private void Awake() {
        _damageZone.Setup((character) => {
            // 当たった敵をロックオン
            Enemy_Base enemy = character as Enemy_Base;
            LockonManager.SetTargetStatic(enemy);
        });
    }

    protected override void Start() {
        base.Start(); // AutoDestroyの寿命管理コルーチンを開始する

        float angleRad = _launchAngle * Mathf.Deg2Rad;
        float dir_x = _isRight ? 1f : -1f;
        _velocity = new Vector2(Mathf.Cos(angleRad) * _speed * dir_x, Mathf.Sin(angleRad) * _speed) + _inheritedVelocity;
    }

    private void FixedUpdate() {
        // 重力を適用しながら弧を描いて落下
        _velocity.y -= _gravity * Time.fixedDeltaTime;
        transform.position += (Vector3)(_velocity * Time.fixedDeltaTime);

        _CheckOffScreen();
    }

    // 画面外に出たら消える
    private void _CheckOffScreen() {
        var cam = Camera.main;
        if (cam == null) return;

        var viewport_pos = cam.WorldToViewportPoint(transform.position);
        if (viewport_pos.x < 0f || viewport_pos.x > 1f || viewport_pos.y < 0f || viewport_pos.y > 1f) {
            Destroy(gameObject);
        }
    }
}
