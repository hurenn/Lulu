using System.Collections;
using UnityEngine;

/// <summary>
/// WaitForSecondsの1フレーム分のオーバーシュートを次回の待機に繰り越し、
/// フレームレートに依存する合計待機時間のズレ(エディタ/ビルド間の差など)を抑える
/// </summary>
public class PreciseWaitTimer {
    private float _debt = 0f;

    public IEnumerator Wait(float seconds) {
        float wait = Mathf.Max(0f, seconds - _debt);
        float start = Time.time;
        yield return new WaitForSeconds(wait);
        _debt = (Time.time - start) - wait;
    }
}
