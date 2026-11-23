using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// イベント(Timeline)の早送り。Timeline実行中にポーズボタンを押すと、ポーズ画面の代わりに早送りを切り替える。
/// 早送り中は時間の流れを速め、メッセージは入力を待たずに次へ送る。イベントが終わったら元の速さに戻す
/// </summary>
public class EventFastForward : MonoBehaviour {
    private const float SPEED = 6f;              // 早送りの速さ(時間の流れの倍率)
    private const float MAX_TIME_SCALE = 100f;   // Time.timeScaleに設定できる上限
    private const float END_GRACE_TIME = 0.3f;   // Timelineの切り替わりなどで一瞬途切れても早送りを続ける時間(実時間)

    private static EventFastForward s_instance;

    /// <summary>早送り中か</summary>
    public static bool IsActive => s_instance != null && s_instance._isActive;

    private bool _isActive;
    private float _baseTimeScale = 1f;     // 早送りしていなければ使われている時間の流れ
    private float _appliedTimeScale = -1f; // 早送りで設定した時間の流れ(他の処理が書き換えたかの判定に使う)
    private float _noEventTime;            // イベントが途切れてからの時間

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void _Create() {
        if (s_instance != null) return;
        var obj = new GameObject("EventFastForward");
        DontDestroyOnLoad(obj);
        s_instance = obj.AddComponent<EventFastForward>();
    }

    /// <summary>
    /// イベント(Timeline)の実行中か。メッセージや選択肢の入力待ちでTimelineが止まっている間も含む。
    /// 時間の流れの影響を受けないTimeline(必殺技のカットインなど)はイベントとして扱わない
    /// </summary>
    public static bool IsEventRunning() {
        foreach (var director in FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None)) {
            if (director.state != PlayState.Playing) continue;
            if (director.timeUpdateMode == DirectorUpdateMode.UnscaledGameTime) continue;
            return true;
        }
        var viewer = FindAnyObjectByType<MessageViewer>();
        return viewer != null && viewer.IsWaitingTimeline;
    }

    /// <summary>
    /// 早送りの開始・解除を切り替える
    /// </summary>
    public static void Toggle() {
        if (s_instance == null) return;
        if (s_instance._isActive) {
            s_instance._Stop();
        } else {
            s_instance._isActive = true;
            s_instance._noEventTime = 0f;
            s_instance._ApplyTimeScale();
        }
    }

    /// <summary>
    /// 早送りを解除する(シーンの開始時に呼ぶ。早送りはステージを跨がない。時間の流れは呼び出し側で戻す)
    /// </summary>
    public static void ResetForNewScene() {
        if (s_instance == null) return;
        s_instance._isActive = false;
        s_instance._noEventTime = 0f;
        s_instance._appliedTimeScale = -1f;
    }

    /// <summary>
    /// 早送りしていなければ使われている時間の流れ(早送り中に時間の流れを保存して後で戻す処理向け)
    /// </summary>
    public static float GetBaseTimeScale() {
        if (IsActive && Mathf.Approximately(Time.timeScale, s_instance._appliedTimeScale)) {
            return s_instance._baseTimeScale;
        }
        return Time.timeScale;
    }

    private void Update() {
        if (!_isActive || Pause_UI.IsOpen) return;

        if (IsEventRunning()) {
            _noEventTime = 0f;
        } else {
            _noEventTime += Time.unscaledDeltaTime;
            if (_noEventTime >= END_GRACE_TIME) {
                _Stop();
                return;
            }
            // イベントが途切れている間は元の速さに戻しておく(イベントが終わって操作が戻った直後に速く動かないように)
            _RestoreBaseTimeScale();
            return;
        }
        _ApplyTimeScale();
    }

    private void _ApplyTimeScale() {
        // 他の処理(Timelineのスロー演出など)が時間の流れを変えていたら、それを元の速さとして扱う
        if (!Mathf.Approximately(Time.timeScale, _appliedTimeScale)) {
            _baseTimeScale = Time.timeScale;
        }
        _appliedTimeScale = Mathf.Min(_baseTimeScale * SPEED, MAX_TIME_SCALE);
        Time.timeScale = _appliedTimeScale;
    }

    private void _Stop() {
        _isActive = false;
        _RestoreBaseTimeScale();
    }

    // 早送りで設定した時間の流れを元に戻す(他の処理が書き換えていたら、そちらを優先して触らない)
    private void _RestoreBaseTimeScale() {
        if (Mathf.Approximately(Time.timeScale, _appliedTimeScale)) {
            Time.timeScale = _baseTimeScale;
        }
        _appliedTimeScale = -1f;
    }
}
