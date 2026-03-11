using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class StageEvent_BossBattle : MonoBehaviour {
    [Serializable]
    private class MessageTriggerActivator {
        public MessageTrigger messageTrigger;
        public float delay = 0f;
        public bool isHalfHPTrigger = false;
        public bool isSeriouslyTrigger = false;
    }
    [SerializeField] private MessageTriggerActivator[] _messageTriggerActivators;
    private float _currentMessageTriggerDelay = 0f;
    private bool _isHalfHp = false;
    private bool _isSeriously = false;

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Enemy_Base _bossEnemy;
    [SerializeField] private GameObject _stageClearTimeline;
    [SerializeField] private MessageViewer _messageViewer;
    [SerializeField] private MessageTrigger _stageClearMessageTrigger;
    [SerializeField] private GameObject _sparkEffect;


    private void Reset() {
        _playerController = FindAnyObjectByType<PlayerController>();
        _bossEnemy = FindAnyObjectByType<Enemy_Base>();
        _messageViewer = FindAnyObjectByType<MessageViewer>();
        _stageClearTimeline = FindAnyObjectByType<PlayableDirector>().gameObject;
        _sparkEffect = transform.Find("SparkEffect")?.gameObject;
    }

    private void Start() {
        _Setup();
    }

    private void Update() {
        _currentMessageTriggerDelay += Time.deltaTime;
        foreach (var activator in _messageTriggerActivators) {
            // HP条件が合わない場合はスキップ
            if (activator.isHalfHPTrigger != _isHalfHp) {
                continue;
            }
            // 本気条件が合わない場合はスキップ
            if (activator.isSeriouslyTrigger != _isSeriously) {
                continue;
            }
            if (activator.messageTrigger == null) {
                continue;
            }

            if (_currentMessageTriggerDelay >= activator.delay) {
                // 追加メッセージを有効化
                activator.messageTrigger.gameObject.SetActive(true);
                activator.messageTrigger = null; // 一度だけ有効化するためにnullに設定
            }
        }
    }

    private void _Setup() {
        if (_bossEnemy != null) {
            _bossEnemy.OnDied += _OnBossDied;
            _bossEnemy.OnDieEnded += _OnBossDieEnded;
            _bossEnemy.OnDowned += _OnBossDowned;
            _bossEnemy.OnSeriously += _OnBossSeriously;
        }
    }

    private void _OnBossDowned() {
        _currentMessageTriggerDelay = 0f;
        _messageViewer?.ForceReset();
        _isHalfHp = true;
        StartCoroutine(_BossDownedRoutine());
    }
    private void _OnBossSeriously() {
        _messageViewer?.ForceReset();
        //_currentMessageTriggerDelay = 0f;
        _isSeriously = true;
    }
    private IEnumerator _BossDownedRoutine() {
        // プレイヤーの操作を停止
        if (_playerController != null) {
            _playerController.isEnabledCharacterInput = false;
        }
        if (_sparkEffect != null) _sparkEffect.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        // プレイヤーの操作を再開
        if (_playerController != null) {
            _playerController.isEnabledCharacterInput = true;
        }
        if(_sparkEffect != null) _sparkEffect.SetActive(false);

        yield break;
    }

    private void _OnBossDied() {
        // プレイヤーの操作を停止
        if (_playerController != null) {
            _playerController.isEnabledCharacterInput = false;
        }
        _messageViewer?.ForceReset();

        if( _stageClearMessageTrigger != null ) {
            StartCoroutine(_ViewClearMessage());
        }
    }

    private IEnumerator _ViewClearMessage() {
        if(_sparkEffect != null) _sparkEffect.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);

        // ステージクリアメッセージ表示
        _stageClearMessageTrigger.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(1f);
        if (_sparkEffect != null) _sparkEffect.SetActive(false);
    }

    private void _OnBossDieEnded() {
        // ステージクリア演出
        _stageClearTimeline.SetActive(true);
    }
}
