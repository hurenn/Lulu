using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Timeline上に置くだけでプレイヤーに経験値を与えるマーカー
public class LevelUpMarker : Marker, INotification {
    public int expAmount = 200; // 付与する経験値

    public PropertyName id => new PropertyName();
}
