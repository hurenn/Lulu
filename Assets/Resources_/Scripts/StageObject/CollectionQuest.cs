using System.Collections;
using TMPro;
using UnityEngine;

// アイテムを指定個数集めたら経験値を付与するギミック管理(青アイス集め等に使用)
public class CollectionQuest : MonoBehaviour {
    [SerializeField] private int _maxCount = 8;
    [SerializeField] private TMP_Text _counterText; // "現在数/最大数"表示

    [SerializeField] private MessageList _messageListScript;
    [SerializeField] private MessageViewer _messageViewer;
    [SerializeField] private MessageData[] _firstItemMessages; // 1個目取得時に表示する場合がある
    [SerializeField] private MessageData[] _lastItemMessages;  // 最後の1個取得時に表示する場合がある

    [SerializeField] private int _rewardExp = 200;
    [SerializeField] private float _hideDelayAfterComplete = 5f; // 全部集めてからUIを隠すまでの時間

    [Header("出現/非表示アニメーション")]
    [SerializeField] private float _hiddenPosX = -200f; // 非表示時に置くPosX(画面外)
    [SerializeField] private float _slideDuration = 0.3f;

    private RectTransform _counterRect;
    private float _restingPosX; // 本来の(表示時の)PosX

    private int _currentCount = 0;
    private bool _isCompleted = false;

    public int CurrentCount => _currentCount;
    public int MaxCount => _maxCount;
    public bool IsCompleted => _isCompleted;

    private void Awake() {
        if (_messageListScript == null) _messageListScript = FindAnyObjectByType<MessageList>();
        if (_messageViewer == null) _messageViewer = FindAnyObjectByType<MessageViewer>();

        if (_counterText != null) {
            _counterRect = _counterText.rectTransform;
            _restingPosX = _counterRect.anchoredPosition.x;

            // 初期状態は非表示。画面外(_hiddenPosX)に置いてから非アクティブにする
            _SetPosX(_hiddenPosX);
            _counterText.gameObject.SetActive(false);
        }
    }

    // 各アイテム(CollectibleItem)から呼ばれる
    public void OnItemCollected() {
        if (_isCompleted || _currentCount >= _maxCount) return;

        _currentCount++;

        if (_counterText != null) {
            if (_currentCount == 1) {
                // 1個目取得時のみ、出現アニメを再生する
                _counterText.gameObject.SetActive(true);
                StartCoroutine(_SlideCo(_restingPosX));
            }
            _counterText.text = _currentCount + "/" + _maxCount;
        }

        if (_currentCount == 1 && _firstItemMessages != null) {
            foreach (var message in _firstItemMessages) {
                _messageListScript.Enqueue(message);
            }
        }

        if (_currentCount >= _maxCount) {
            _isCompleted = true;

            if (_lastItemMessages != null) {
                foreach (var message in _lastItemMessages) {
                    _messageListScript.Enqueue(message);
                }
            }

            var player = PlayerCharacterManager.Current as Player_Character;
            player?.AddExp(_rewardExp);

            if (_counterText != null) {
                StartCoroutine(_HideAfterDelayCo());
            }
        }
    }

    private IEnumerator _HideAfterDelayCo() {
        yield return new WaitForSeconds(_hideDelayAfterComplete);
        if (_counterText != null) {
            yield return _SlideCo(_hiddenPosX); // 非表示アニメの完了を待ってから非アクティブにする
            _counterText.gameObject.SetActive(false);
        }
    }

    // PosXを目的の値まで滑らせる(出現/非表示アニメ共用)
    private IEnumerator _SlideCo(float targetX) {
        float startX = _counterRect.anchoredPosition.x;
        float elapsed = 0f;
        while (elapsed < _slideDuration) {
            elapsed += Time.deltaTime;
            float x = Mathf.Lerp(startX, targetX, Mathf.Clamp01(elapsed / _slideDuration));
            _SetPosX(x);
            yield return null;
        }
        _SetPosX(targetX);
    }

    private void _SetPosX(float x) {
        var pos = _counterRect.anchoredPosition;
        pos.x = x;
        _counterRect.anchoredPosition = pos;
    }
}
