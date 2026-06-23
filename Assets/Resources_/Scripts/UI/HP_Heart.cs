using UnityEngine;

public class HP_Heart : MonoBehaviour {
    /// <summary>
    /// HP表示
    /// </summary>
    [SerializeField] private GameObject _hpFill;
    [SerializeField] private GameObject _hpBreakHeart;
    [SerializeField] private Animator _hpAnim;

    // 出現エフェクト
    [SerializeField] private GameObject _effectPrefab;

    /// <summary>
    /// ハートを満タンにする
    /// </summary>
    public void SetFill(bool is_fill) {
        if (_hpFill != null) {
            _hpFill.SetActive(is_fill);
            // HP回復アニメーション再生
            if (_hpBreakHeart != null && is_fill && _hpBreakHeart.activeSelf) {
                _PlayHealHeart();
            }
        }
        if (_hpBreakHeart != null) { _hpBreakHeart.SetActive(!is_fill); }
    }

    /// <summary>
    /// HP回復アニメーション再生
    /// </summary>
    private void _PlayHealHeart() {
        Instantiate(_effectPrefab, transform);
        _hpAnim.Play("RecoverHeart", 0, 0f);
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
