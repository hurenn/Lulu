using TMPro;
using UnityEngine;
using UnityEngine.Playables;

// 救出ミッション管理。ステージ中の救出対象(Survivor)を全員救出したらステージクリアとしてTimelineを流す
public class RescueMission : MonoBehaviour {
    [SerializeField] private int _maxCount = 5;
    [SerializeField] private TMP_Text _counterText; // "救出済み人数/最大人数"表示

    // 救出するたびに先頭から順にアクティブにする(救出した相手に関わらず順番は固定)
    [SerializeField] private GameObject[] _rescueMessageObjects;

    [SerializeField] private PlayableDirector _clearTimeline; // 全員救出時に流すTimeline

    [SerializeField] private GoalMarker _goalMarker; // 残り1人になったら、その救出対象の方向を示す矢印

    private int _currentCount = 0;
    private int _messageCount = 0; // 救出メッセージを出した数（アオイ等のイベントキャラは数えない）
    private bool _isCompleted = false;

    public int CurrentCount => _currentCount;
    public int MaxCount => _maxCount;
    public bool IsCompleted => _isCompleted;

    private void Start() {
        // リトライ時：救出済みの人数を復元する（その人数までのメッセージは表示をスキップする）
        var checkpoint_manager = CheckpointManager.Instance;
        _currentCount = Mathf.Min(checkpoint_manager.RescuedSurvivorCount, _maxCount);
        _messageCount = checkpoint_manager.RescuedMessageSurvivorCount;
        _isCompleted = _currentCount >= _maxCount;
        _UpdateCounterText();
        _UpdateGoalMarker();

        // クリアTimeline中にリトライした場合は、再度クリアTimelineを流す
        if (_isCompleted) {
            _PlayClearTimeline();
        }
    }

    // 各救出対象(Survivor)から呼ばれる
    /// <param name="shows_message">救出メッセージを出すか（アオイ等のイベントキャラは出さず、順番にも数えない）</param>
    public void OnSurvivorRescued(bool shows_message = true) {
        if (_isCompleted || _currentCount >= _maxCount) return;

        _currentCount++;
        _UpdateCounterText();

        if (shows_message) {
            int message_index = _messageCount;
            _messageCount++;
            if (_rescueMessageObjects != null && message_index < _rescueMessageObjects.Length && _rescueMessageObjects[message_index] != null) {
                _rescueMessageObjects[message_index].SetActive(true);
            }
        }

        if (_currentCount >= _maxCount) {
            _isCompleted = true;
            _PlayClearTimeline();
        }
        _UpdateGoalMarker();
    }

    /// <summary>
    /// 残り1人の間だけ、その救出対象の方向にゴールマーカーを表示する
    /// </summary>
    private void _UpdateGoalMarker() {
        if (_goalMarker == null) return;

        Survivor last_survivor = null;
        if (!_isCompleted && _maxCount - _currentCount == 1) {
            foreach (var survivor in FindObjectsByType<Survivor>(FindObjectsSortMode.None)) {
                if (!survivor.IsRescued) {
                    last_survivor = survivor;
                    break;
                }
            }
        }

        if (last_survivor != null) {
            _goalMarker.SetGoal(last_survivor.transform);
        }
        _goalMarker.SetMarkerActive(last_survivor != null);
    }

    private void _UpdateCounterText() {
        if (_counterText != null) {
            _counterText.text = _currentCount + "/" + _maxCount;
        }
    }

    private void _PlayClearTimeline() {
        if (_clearTimeline == null) {
            Debug.LogError("RescueMission: クリア時のTimelineが設定されていません");
            return;
        }
        _clearTimeline.time = 0;
        _clearTimeline.Evaluate();
        _clearTimeline.Play();
    }
}
