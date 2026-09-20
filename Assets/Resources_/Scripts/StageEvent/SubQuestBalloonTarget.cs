using UnityEngine;

// 目標の風船に触れたことをSubQuest_Balloonへ伝える。クエスト進行中は自身を上昇させる。
public class SubQuestBalloonTarget : MonoBehaviour {
    [SerializeField] private SubQuest_Balloon _quest;
    [SerializeField] private float _riseSpeed = 1f; // 上昇速度(m/s、風船側で設定可能)
    [SerializeField] private AudioClip _seCatch;     // 取得時の効果音

    private void Update() {
        if (Pause_UI.IsOpen) return;
        if (_quest == null || !_quest.IsRunning) return;

        transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.CompareTag("Player")) return;
        _quest.OnBalloonReached();

        // SetActive(false)すると自身のAudioSourceも止まってしまうため、PlayClipAtPointで独立に再生する
        if (_seCatch != null) {
            AudioSource.PlayClipAtPoint(_seCatch, transform.position);
        }
        gameObject.SetActive(false); // 取った風船は非表示にする
    }
}
