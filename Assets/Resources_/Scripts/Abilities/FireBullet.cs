using UnityEngine;

public class FireBullet : AutoDestroy
{
    // 直進速度
    [SerializeField]
    private float _speed = 10.0f;

    // 進行方向
    private bool _isRight = true;
    public bool IsRight { set { _isRight = value; } }

    private void FixedUpdate() {
        // 右に直進
        transform.position += transform.right * _speed * (_isRight ? 1 : -1);
    }
}
