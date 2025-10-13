using UnityEngine;
using UnityEngine.UI;

public class ExpGauge : MonoBehaviour {
    /// <summary>
    /// 対象キャラクターのパラメーター
    /// </summary>
    [SerializeField] private CharacterParameter _characterParameter;
    /// <summary>
    /// 経験値ゲージ表示
    /// </summary>
    [SerializeField] private Image _expFillImage;

    public void Start() {
        if (_characterParameter != null) {
            // 経験値更新イベントに登録
            _characterParameter.OnExpChanged += (exp) => UpdateExpGauge(exp);
            // 初期表示
            UpdateExpGauge();
        }
    }

    /// <summary>
    /// 経験値ゲージの更新
    /// </summary>
    public void UpdateExpGauge(int exp = 0) {
        var currentExp = _characterParameter.currentExp;
        var nextLevelExp = _characterParameter.nextLevelExp;
        var fillAmount = (float)currentExp / nextLevelExp;
        _expFillImage.fillAmount = fillAmount;
    }
}
