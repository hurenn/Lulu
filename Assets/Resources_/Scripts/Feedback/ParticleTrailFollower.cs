using UnityEngine;

/// <summary>
/// 弾などが通った跡にパーティクルを残す。
/// パーティクルは自身の子にせず別のオブジェクトとして出し、毎フレーム位置だけを追いかけさせる
/// (自身がどの経路で消えても(命中・寿命・防御など)、出し終えた粒はその場に残って自然に消えるように)。
/// パーティクルは移動距離に応じて出し(Rate over Distance)、ワールド座標に残す設定にしておくこと
/// </summary>
public class ParticleTrailFollower : MonoBehaviour {
    [SerializeField] private ParticleSystem _trailPrefab;
    [SerializeField] private Vector3 _offset = Vector3.zero; // 追いかける位置のずれ(自身の向き・反転に合わせる)

    private ParticleSystem _trail;

    private void Start() {
        if (_trailPrefab == null) return;
        _trail = Instantiate(_trailPrefab, _GetPosition(), Quaternion.identity);
    }

    private void LateUpdate() {
        if (_trail != null) _trail.transform.position = _GetPosition();
    }

    private Vector3 _GetPosition() {
        return transform.TransformPoint(_offset);
    }

    private void OnDestroy() {
        if (_trail == null) return;
        // 新しく出すのをやめ、出し終えた粒が消えたら削除する
        var main = _trail.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
        _trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _trail = null;
    }
}
