using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 1トリガー(MessageTrigger)または1Timelineクリップ(MessagePlayableAsset)分の
/// メッセージ列とメタデータを保持するアセット
/// </summary>
[CreateAssetMenu(fileName = "MessageSequence", menuName = "Lulu/MessageSequenceAsset")]
public class MessageSequenceAsset : ScriptableObject {
    [System.Serializable]
    public class MessageEntry {
        public LocalizedString text;   // メッセージ本文(String Tableでja/enを切替)
        public Sprite characterIcon;   // キャラクターアイコン
        public float addShowTime;      // 追加表示時間
        public bool isAutoForce;       // 自動送りかどうか
        public bool isUnScaledTime;    // UnscaledTimeを使うかどうか
    }

    public MessageEntry[] messages;    // メッセージデータ配列

    [Header("トリガー用設定")]
    public bool isForced;              // 強制メッセージかどうか(MessageTrigger用)

    [Header("ウィンドウ振動(Timelineクリップ用)")]
    public bool shakeWindow;
    public float shakeIntensity = 20f;
    public float shakeDuration = 0.4f;
}
