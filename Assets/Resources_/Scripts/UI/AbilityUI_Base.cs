using UnityEngine;
using UnityEngine.UI;

public class AbilityUI_Base : MonoBehaviour
{
    /// <summary>
    /// アイコン画像
    /// </summary>
    [SerializeField]
    protected Image _iconImage;

    [SerializeField] protected Sprite _iceIconSprite;
    [SerializeField] protected Sprite _lightIconSprite;
    [SerializeField] protected Sprite _fireIconSprite;

    /// <summary>
    /// 能力UIの設定
    /// </summary>
    /// <param name="ability_type">能力タイプ</param>
    public void SetAbilityUI(eAbilityType ability_type) {
        switch(ability_type) {
            case eAbilityType.Ice:
                _iconImage.sprite = _iceIconSprite;
                break;
            case eAbilityType.Light:
                _iconImage.sprite = _lightIconSprite;
                break;
            case eAbilityType.Fire:
                _iconImage.sprite = _fireIconSprite;
                break;
            default:
                Debug.LogError("不明な能力タイプ：" + ability_type);
                break;
        }
    }
}
