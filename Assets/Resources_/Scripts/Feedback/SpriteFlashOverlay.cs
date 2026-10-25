using UnityEngine;

/// <summary>
/// 元のスプライトと同じ形の光を手前に重ねる(被弾時の白フラッシュ等)。
/// 色・アルファはFeel等で動かし、ここでは形・向き・並び順だけを元に合わせる
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(1000)]
public class SpriteFlashOverlay : MonoBehaviour {
    [SerializeField] private SpriteRenderer _source;
    private SpriteRenderer _flash;

    private void Reset() {
        if (transform.parent != null) _source = transform.parent.GetComponent<SpriteRenderer>();
    }

    private void Awake() {
        _flash = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 重ねる対象を設定し、その子に移る(Variantごとに見た目のオブジェクトが違っても、実行時に正しく重ねるため)
    /// </summary>
    public void Attach(SpriteRenderer source) {
        _source = source;
        if (source == null) {
            return;
        }
        transform.SetParent(source.transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void LateUpdate() {
        // 光っていない間・元が姿を消している間は描画しない
        bool is_visible = _source != null && _source.enabled && _flash.color.a > 0f;
        _flash.enabled = is_visible;
        if (!is_visible) {
            return;
        }
        _flash.sprite = _source.sprite;
        _flash.flipX = _source.flipX;
        _flash.flipY = _source.flipY;
        _flash.sortingLayerID = _source.sortingLayerID;
        _flash.sortingOrder = _source.sortingOrder + 1; // 本体の手前に重ねる
    }
}
