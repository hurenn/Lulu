using UnityEngine;

public class HP_Heart : MonoBehaviour {
    /// <summary>
    /// HP表示
    /// </summary>
    [SerializeField] private GameObject _hpFill;
    [SerializeField] private Animator _hpAnim;

    // 出現エフェクト
    [SerializeField] private GameObject _effectPrefab;

    /// <summary>
    /// ハートを満タンにする
    /// </summary>
    public void SetFill(bool is_fill) {
        _hpFill.SetActive(is_fill);
    }

    /// <summary>
    /// HP獲得アニメーション再生
    /// </summary>
    public void OnPlaySpawnAnim() {
        if(_effectPrefab != null) {
            Instantiate(_effectPrefab, transform);
        }
        _hpAnim.Play("SpawnHeart", 0, 0f);
    }
}
