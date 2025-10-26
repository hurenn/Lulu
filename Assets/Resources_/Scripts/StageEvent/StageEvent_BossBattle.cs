using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class StageEvent_BossBattle : MonoBehaviour {
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

    private void _Setup() {
        if (_bossEnemy != null) {
            _bossEnemy.OnDied += _OnBossDied;
            _bossEnemy.OnDieEnded += _OnBossDieEnded;
        }
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
