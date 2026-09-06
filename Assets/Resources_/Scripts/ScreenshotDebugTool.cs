using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 高画質スクリーンショット撮影用のデバッグキー。
/// 0:スクリーンショットを2倍解像度で撮影
/// 1:TimeScaleを半分にする 2:TimeScaleを1に戻す 3:一時停止と再開を切り替え
/// TimeScaleRequestManagerのスタックは介さず、Time.timeScaleを直接操作する
/// （撮影用に任意のタイミングで自由に止め・戻ししたいため）。
/// どのStageシーンを最初に再生しても使えるよう、シーンに手動配置せず起動時に自動生成する。
/// TrailRendererはTime.timeScaleの影響を受けず実時間で減衰するため、
/// 撮影用に時間を遅く/停止している間は消えた分だけtimeを補正して見た目の長さを維持する。
/// </summary>
public class ScreenshotDebugTool : PersistentSingleton<ScreenshotDebugTool> {
    private const int SCREENSHOT_SUPER_SIZE = 2;
    private const string SCREENSHOT_FOLDER = "Screenshots";
    private const float MIN_TIME_SCALE_FOR_TRAIL = 0.001f; // 0除算防止（timeScale=0の時は疑似的に極小値として扱う）

    private bool _isPaused = false;
    private float _timeScaleBeforePause = 1f;
    private readonly Dictionary<TrailRenderer, float> _baseTrailTimes = new Dictionary<TrailRenderer, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        _ = Instance;
    }

    private void Update() {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit0Key.wasPressedThisFrame) {
            CaptureScreenshot();
        }

        if (keyboard.digit1Key.wasPressedThisFrame) {
            Time.timeScale *= 0.5f;
            UpdateTrailTimeCompensation();
        }

        if (keyboard.digit2Key.wasPressedThisFrame) {
            Time.timeScale = 1f;
            _isPaused = false;
            UpdateTrailTimeCompensation();
        }

        if (keyboard.digit3Key.wasPressedThisFrame) {
            TogglePause();
            UpdateTrailTimeCompensation();
        }
    }

    /// <summary>
    /// TrailRendererのtimeは実時間基準で減衰するため、Time.timeScaleに反比例させて
    /// 見た目の尾の長さ（ワールド距離）を通常時と揃える。timeScaleが1に戻ったら元の値へ復元する。
    /// </summary>
    private void UpdateTrailTimeCompensation() {
        if (Mathf.Approximately(Time.timeScale, 1f)) {
            foreach (var pair in _baseTrailTimes) {
                if (pair.Key != null) {
                    pair.Key.time = pair.Value;
                }
            }
            _baseTrailTimes.Clear();
            return;
        }

        float scale = Mathf.Max(Time.timeScale, MIN_TIME_SCALE_FOR_TRAIL);
        var trails = FindObjectsByType<TrailRenderer>(FindObjectsSortMode.None);
        foreach (var trail in trails) {
            if (!_baseTrailTimes.TryGetValue(trail, out float baseTime)) {
                baseTime = trail.time;
                _baseTrailTimes[trail] = baseTime;
            }
            trail.time = baseTime / scale;
        }
    }

    private void CaptureScreenshot() {
        string folder = Path.Combine(Application.dataPath, SCREENSHOT_FOLDER);
        Directory.CreateDirectory(folder);
        string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName), SCREENSHOT_SUPER_SIZE);
        Debug.Log($"[ScreenshotDebugTool] Captured: {fileName}");
    }

    private void TogglePause() {
        if (_isPaused) {
            Time.timeScale = _timeScaleBeforePause;
        } else {
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }
        _isPaused = !_isPaused;
    }
}
