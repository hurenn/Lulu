using UnityEngine;

// 収集アイテム1つ分(青アイス等)。プレイヤーが触れたらCollectionQuestへ伝えて消える
public class CollectibleItem : MonoBehaviour {
    [SerializeField] private CollectionQuest _quest;
    [SerializeField] private AudioClip _seCollect;
    [SerializeField] private float _seCollectVolume = 2f; // クリップ自体の収録音量が小さいため増幅する
    [SerializeField] private GameObject _pickEffect; // 入手時に表示するエフェクト

    private bool _isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (_isCollected || !collision.CompareTag("Player")) return;
        _isCollected = true;

        _quest.OnItemCollected();

        // SetActive(false)で自身のAudioSourceも止まるため、PlayClipAtPointで独立に再生する
        if (_seCollect != null) {
            AudioUtil.PlayClipAtPointUnclamped(_seCollect, transform.position, _seCollectVolume);
        }

        if (_pickEffect != null) {
            Instantiate(_pickEffect, transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }
}
