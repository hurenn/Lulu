using UnityEngine;

// Zelxが生成するミスト。プレイヤーに向かって弧を描きながら徐々に近づく
public class SylxMist : Enemy_Base {
    [Header("追尾AIパラメータ")]
    [SerializeField] private float _approachSpeed = 2.0f;  // プレイヤーに近づく速さ
    [SerializeField] private float _arcAngle = 45f;         // 初動でつける弧の角度(度)
    [SerializeField] private float _arcSettleTime = 2.0f;   // 弧が真っ直ぐになるまでの時間

    private Transform _playerTransform;
    private float _elapsed = 0f;
    private float _arcSign = 1f;

    protected override void _Setup() {
        base._Setup();
        var controller = PlayerCharacterManager.Controller;
        _playerTransform = controller != null ? controller.Character.transform : null;
        _arcSign = Random.value < 0.5f ? -1f : 1f;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();

        if (_isDead || _damageReactionTimer > 0 || _playerTransform == null) {
            return;
        }

        _Move();
    }

    // プレイヤー方向へ、時間経過で収束する弧を描きながら近づく
    private void _Move() {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _arcSettleTime);

        Vector2 toTarget = _playerTransform.position - transform.position;
        Vector2 direction = toTarget.normalized;

        // 弧の角度を時間経過で徐々に0へ収束させ、次第に真っ直ぐ近づくようにする
        float currentAngle = Mathf.Lerp(_arcAngle, 0f, t) * _arcSign;
        direction = Quaternion.Euler(0f, 0f, currentAngle) * direction;

        _rb.MovePosition(transform.position + (Vector3)(direction * _approachSpeed * Time.deltaTime));
    }
}
