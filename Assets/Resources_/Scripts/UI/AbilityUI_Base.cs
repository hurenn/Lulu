using System.Collections;
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

    // UIキャンバス
    [SerializeField] private GameObject _effectPrefab;

    /// <summary>
    /// 能力UIの設定
    /// </summary>
    /// <param name="ability_type">能力タイプ</param>
    public void SetAbilityUI(eAbilityType ability_type, bool is_effect = true) {
        StartCoroutine(_JoinEffect(ability_type, is_effect));
    }

    /// <summary>
    /// 仲間の能力取得エフェクト
    /// </summary>
    /// <param name="ability_type"></param>
    /// <returns></returns>
    private IEnumerator _JoinEffect(eAbilityType ability_type, bool is_effect = true) {
        _UpdateUI(ability_type);
        yield return new WaitForSeconds(0.1f);
        if (is_effect) {
            Instantiate(_effectPrefab, _iconImage.transform);
        }
    }

    private void _UpdateUI(eAbilityType ability_type) {
        // UI表示更新
        switch (ability_type) {
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

    /*
    private IEnumerator _JoinAnimation(eAbilityType ability_type, Vector3 world_position) {
        // ワールド座標をスクリーン座標に変換
        Vector3 screen_position = Camera.main.WorldToScreenPoint(world_position);
        // スクリーン座標をUI_Canvas座標に変換
        Vector2 start_ui_position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _uiCanvas.transform as RectTransform,
            screen_position,
            Camera.main,
            out start_ui_position);

        // 目標位置
        Vector2 target_ui_position = _rect.anchoredPosition;

        // アニメーション実行
        float elapsed_time = 0f;
        while(elapsed_time < _animationDuration) {
            elapsed_time += Time.deltaTime;
            float t = elapsed_time / _animationDuration;

            // イージング
            t = Mathf.SmoothStep(0f, 1f, t);
            _rect.anchoredPosition = Vector2.Lerp(start_ui_position, target_ui_position, t);

            yield return null;
        }
        _UpdateUI(ability_type);
    }
    */
}
