---
name: daily-debug
description: Luluプロジェクトの日次デバッグ。スクリプトを調査して不具合・懸念点を洗い出し、ユーザー判断が不要なものは日付ブランチ上で自動修正し、最後に報告書を作成する。ユーザーが /daily-debug を実行したとき、または「日次デバッグ」「今日のデバッグ」を依頼したときに使う。
allowed-tools: Read Grep Glob Edit Write Agent Bash(git status*) Bash(git diff*) Bash(git log*) Bash(git show*) Bash(git rev-parse*) Bash(git branch*) Bash(git switch*) Bash(git checkout*) Bash(git add*) Bash(git commit*) Bash(file *) Bash(grep *) Bash(iconv *) Bash(mkdir *) Bash(date*) Bash(cat *) Bash(ls *) mcp__UnityMCP__refresh_unity mcp__UnityMCP__read_console mcp__UnityMCP__validate_script mcp__UnityMCP__find_in_file mcp__UnityMCP__manage_editor
---

# 日次デバッグ (/daily-debug)

このスキルの起動自体が「ユーザー判断が不要な修正はまとめて自動で進めてよい」という明示的な許可である。
通常の「1件ずつ確認を取る」ルールはこのスキル実行中に限り適用しない。代わりに、判断が必要なものは修正せず報告書に回す。

## 作業ディレクトリと状態ファイル

- 状態・報告書の置き場: `.claude/daily-debug/`(git管理外)
  - `state.md` … 前回調査したコミット、ローテーション位置、保留中の要判断項目、ユーザーが却下した項目
  - `reports/YYYY-MM-DD.md` … その日の報告書
- 初回で `state.md` が無ければ作成する(テンプレートは末尾参照)。

## 手順

### 1. 事前チェック(満たさなければ中断して理由を報告)
1. `git status --porcelain` が空であること。未コミットの変更がある場合は**何も変更せず**、「作業ツリーが汚れているため中断」とユーザーに伝えて、続行方法(コミット/スタッシュ/調査のみ実施)を尋ねる。勝手にstashしない。
2. 現在のブランチ名を記録する(`ORIGINAL_BRANCH`)。`claude/daily-fix/` で始まるブランチ上にいる場合は中断して確認する。
3. `.claude/daily-debug/state.md` を読み、前回の調査コミット・保留項目・却下項目を把握する。
4. UnityMCPが使えるか確認する(`mcp__UnityMCP__read_console` を1回呼ぶ)。応答が無ければ「Unity未接続」として静的解析のみで進める。応答が無くても**繰り返し呼び直さない**。

### 2. 調査範囲の決定
対象は `Assets/Resources_/Scripts/` 配下のみ。`Assets_OLD`、`DOTween`、`TextMesh Pro`、`Synaptic AI Pro`、`ParticleEffectForUGUI` などのサードパーティ/旧資産は対象外。

- **重点範囲**: 前回調査コミットから `HEAD` までに変更された `.cs` ファイル(`git diff --name-only <前回コミット>..HEAD -- Assets/Resources_/Scripts`)と、それらを直接参照しているファイル。
- **ローテーション範囲**: 以下のフォルダから `state.md` のローテーション位置にある1つを毎回追加で精査し、位置を1つ進める。
  `Abilities` → `Character` → `Battle` → `Warp` → `StageEvent` → `StageObject` → `UI`(+`UI/Pause`) → `Localization`+`Editor`+直下ファイル → 先頭へ
- 初回(前回コミットが無い)は全スクリプトを対象とする。

### 3. 調査(並列エージェント)
範囲が広い場合は Agent ツールで最大4並列に分割して調査させる(読み取り専用の調査に限定させ、修正はメインで行う)。各エージェントには以下の観点と、下記「プロジェクト固有の注意点」を渡す。

観点:
- 実行時例外: null参照、破棄済みオブジェクトへのアクセス、配列範囲外、`GetComponent` 結果の未チェック
- Unityライフサイクルの誤り: Awake/Start/OnEnable順序依存、`OnDestroy`でのイベント購読解除漏れ、シーン遷移を跨ぐ参照、DontDestroyOnLoad系
- 物理/時間: `Update`と`FixedUpdate`の取り違え、`Time.deltaTime`/`fixedDeltaTime`の乗算漏れ、`timeScale=0`(ポーズ・ヒットストップ)時の挙動
- コルーチン/DOTween: 停止漏れ、オブジェクト破棄後のTween継続(`SetLink`/`Kill`漏れ)
- ロジックの誤り: 条件式の誤り、コピペミス、状態フラグのリセット漏れ、到達不能コード
- 性能: 毎フレームのGCアロケーション、`Find`系/`GetComponent`の毎フレーム呼び出し
- 文字化け(U+FFFD)やエンコーディング問題

各指摘には「ファイル:行」「具体的な発生シナリオ」「確度(確実/高い/推測)」を付けさせる。**推測レベルの指摘は自分でコードを読んで裏取りし、再現シナリオが説明できないものは捨てる。** `state.md` の却下済み項目と同じものは再提案しない。

### 4. 分類
裏取りできた指摘を次の2つに分ける。

