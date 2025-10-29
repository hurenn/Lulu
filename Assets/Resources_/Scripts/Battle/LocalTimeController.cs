using System.Collections;
using UnityEngine;

/// <summary>
/// ローカル時間制御クラス
/// </summary>
public class LocalTimeController : MonoBehaviour
{
    public float _localTimeScale { get; private set; } = 1.0f;
    public float localDeltaTime => Time.deltaTime * _localTimeScale;

    private Coroutine _timeStopRoutine;

    /// <summary>
    /// ヒットストップ開始
    /// </summary>
    /// <param name="duration">ヒットストップ時間</param>
    /// <param name="resumeSpeed">回復速度</param>
    public void HitStop(float duration, float resumeSpeed = 1.0f)
    {
        if (_timeStopRoutine != null)
        {
            StopCoroutine(_timeStopRoutine);
        }
        _timeStopRoutine = StartCoroutine(_HitStopRoutine(duration, resumeSpeed));
    }
    private IEnumerator _HitStopRoutine(float duration, float resumeSpeed)
    {
        _localTimeScale = 0.0f;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            // ヒットストップ時間を計測（リアルタイムで）
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        while (_localTimeScale < 1.0f)
        {
            // 時間を元に戻す
            _localTimeScale += resumeSpeed * Time.unscaledDeltaTime;
            _localTimeScale = Mathf.Min(_localTimeScale, 1.0f);
            yield return null;
        }
        _localTimeScale = 1.0f;
        _timeStopRoutine = null;
    }
}
