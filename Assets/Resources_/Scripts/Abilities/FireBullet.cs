using UnityEngine;

public class FireBullet : AutoDestroy
{
    // 直進速度
    [SerializeField]
    private float _speed = 10.0f;

    private void FixedUpdate() {
        // 右に直進
        transform.position += transform.right * _speed;
    }
}