**自動修正してよいもの(すべて満たす)**
- 不具合であることが明確で、正しい挙動が1通りに決まる
- 修正がスクリプト内で完結し、ゲームの見た目・手触り・バランス・仕様を変えない
- 修正範囲が小さい(目安: 1件あたり数ファイル・数十行以内)
- 例: nullチェック追加、購読解除漏れ、Tween/コルーチンの停止漏れ、明らかなタイプミス、deltaTime乗算漏れ(ただし速度値が変わるものは下記)

**ユーザー判断に回すもの(1つでも該当)**
- ゲームの挙動・見た目・速度・タイミング・難易度が変わる(例: deltaTime修正でプレハブの値の再スケールが必要になるケース)
- 意図的な仕様かバグか判断できない
- 設計変更・リファクタリング・クラス構成の変更を伴う
- シーン/プレハブ/ScriptableObject/アセットの変更が必要
- 公開APIやシリアライズフィールドの名前・型変更(インスペクタの設定値が消える恐れ)
- 削除を伴う(ファイル・未使用コードの削除含む)

### 5. 修正
1. 自動修正対象が1件以上ある場合のみ、`git switch -c claude/daily-fix/YYYY-MM-DD`(同名があれば `-2` などを付与)でブランチを作成する。
2. 1件ずつ修正する。編集前に `file <path>` を確認し、`Non-ISO extended-ASCII text` ならShift-JISの可能性があるので `iconv -f CP932 -t UTF-8` で変換してから編集する。編集後は `grep -c $'\xef\xbf\xbd' <path>` で文字化けが0であることを確認する。
3. 周辺コードのスタイル(命名、コメント密度、日本語コメント)に合わせる。修正に関係ない整形・リネームはしない。
4. Unity接続時: 全修正後に `mcp__UnityMCP__refresh_unity`(compile:"request", scope:"scripts") → `mcp__UnityMCP__read_console`(types:["error"]) でコンパイルエラーが無いことを確認する。エラーが出た修正は直すか、直せなければその修正を取り消して「要判断」に回す。`execute_menu_item` やPlay Mode実行など重い操作はしない。
   Unity未接続時: 修正は静的に十分確信できるものに限定し、報告書に「コンパイル未確認」と明記する。
5. 修正1件ごとに1コミットする。メッセージは `[daily-debug] <ファイル名>: <修正内容の要約>` 形式で、末尾にこのセッションの指示どおりのCo-Authored-By行を付ける。
6. 最後に `git switch <ORIGINAL_BRANCH>` で元のブランチに戻す。**push・マージ・PR作成はしない。**

### 6. 報告
`.claude/daily-debug/reports/YYYY-MM-DD.md` に以下の構成で書き出し、同じ内容の要約をターミナルにも表示する。

```
# 日次デバッグ報告 YYYY-MM-DD
- 調査範囲: <コミット範囲> / ローテーション: <フォルダ>
- Unity: 接続あり(コンパイルOK) | 未接続(静的解析のみ)
- 修正ブランチ: claude/daily-fix/YYYY-MM-DD(N件のコミット) | 修正なし

## 自動修正した項目
### 1. <タイトル>
- 場所: `path:line`
- 問題: <どういう状況で何が起きるか>
- 修正: <何をどう変えたか>
- コミット: <短縮ハッシュ>

## 要判断の項目(未修正)
### A. <タイトル>
- 場所 / 問題 / 発生シナリオ
- 選択肢: <修正案と、それぞれの影響>
- 推奨: <あれば>

## 前回から持ち越している要判断項目
<state.mdの保留項目で未解決のもの。解決済みになっていれば削除>

## マージ方法
git diff <ORIGINAL_BRANCH>...claude/daily-fix/YYYY-MM-DD
git merge claude/daily-fix/YYYY-MM-DD
```

最後に `state.md` を更新する(前回調査コミット=今回の `HEAD`(元ブランチ)、ローテーション位置、保留項目の追加・解消)。
ユーザーが報告を見て「これは仕様」「直さなくていい」と答えた項目は、`state.md` の却下リストに追記する。

## プロジェクト固有の注意点
- 2Dアクション、Unity 6000.0.40f1、New Input System、Cinemachine、DOTween。
- Bスロットのジャンプは `Ability_Warp.ExecuteSimple()` が `eAbilityResult.Jump` を返すことで実装されている(仕様)。
- `AbilityUIManager` の `_abilityUI_Y/X/A/B` は名前と異なり能力タイプ専用UIへの参照(仕様)。
- シングルトンは `PersistentSingleton<T>` / `SceneSingleton<T>` に統一済み。`GameSceneManager` は `Required Objects.prefab` 内の非ルートオブジェクトなので、DontDestroyOnLoad系を触る場合はルートかどうかを必ず確認する。
- 衝突判定系(`DamageZone.cs` 等)で `PlayerCharacterManager` を使わず所有関係で判定している箇所はユーザー判断で現状維持。
- メッセージは自前JSON+key方式でローカライズされている。キー文字列の変更は要判断。

## state.md テンプレート
```
# daily-debug state
- 前回調査コミット: (なし)
- 次回ローテーション: Abilities

## 保留中の要判断項目
(なし)

## 却下済み(再提案しない)
(なし)
```
