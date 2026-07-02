using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// タイムアタック記録地点
/// </summary>
public class StageObject_TimeAttack : StageObject_Base {
    private readonly float _DEFAULT_BEST_TIME = 30.0f;
    [Serializable]
    private enum TimeAttackType {
        StartPoint, // スタート地点
        GoalPoint,  // 記録地点
        ResetPoint  // リセット地点
    }
    [Serializable]
    private class TimeReward {
        public float time;
        public GameObject reward;
    }

    [SerializeField] private TimeAttackType _isStartPoint = TimeAttackType.StartPoint;
    [SerializeField] private TMP_Text _recordText;
    [SerializeField] private TMP_Text _bestTimeText;
    [SerializeField] private AudioSource _se;
    [SerializeField] private TimeReward[] _timeRewards;

    // インスタンス間で共有するタイマー状態
    private static float _currentTime = 0f;
    private static float _bestTime = float.MaxValue;
    private static bool _isRunning = false;
    private static IEnumerator _coroutine;

    private void Update() {
        if (!_isRunning || _isStartPoint != TimeAttackType.StartPoint) {
            return;
        }
        _currentTime += Time.deltaTime;
        if (_recordText != null) {
            _recordText.text = $"Time: {_FormatTime(_currentTime)}";
        }
    }

    protected override void _HitPlayer(Player_Character player) {
        base._HitPlayer(player);
        switch (_isStartPoint) {
            case TimeAttackType.StartPoint:
                StartTimeAttack();
                break;
            case TimeAttackType.GoalPoint:
                RecordTimeAttack();
                break;
            case TimeAttackType.ResetPoint:
                ResetTimeAttack();
                break;
        }
    }

    /// <summary>
    /// タイムアタックを開始する
    /// </summary>
    private void StartTimeAttack() {
        if (_isRunning) {
            return;
        }
        _currentTime = 0f;
        _isRunning = true;

        // 経過タイム表示・最高記録非表示
        if (_recordText != null) {
            _recordText.gameObject.SetActive(true);
            _recordText.text = _FormatTime(0f);
        }
        _se?.Play();
    }

    /// <summary>
    /// タイムアタックを終了して記録する
    /// </summary>
    private void RecordTimeAttack() {
        if (!_isRunning) {
            return;
        }
        _isRunning = false;

        // ベストタイム更新
        bool isNewRecord = _currentTime < _bestTime;
        if (isNewRecord) {
            _bestTime = _currentTime;
        }

        // 最高記録表示
        if (_bestTimeText != null) {
            _bestTimeText.gameObject.SetActive(true);
            string bestStr = _bestTime < float.MaxValue ? _FormatTime(_bestTime) : "--:--.--";
            _bestTimeText.text = $"BestTime: \n{bestStr}";
        }

        // 対象の報酬を表示
        foreach (var reward in _timeRewards) {
            if (reward != null && _currentTime <= reward.time) {
                if(reward.reward != null) {
                    reward.reward.SetActive(true);
                }
                break; // 最初に条件を満たした報酬だけを表示
            }
        }

        // recordTextを3秒かけて3回点滅
        if (_recordText != null) {
            _recordText.text = $"Time: {_FormatTime(_currentTime)}";
            _coroutine = _BlinkText(_recordText, 3, 3f);
            StartCoroutine(_coroutine);
        }

        _se?.Play();
    }

    private IEnumerator _BlinkText(TMP_Text text, int blinkCount, float totalDuration) {
        float halfInterval = totalDuration / (blinkCount * 2f);
        for (int i = 0; i < blinkCount; i++) {
            text.gameObject.SetActive(false);
            yield return new WaitForSeconds(halfInterval);
            text.gameObject.SetActive(true);
            yield return new WaitForSeconds(halfInterval);
        }
    }

    /// <summary>
    /// タイムアタックを中断してリセットする
    /// </summary>
    private void ResetTimeAttack() {
        _isRunning = false;
        _currentTime = 0f;
        _bestTime = _DEFAULT_BEST_TIME;
        if(_coroutine != null) {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        // 両方非表示
        _recordText?.gameObject.SetActive(false);
        _bestTimeText?.gameObject.SetActive(false);
    }

    private static string _FormatTime(float time) {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int centiseconds = (int)((time % 1f) * 100f);
        return $"{minutes:D2}:{seconds:D2}.{centiseconds:D2}";
    }
}
