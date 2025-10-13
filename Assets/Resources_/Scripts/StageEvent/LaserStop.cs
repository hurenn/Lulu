using System.Collections;
using UnityEngine;

/// <summary>
/// 触れてしばらくするとレーザーを止めるイベント用オブジェクト
/// </summary>
public class LaserStop : StageObject_Base {
    [Header("レーザーを止めるまでの時間")] [SerializeField] 
    private float _stopTime = 3.0f;
    [SerializeField]
    private Animator[] _laserAnimators;

    [SerializeField]
    private GameObject _afeterMessageTrigger;

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        StartCoroutine(_StopperWait());
    }

    private IEnumerator _StopperWait() {
        yield return new WaitForSeconds(_stopTime);
        foreach (var animator in _laserAnimators) {
            animator.SetBool("isStopped", true);
        }

        yield return new WaitForSeconds(_stopTime);
        if (_afeterMessageTrigger != null) {
            _afeterMessageTrigger.SetActive(true);
        }
    }
}
