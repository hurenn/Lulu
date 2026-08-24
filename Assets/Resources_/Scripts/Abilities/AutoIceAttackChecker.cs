using UnityEngine;

public class AutoIceAttackChecker : MonoBehaviour {
    private Enemy_Base _targetEnemy;

    /// <summary>
    /// 攻撃対象の敵を取得してクリア
    /// </summary>
    public Enemy_Base PopTargetEnemy() {
        if (_targetEnemy != null && _targetEnemy.isDead) {
            _targetEnemy = null;
        }
        var target = _targetEnemy;
        _targetEnemy = null;

        return target;
    }
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy")) {
            _targetEnemy = collision.GetComponent<Enemy_Base>();
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy")) {
            _targetEnemy = null;
        }
    }
}
