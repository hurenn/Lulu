using System.Collections;
using UnityEngine;

// 目標の風船に触れたことをSubQuest_Balloonへ伝える。クエスト進行中は自身を上昇させる。
public class SubQuestBalloonTarget : MonoBehaviour {
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SubQuest_Balloon _quest;
    [SerializeField] private float _riseSpeed = 1f; // 上昇速度(m/s、風船側で設定可能)
    [SerializeField] private AudioClip _seCatch;     // 取得時の効果音
    [SerializeField] private float _disappearDuration = 0.3f; // 取得時に収縮して消えるまでの時間

    private bool _isCaught = false;

    private void Update() {
        if (Pause_UI.IsOpen) return;
        if (_quest == null || !_quest.IsRunning) return;

        transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (_isCaught || !collision.CompareTag("Player")) return;
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
}
