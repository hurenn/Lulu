using System.Collections;
using UnityEngine;

// 目標の風船に触れたことをSubQuest_Balloonへ伝える。
// クエスト進行中はその場で漂い続け、時間切れと同時に空高く飛んでいく。
public class SubQuestBalloonTarget : MonoBehaviour {
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SubQuest_Balloon _quest;
    [SerializeField] private AudioClip _seCatch;     // 取得時の効果音
    [SerializeField] private float _disappearDuration = 0.3f; // 取得時に収縮して消えるまでの時間

    [Header("待機中の漂い")]
    [SerializeField] private float _hoverAmplitude = 0.2f; // 上下に揺れる振れ幅
    [SerializeField] private float _hoverSpeed = 1f;       // 揺れる速さ

    [Header("時間切れ時の逃走")]
    [SerializeField] private float _escapeSpeed = 5f;      // 空へ飛んでいく速度(m/s)
    [SerializeField] private float _escapeDuration = 3f;   // 飛んでいき続ける時間

    private bool _isCaught = false;
    private bool _wasRunning = false;
    private Vector3 _basePosition;

    private void Awake() {
        _basePosition = transform.position;
    }

    private void Update() {
        if (Pause_UI.IsOpen || _quest == null || _isCaught) return;

        if (_quest.IsRunning) {
            _wasRunning = true;
            // その場で上下にゆっくり漂う
            float offsetY = Mathf.Sin(Time.time * _hoverSpeed) * _hoverAmplitude;
            transform.position = new Vector3(_basePosition.x, _basePosition.y + offsetY, _basePosition.z);
        } else if (_wasRunning && !_quest.IsCleared) {
            // 実行中から非実行に変わり、かつクリアもしていない = 時間切れ
            _wasRunning = false;
            StartCoroutine(_FlyAwayCo());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (_isCaught || _quest == null || !_quest.IsRunning || !collision.CompareTag("Player")) return;
        _isCaught = true;

        _quest.OnBalloonReached();

        // SetActive(false)すると自身のAudioSourceも止まってしまうため、PlayClipAtPointで独立に再生する
        if (_seCatch != null) {
            AudioSource.PlayClipAtPoint(_seCatch, transform.position);
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // 収縮演出中の多重ヒットを防ぐ

        StartCoroutine(_ShrinkAndDisappearCo());
    }

    // 下端を基準に収縮しながら消える演出
    private IEnumerator _ShrinkAndDisappearCo() {
        var originalScale = transform.localScale;
        var originalPosition = transform.position;
        float halfHeight = _spriteRenderer != null ? _spriteRenderer.bounds.extents.y : 0f;

        float elapsed = 0f;
        while (elapsed < _disappearDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _disappearDuration);
            float scale = Mathf.Lerp(1f, 0f, t);
            transform.localScale = originalScale * scale;
            // 下端の位置を固定したまま上端側だけ縮んでいくように座標を補正する
            transform.position = new Vector3(originalPosition.x, originalPosition.y - halfHeight * (1f - scale), originalPosition.z);
            yield return null;
        }

        if (_spriteRenderer != null) {
            _spriteRenderer.enabled = false; // 取った風船は非表示にする
        }
    }

    // 時間切れ時、空高く飛んでいく演出
    private IEnumerator _FlyAwayCo() {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // 逃げていく間はもう捕まえられない

        float elapsed = 0f;
        while (elapsed < _escapeDuration) {
            elapsed += Time.deltaTime;
            transform.position += Vector3.up * (_escapeSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
