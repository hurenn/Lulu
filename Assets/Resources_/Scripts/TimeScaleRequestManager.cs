using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数の演出（チュートリアル・Timeline等）が同時にTime.timeScaleを操作しても
/// 正しく元の値へ復元できるよう、要求をスタックで一元管理する。
/// 各要求元は必ずRequest/Releaseを対で呼び出すこと。
/// </summary>
public static class TimeScaleRequestManager {
    private static readonly List<float> _requestStack = new List<float>();
    // 誰も要求していない時の本来の値（最初の要求時にのみ更新する）
    private static float _baselineScale = 1f;

    /// <summary>
    /// 時間の流れを指定の速度に変更する
    /// </summary>
    public static void Request(float scale) {
        if (_requestStack.Count == 0) {
            // 誰も要求していない状態で保存するため、これが本来の値になる
            _baselineScale = Time.timeScale;
        }
        _requestStack.Add(scale);
        Time.timeScale = scale;
    }

    /// <summary>
    /// 直前のRequestを解除する。他に要求元が残っていればその値に、
    /// 誰も残っていなければ本来の値に復元する
    /// </summary>
    public static void Release() {
        if (_requestStack.Count == 0) {
            return;
        }
        _requestStack.RemoveAt(_requestStack.Count - 1);
        Time.timeScale = _requestStack.Count > 0
            ? _requestStack[_requestStack.Count - 1]
            : _baselineScale;
    }
}
