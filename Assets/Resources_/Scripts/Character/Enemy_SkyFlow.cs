using UnityEngine;

// 開始位置を中心とした一定範囲内を、サイン波で途切れず漂い続ける飛行敵
public class Enemy_SkyFlow : Enemy_Base {
    [Header("漂うAIパラメータ")]
    [SerializeField] private float _driftSpeed = 1.0f;       // 漂う速さ
    [SerializeField] private float _rangeX = 2.0f;            // 開始位置からの左右範囲
    [SerializeField] private float _rangeY = 1.0f;            // 開始位置からの上下範囲

    private Vector3 _startPosition;
    private float _driftTimeOffset; // 複数の敵が同じ動きに同期しないようにする位相ずらし

    protected override void _Setup() {
        base._Setup();
        _startPosition = transform.position;
        _driftTimeOffset = Random.Range(0f, 100f);
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();

        if (_isDead || _damageReactionTimer > 0) {
            return; // ノックバック等の物理挙動を邪魔しない
        }

        // サイン波で計算するため、目標地点への到達判定なしに永久にループし続ける
        float t = Time.time * _driftSpeed + _driftTimeOffset;
        Vector3 offset = new Vector3(Mathf.Sin(t) * _rangeX, Mathf.Sin(t * 1.3f + 1.5f) * _rangeY, 0f);
        _rb.MovePosition(_startPosition + offset);
    }

    private void OnDrawGizmosSelected() {
        // 漂う範囲を可視化
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? _startPosition : transform.position;
        Gizmos.DrawWireCube(center, new Vector3(_rangeX * 2f, _rangeY * 2f, 0f));
    }
}
