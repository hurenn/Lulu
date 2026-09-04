# MessageSequenceAsset について

シーン・Timelineに直書きされていたメッセージ本文(日本語/英語)とメタデータを、外部化・再利用可能な形に切り出したScriptableObjectです。
**1つの`MessageTrigger`または1つのTimelineクリップ(`MessagePlayableAsset`)＝1つのアセット**という対応関係になっています。

## 保存場所

```
Assets/Resources_/Localization/MessageSequences/
├── Stage1-1/  ～  Stage1-4/    …シーンのMessageTrigger用(トリガーのGameObject名でファイル名を付与)
└── Timelines/{Timeline名}/     …Timelineクリップ用(clip_0.asset, clip_1.asset...)
```

## 中身の構造(`Assets/Resources_/Scripts/UI/MessageSequenceAsset.cs`)

| フィールド | 意味 |
|---|---|
| `messages` (`MessageEntry[]`) | 1トリガー/1クリップ内の連続メッセージ配列 |
| `isForced` | 強制メッセージか(`MessageTrigger`用。他のメッセージを中断して割り込む) |
| `shakeWindow`/`shakeIntensity`/`shakeDuration` | メッセージウィンドウ振動演出(Timelineクリップ用) |

`MessageEntry`(各メッセージ1件分):

| フィールド | 意味 |
|---|---|
| `text` (`LocalizedString`) | 本文。実体はStringテーブル"Message"のキー参照(ja/enはテーブル側で管理、このアセット自体には文字列を持たない) |
| `characterIcon` | 話者アイコン(名前表示にも使われる) |
| `addShowTime` | 基本表示時間への追加(マイナス値も可) |
| `isAutoForce` | 自動送りか(falseならプレイヤー入力待ち) |
| `isUnScaledTime` | Time.timeScaleの影響を受けないか |

## テキストの実体(Stringテーブル)

- `Assets/Resources_/Localization/Tables/Message.asset`(コレクション)+`Message_ja.asset`/`Message_en.asset`(言語別テーブル)
- キー命名規則: シーンは`{シーン名}.{トリガーGameObject名}.{メッセージ番号}`(例: `Stage1_1.MessageTrigger_wall.0`)、Timelineは`{Timeline名}.clip{クリップ番号}.{メッセージ番号}`
- 本文を直接編集したい場合は、Unity Editorの「Window > Asset Management > Localization Tables」からMessageコレクションを開いて直接編集するか、`MessageCsvTool`でCSV経由で編集する(外部翻訳の手順は[Translations/README.md](../Translations/README.md)参照)

## 誰が参照しているか

- `MessageTrigger.cs`の`_messageSequence`フィールド
- `MessagePlayableAsset.cs`(Timelineクリップ)の`_messageSequence`フィールド

どちらもInspector上でこのアセットをドラッグ&ドロップして差し替えられる。

## 新しいメッセージトリガー/クリップを追加する手順

1. Stringテーブルに新しいキーを追加(Localization Tables画面、またはコードで`table.AddEntry(key, text)`)してja/en本文を入力する
2. `Assets > Create > Lulu > MessageSequenceAsset`で新規アセットを作成し、適切なフォルダに保存する
3. `messages`配列に`MessageEntry`を追加し、`text`にInspectorから該当Stringテーブルキーを割り当て、アイコン等のメタデータを設定する
4. シーン上の`MessageTrigger`(またはTimeline上の`MessagePlayableAsset`クリップ)の`_messageSequence`にこのアセットをセットする
