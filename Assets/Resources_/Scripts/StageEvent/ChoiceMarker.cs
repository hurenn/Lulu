using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 選択肢1件分のデータ
[System.Serializable]
public class MessageChoiceOption {
    public string labelKey;                // 選択肢テキストのローカライズキー
    public MessageData[] branchMessages;    // 選択後に表示する軽量な分岐セリフ(任意、Timelineを使わない場合用)
    public PlayableDirector nextTimeline;   // 選択後に再生する分岐先Timeline(任意)
    public bool endsSequence;               // 分岐再生後、会話を終了する(元のTimelineを再開しない)
}

// Timeline上に配置する選択肢発生ポイント
public class ChoiceMarker : Marker, INotification {
    public MessageData prompt;              // 選択肢を出す前に表示する質問文(keyが空なら直前の表示を維持したまま選択肢のみ表示)
    public MessageChoiceOption[] choices;

    public PropertyName id => new PropertyName();
}
