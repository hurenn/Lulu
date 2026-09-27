using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Timeline上に置くと、指定した対象(IActivatableを実装したコンポーネントを持つGameObject)の行動開始を通知する汎用マーカー
public class ActivateMarker : Marker, INotification {
    public ExposedReference<GameObject> target;

    public PropertyName id => new PropertyName();
}
