using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// 指定したシーン群をHierarchy順(深さ優先)に走査し、メッセージキーを持つ全コンポーネント/Timelineの
/// 出現順に沿ってID(ステージID2桁+連番3桁)へ振り直すロジック。GUIを持たないのでreflection等からも直接呼べる。
/// </summary>
public static class MessageIdOrganizer {
    public class PreviewEntry {
        public string oldKey;
        public string newKey;
        public string ja;
    }

    public class PreviewResult {
        public List<PreviewEntry> entries = new List<PreviewEntry>();
        public List<string> warnings = new List<string>();
        public bool ok => warnings.Count == 0 && entries.Count > 0;
    }

    // MessageData.key / MessageChoiceOption.labelKey をシリアライズ順に列挙する。
    // MessageTrigger/TalkTrigger/SubQuest系/ChoiceMarker等、メッセージを持つ型が増えても追従できるよう型は限定しない
    private static List<SerializedProperty> _KeyProps(SerializedObject so) {
        var result = new List<SerializedProperty>();
        var it = so.GetIterator();
        while (it.Next(true)) {
            if (it.propertyType != SerializedPropertyType.String) continue;
            string ownerType;
            if (it.name == "key") ownerType = nameof(MessageData);
            else if (it.name == "labelKey") ownerType = nameof(MessageChoiceOption);
            else continue;
            int dot = it.propertyPath.LastIndexOf('.');
            if (dot < 0 || so.FindProperty(it.propertyPath.Substring(0, dot))?.type != ownerType) continue;
            result.Add(it.Copy());
        }
        return result;
    }

    // Timeline内のクリップ・マーカーを全トラック横断で時刻順に並べる(同時刻はトラック順)
    private static List<UnityEngine.Object> _TimelineItems(TimelineAsset timeline) {
        var items = new List<(double time, UnityEngine.Object obj)>();
        var tracks = new HashSet<TrackAsset>();
        foreach (var track in timeline.GetOutputTracks().Append(timeline.markerTrack)) {
            if (track == null || !tracks.Add(track)) continue;
            foreach (var clip in track.GetClips()) if (clip.asset != null) items.Add((clip.start, clip.asset));
            foreach (var marker in track.GetMarkers()) if (marker is UnityEngine.Object mo) items.Add((marker.time, mo));
        }
        return items.OrderBy(x => x.time).Select(x => x.obj).ToList();
    }

    private static IEnumerable<UnityEngine.Object> _AllTimelineAssets() {
        foreach (var guid in AssetDatabase.FindAssets("t:TimelineAsset")) {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (timeline != null) foreach (var item in _TimelineItems(timeline)) yield return item;
        }
    }

    private static void _Collect(UnityEngine.Object obj, string where, string stageId, List<string> acc, List<string> warnings) {
        foreach (var p in _KeyProps(new SerializedObject(obj))) {
            var key = p.stringValue;
            if (string.IsNullOrEmpty(key)) {
                // ChoiceMarker.promptは空キー=「直前の表示を維持」の意味で正当
                if (p.propertyPath != "prompt.key") warnings.Add($"キー未設定のメッセージがあります: {where} ({p.propertyPath})");
                continue;
            }
            // 他ステージのメッセージを流用している箇所は、そのステージの採番を崩さないよう対象外。
            // ID形式でないキー(ZZ_Test系のテスト用キー等)も採番対象外
            if (!MessageJsonUtil.IsValidKey(key) || !key.StartsWith(stageId)) continue;
            acc.Add(key);
        }
    }

    private static void _Walk(Transform t, string stageId, List<string> acc, HashSet<PlayableAsset> consumed, List<string> warnings) {
        foreach (var mb in t.GetComponents<MonoBehaviour>()) {
            if (mb != null) _Collect(mb, $"{t.name} [{mb.GetType().Name}]", stageId, acc, warnings);
        }
        var dir = t.GetComponent<PlayableDirector>();
        if (dir != null && dir.playableAsset is TimelineAsset timeline && consumed.Add(timeline)) {
            foreach (var item in _TimelineItems(timeline)) _Collect(item, $"Timeline {timeline.name}", stageId, acc, warnings);
        }
        for (int i = 0; i < t.childCount; i++) _Walk(t.GetChild(i), stageId, acc, consumed, warnings);
    }

