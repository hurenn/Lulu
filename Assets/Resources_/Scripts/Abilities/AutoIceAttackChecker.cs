using UnityEngine;

public class AutoIceAttackChecker : MonoBehaviour {
    private Enemy_Base _targetEnemy;

    /// <summary>
    /// UŒ‚‘ÎÛ‚Ì“G‚ğæ“¾‚µ‚ÄƒNƒŠƒA
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
