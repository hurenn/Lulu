using UnityEngine;

// 敵が発射する弾。指定方向へ直進し、寿命が尽きるかヒットすると消える
public class EnemyBullet : MonoBehaviour {
    [SerializeField] private float _speed = 8.0f;
    [SerializeField] private float _lifetime = 5.0f;

    private Vector2 _direction = Vector2.right;

    public void SetDirection(Vector2 direction) {
        _direction = direction.normalized;

        // 進行方向に応じて見た目を反転させる
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (_direction.x < 0 ? -1f : 1f);
        transform.localScale = scale;
    }

    private void Start() {
        Destroy(gameObject, _lifetime);
    }

    private void Update() {
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
    }
}