    // 対象シーンを順に開いて処理する。開いていなかったシーンは追加で開き、処理後に閉じる(ユーザーの開いているシーンを差し替えない)
    private static void _ForEachScene(IList<string> scenePaths, Action<Scene> action) {
        foreach (var scenePath in scenePaths) {
            var scene = SceneManager.GetSceneByPath(scenePath);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            action(scene);
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>チェックされたシーンを渡された順に走査し、新IDへの付け替え案を計算する(書き込みは行わない)。</summary>
    public static PreviewResult ComputePreview(IList<string> scenePathsInOrder, string stageId) {
        var result = new PreviewResult();
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).isDirty) {
                result.warnings.Add("未保存のシーンがあります。保存してから実行してください: " + SceneManager.GetSceneAt(i).path);
                return result;
            }
        }
        if (MessageJsonUtil.HasPending) {
            result.warnings.Add("JSONに未反映の変更があります。先に「Lulu/Localization/JSONへ反映」を実行してください。");
            return result;
        }
        if (scenePathsInOrder.Count == 0) {
            result.warnings.Add("対象シーンが選択されていません。");
            return result;
        }

        var foundKeys = new List<string>();
        var consumed = new HashSet<PlayableAsset>();
        _ForEachScene(scenePathsInOrder, scene => {
            foreach (var root in scene.GetRootGameObjects()) _Walk(root.transform, stageId, foundKeys, consumed, result.warnings);
        });

        // 同じキーを複数箇所から参照している場合(共通の選択肢ラベル、TalkTriggerとTimelineの併用等)は最初の出現位置で採番する
        var orderedOldKeys = foundKeys.Distinct().ToList();

        var table = MessageJsonUtil.LoadTable();
        var oldKeySet = new HashSet<string>(orderedOldKeys);
        for (int i = 0; i < orderedOldKeys.Count; i++) {
            var oldKey = orderedOldKeys[i];
            var newKey = stageId + (i + 1).ToString("D3");
            if (table.ContainsKey(newKey) && !oldKeySet.Contains(newKey)) {
                result.warnings.Add($"新ID {newKey} は既存の別メッセージと衝突します(対象シーンの選択漏れの可能性があります): oldKey={oldKey}");
            }
            table.TryGetValue(oldKey, out var entry);
            result.entries.Add(new PreviewEntry {
                oldKey = oldKey,
                newKey = newKey,
                ja = entry != null ? entry.GetValueOrDefault("ja", "") : "(JSON未登録)"
            });
        }

        var unreferenced = table.Keys.Where(k => MessageJsonUtil.IsValidKey(k) && k.StartsWith(stageId) && !oldKeySet.Contains(k)).OrderBy(k => k).ToList();
        if (unreferenced.Count > 0) {
            result.warnings.Add($"JSON内のステージ{stageId}のうち、選択シーンから参照されていないキーが{unreferenced.Count}件あります。シーンの選択漏れか、不要になったメッセージがないか確認してください: " + string.Join(", ", unreferenced));
        }

        return result;
    }

    private static bool _Rewrite(UnityEngine.Object obj, Dictionary<string, string> mapping) {
        var so = new SerializedObject(obj);
        bool changed = false;
        foreach (var p in _KeyProps(so)) {
            if (mapping.TryGetValue(p.stringValue, out var nk)) { p.stringValue = nk; changed = true; }
        }
        if (changed) {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(obj);
        }
        return changed;
    }

    /// <summary>ComputePreviewで作った案をJSON・シーン・Timelineへ実際に書き込む。</summary>
    public static void Apply(IList<string> scenePathsInOrder, PreviewResult preview) {
        if (!preview.ok) throw new InvalidOperationException("警告が解消されていないため適用できません。");

        var mapping = new Dictionary<string, string>();
        foreach (var e in preview.entries) mapping[e.oldKey] = e.newKey;

        var table = MessageJsonUtil.LoadTable();
        var newTable = new Dictionary<string, Dictionary<string, string>>();
        foreach (var kv in table) {
            newTable[mapping.TryGetValue(kv.Key, out var newKey) ? newKey : kv.Key] = kv.Value;
        }
        MessageJsonUtil.SaveTable(newTable);

        // Timelineは複数シーンから共有されうるため、選択シーンに限らずプロジェクト内の全Timelineを書き換える
        foreach (var item in _AllTimelineAssets()) _Rewrite(item, mapping);
        AssetDatabase.SaveAssets();

        _ForEachScene(scenePathsInOrder, scene => {
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                    if (mb != null && _Rewrite(mb, mapping)) changed = true;
                }
            }
            if (changed) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        });
    }
}

