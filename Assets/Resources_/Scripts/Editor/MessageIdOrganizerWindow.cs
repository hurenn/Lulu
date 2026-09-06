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
/// 指定したシーン群をHierarchy順(MessageTrigger+PlayableDirectorを深さ優先で走査)に
/// 沿ってID(ステージID2桁+連番3桁)へ振り直すロジック。GUIを持たないのでreflection等からも直接呼べる。
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

    private static readonly System.Reflection.FieldInfo _fiTrig =
        typeof(MessageTrigger).GetField("_messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    private static readonly System.Reflection.FieldInfo _fiPlayable =
        typeof(MessagePlayableAsset).GetField("messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    private static List<string> _GetTimelineKeys(TimelineAsset timeline, List<string> warnings) {
        var result = new List<string>();
        foreach (var track in timeline.GetOutputTracks()) {
            var clips = new List<TimelineClip>();
            foreach (var clip in track.GetClips()) if (clip.asset is MessagePlayableAsset) clips.Add(clip);
            clips.Sort((a, b) => a.start.CompareTo(b.start));
            foreach (var clip in clips) {
                var arr = _fiPlayable.GetValue(clip.asset as MessagePlayableAsset) as MessageData[];
                foreach (var md in arr) {
                    if (string.IsNullOrEmpty(md.key)) warnings.Add($"キー未設定のメッセージがあります(Timeline: {timeline.name})");
                    else result.Add(md.key);
                }
            }
        }
        return result;
    }

    private static void _Walk(Transform t, List<string> acc, HashSet<PlayableAsset> consumed, List<string> warnings) {
        var trig = t.GetComponent<MessageTrigger>();
        if (trig != null) {
            var mdl = _fiTrig.GetValue(trig) as MessageDataList;
            if (mdl?.messageDatas != null) {
                foreach (var md in mdl.messageDatas) {
                    if (string.IsNullOrEmpty(md.key)) warnings.Add($"キー未設定のメッセージがあります: {t.name}");
                    else acc.Add(md.key);
                }
            }
        }
        var dir = t.GetComponent<PlayableDirector>();
        if (dir != null && dir.playableAsset is TimelineAsset timeline && !consumed.Contains(dir.playableAsset)) {
            var keys = _GetTimelineKeys(timeline, warnings);
            if (keys.Count > 0) {
                acc.AddRange(keys);
                consumed.Add(dir.playableAsset);
            }
        }
        for (int i = 0; i < t.childCount; i++) _Walk(t.GetChild(i), acc, consumed, warnings);
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
        if (scenePathsInOrder.Count == 0) {
            result.warnings.Add("対象シーンが選択されていません。");
            return result;
        }

        var orderedOldKeys = new List<string>();
        var consumed = new HashSet<PlayableAsset>();
        foreach (var scenePath in scenePathsInOrder) {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) _Walk(root.transform, orderedOldKeys, consumed, result.warnings);
        }

        var seen = new HashSet<string>();
        foreach (var k in orderedOldKeys) if (!seen.Add(k)) result.warnings.Add("重複キー: " + k);

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

        int existingStageCount = table.Keys.Count(k => MessageJsonUtil.IsValidKey(k) && k.StartsWith(stageId));
        if (existingStageCount != orderedOldKeys.Count) {
            result.warnings.Add($"JSON内の既存ステージ{stageId}件数({existingStageCount})と選択シーンから見つかった件数({orderedOldKeys.Count})が一致しません。シーンの選択漏れ・余分がないか確認してください。");
        }

        return result;
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

        var consumedTimelines = new HashSet<PlayableAsset>();
        foreach (var scenePath in scenePathsInOrder) {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            foreach (var trig in UnityEngine.Object.FindObjectsByType<MessageTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var so = new SerializedObject(trig);
                var arr = so.FindProperty("_messageDatas")?.FindPropertyRelative("messageDatas");
                if (arr == null) continue;
                for (int idx = 0; idx < arr.arraySize; idx++) {
                    var keyProp = arr.GetArrayElementAtIndex(idx).FindPropertyRelative("key");
                    if (mapping.TryGetValue(keyProp.stringValue, out var nk)) { keyProp.stringValue = nk; changed = true; }
                }
                so.ApplyModifiedProperties();
            }

            foreach (var dir in UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!(dir.playableAsset is TimelineAsset timeline) || consumedTimelines.Contains(dir.playableAsset)) continue;
                bool tlChanged = false;
                foreach (var track in timeline.GetOutputTracks()) {
                    foreach (var clip in track.GetClips()) {
                        if (!(clip.asset is MessagePlayableAsset mpa)) continue;
                        var so2 = new SerializedObject(mpa);
                        var arr2 = so2.FindProperty("messageDatas");
                        for (int idx = 0; idx < arr2.arraySize; idx++) {
                            var keyProp2 = arr2.GetArrayElementAtIndex(idx).FindPropertyRelative("key");
                            if (mapping.TryGetValue(keyProp2.stringValue, out var nk2)) { keyProp2.stringValue = nk2; tlChanged = true; }
                        }
                        so2.ApplyModifiedProperties();
                        EditorUtility.SetDirty(mpa);
                    }
                }
                if (tlChanged) AssetDatabase.SaveAssets();
                consumedTimelines.Add(dir.playableAsset);
            }

            if (changed) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
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
            "チェックしたシーンを上から順に開き、各シーンのHierarchyをMessageTrigger/PlayableDirectorの出現順(深さ優先)で走査してIDを振り直します。\n" +
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
