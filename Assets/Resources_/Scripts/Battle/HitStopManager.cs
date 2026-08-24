using System.Collections.Generic;
using UnityEngine;

public class HitStopManager : MonoBehaviour {
    public static HitStopManager Instance { get; private set; }
    private List<LocalTimeController> _targets = new List<LocalTimeController>();

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        Instance = this;
    }

    /// <summary>
    /// ターゲット登録
    /// </summary>
    public void RegisterTarget(LocalTimeController target) {
        if (!_targets.Contains(target)) {
            _targets.Add(target);
        }
    }

    /// <summary>
    /// ターゲット登録解除
    /// </summary>
    public void Unregister(LocalTimeController target) {
        if (_targets.Contains(target)) {
            _targets.Remove(target);
        }
    }

    public void TriggerHitStop(float duration, float resumeSpeed = 1.0f) {
        foreach (var target in _targets) {
            target.HitStop(duration, resumeSpeed);
        }
    }
}