/// <summary>
/// MessageIdOrganizerのUI。メニュー「Lulu/Localization/ID整理ウィンドウ」から開く。
/// </summary>
public class MessageIdOrganizerWindow : EditorWindow {
    private List<string> _scenePaths = new List<string>();
    private HashSet<string> _checked = new HashSet<string>();
    private string _stageId = "01";
    private MessageIdOrganizer.PreviewResult _preview;
    private Vector2 _scroll;

    [MenuItem("Lulu/Localization/ID整理ウィンドウ")]
    public static void Open() => GetWindow<MessageIdOrganizerWindow>("メッセージID整理");

    private void OnEnable() {
        _scenePaths = MessageJsonUtil.AllScenePaths().OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    private void OnGUI() {
        EditorGUILayout.HelpBox(
            "チェックしたシーンを上から順に開き、各シーンのHierarchyを深さ優先で走査し、メッセージを持つコンポーネント・Timeline(クリップ/選択肢マーカーを時刻順)の出現順でIDを振り直します。\n" +
            "他ステージのIDを流用している箇所は対象外です。同じIDを複数箇所で使っている場合は最初の出現位置で採番します。\n" +
            "対象シーン・順序は誤検出を避けるため必ず手動で選んでください(自動判定はしません)。",
            MessageType.Info);

        _stageId = EditorGUILayout.TextField("ステージID(2桁)", _stageId);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("対象シーン(チェックした順に処理されます。▲▼で並べ替え)", EditorStyles.boldLabel);
        for (int i = 0; i < _scenePaths.Count; i++) {
            var path = _scenePaths[i];
            EditorGUILayout.BeginHorizontal();
            bool wasChecked = _checked.Contains(path);
            bool nowChecked = EditorGUILayout.ToggleLeft(Path.GetFileNameWithoutExtension(path), wasChecked, GUILayout.Width(240));
            if (nowChecked != wasChecked) { if (nowChecked) _checked.Add(path); else _checked.Remove(path); }

            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(24))) {
                (_scenePaths[i], _scenePaths[i - 1]) = (_scenePaths[i - 1], _scenePaths[i]);
            }
            GUI.enabled = i < _scenePaths.Count - 1;
            if (GUILayout.Button("▼", GUILayout.Width(24))) {
                (_scenePaths[i], _scenePaths[i + 1]) = (_scenePaths[i + 1], _scenePaths[i]);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("プレビュー")) {
            var ordered = _scenePaths.Where(p => _checked.Contains(p)).ToList();
            _preview = MessageIdOrganizer.ComputePreview(ordered, _stageId);
        }

        if (_preview != null) {
            EditorGUILayout.Space();
            foreach (var w in _preview.warnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
            EditorGUILayout.LabelField($"件数: {_preview.entries.Count}");

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(280));
            foreach (var e in _preview.entries) {
                EditorGUILayout.LabelField($"{e.oldKey} → {e.newKey}   {e.ja}");
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            GUI.enabled = _preview.ok;
            if (GUILayout.Button("適用")) {
                var ordered = _scenePaths.Where(p => _checked.Contains(p)).ToList();
                MessageIdOrganizer.Apply(ordered, _preview);
                _preview = null;
                MessageTextTool.CheckIntegrity();
                EditorUtility.DisplayDialog("完了", "ID整理が完了しました。詳細はConsoleの整合性チェック結果を確認してください。", "OK");
            }
            GUI.enabled = true;
        }
    }
}
