using System.Collections.Generic;
using UnityEngine;

public class HitStopManager : SceneSingleton<HitStopManager> {
    private List<LocalTimeController> _targets = new List<LocalTimeController>();

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
