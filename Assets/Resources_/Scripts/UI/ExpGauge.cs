using UnityEngine;
using UnityEngine.UI;

public class ExpGauge : MonoBehaviour {
    /// <summary>
    /// 対象キャラクターのパラメーター
    /// </summary>
    private PlayerParameter _playerParameter;
    /// <summary>
    /// 経験値ゲージ表示
    /// </summary>
    [SerializeField] private Image _expFillImage;
    [SerializeField] private Image _expWhiteImage;
    /// <summary>
    /// 経験値追加アニメーション
    /// </summary>
    [SerializeField] private Animator _expAddAnim;

    public void Start() {
        _playerParameter = PlayerParameter.Instance;

        if (_playerParameter != null) {
            // 経験値更新イベントに登録
            _playerParameter.OnExpChanged += (exp) => UpdateExpGauge(exp);
            // 初期表示
            UpdateExpGauge();
        }
    }

    /// <summary>
    /// 経験値ゲージの更新
    /// </summary>
    public void UpdateExpGauge(int exp = 0) {
        if(_expFillImage == null || _playerParameter == null) {
            return;
        }
        var currentExp = _playerParameter.currentExp;
        var nextLevelExp = _playerParameter.nextExp;
        var fillAmount = (float)currentExp / nextLevelExp;
        _expFillImage.fillAmount = fillAmount;
        //_expWhiteImage.fillAmount = fillAmount;

        // 経験値追加アニメーション再生
        if (exp > 0 && _expAddAnim != null) {
            _expAddAnim.Play("AddExp", 0, 0f);
        }
    }
}
