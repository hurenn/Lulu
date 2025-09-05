using UnityEngine;

public class HP_Heart : MonoBehaviour {
    /// <summary>
    /// HP表示
    /// </summary>
    [SerializeField] private GameObject _hpFill;

    /// <summary>
    /// ハートを満タンにする
    /// </summary>
    public void SetFill(bool is_fill) {
        _hpFill.SetActive(is_fill);
    }
}
