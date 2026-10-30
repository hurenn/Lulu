using UnityEngine;

/// <summary>
/// キャラクターの子に置く、パーティクルの軌跡(靄など)
/// キャラクターが消える時にDetachAllを呼ぶと、軌跡を切り離して残し、出し終えたパーティクルが消えてから削除する
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleTrail : MonoBehaviour {
    /// <summary>
    /// root以下の軌跡を切り離し、新しく出すのを止める(出し終えたパーティクルが消えたら削除される)
    /// </summary>
    public static void DetachAll(GameObject root) {
        if (root == null) return;
        foreach (var trail in root.GetComponentsInChildren<ParticleTrail>()) {
            trail.Detach();
        }
    }

    public void Detach() {
        var particle = GetComponent<ParticleSystem>();
        var main = particle.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
        transform.SetParent(null, true);
        particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
