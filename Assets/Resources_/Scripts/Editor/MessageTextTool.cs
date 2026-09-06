using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// メッセージ本文テーブル(Assets/Resources/Localization/Messages.json)の運用ツール。
/// シーン/Timelineに新しいメッセージを追加した際のキー自動採番と、キー参照の整合性チェックを行う。
/// </summary>
public static class MessageTextTool {
    private const string _JSON_ASSET_PATH = "Assets/Resources/Localization/Messages.json";

    // キーは「ステージID2桁+メッセージID3桁」の5桁数値文字列(例: 01001)。
    // シーン名/Timeline名の"Stage(\d+)"または先頭の"数字_"からステージ番号を判定し、無ければ00とする。
    private static string _StageIdFromName(string name) {
        var m = Regex.Match(name, @"Stage(\d+)");
        if (!m.Success) m = Regex.Match(name, @"^(\d+)_");
        return m.Success ? int.Parse(m.Groups[1].Value).ToString("D2") : "00";
    }

    // ビルド設定のシーン一覧だけでは未登録シーンを見落とすため、プロジェクトの全シーンを対象にする
    private static IEnumerable<string> _EnabledScenePaths() {
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Resources_/Scenes" })) {
            yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }

    private static Dictionary<string, Dictionary<string, string>> _LoadTable() {
        if (!File.Exists(_JSON_ASSET_PATH)) return new Dictionary<string, Dictionary<string, string>>();
        var json = File.ReadAllText(_JSON_ASSET_PATH);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
               ?? new Dictionary<string, Dictionary<string, string>>();
    }

    private static void _SaveTable(Dictionary<string, Dictionary<string, string>> table) {
        var sorted = new SortedDictionary<string, Dictionary<string, string>>(table);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(sorted, Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(_JSON_ASSET_PATH));
        File.WriteAllText(_JSON_ASSET_PATH, json, new System.Text.UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    [MenuItem("Lulu/Localization/未設定キーへ自動採番")]
    public static void AssignMissingKeys() {
        if (EditorApplication.isPlaying) { Debug.LogError("Play Mode中は実行できません。"); return; }
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("未保存のシーンがあります。保存してから実行してください。"); return; }
        }

        var table = _LoadTable();
        int added = 0;

        // 既存キー(5桁ID)からステージごとの現在の最大番号を復元し、そこから連番を継続する
        var nextNumber = new Dictionary<string, int>();
        foreach (var k in table.Keys) {
            if (!Regex.IsMatch(k, @"^\d{5}$")) continue;
            var stage = k.Substring(0, 2);
            var num = int.Parse(k.Substring(2));
            if (!nextNumber.TryGetValue(stage, out var cur) || num > cur) nextNumber[stage] = num;
        }
        string NewKey(string stageId) {
            nextNumber.TryGetValue(stageId, out var cur);
            cur++;
            nextNumber[stageId] = cur;
            return stageId + cur.ToString("D3");
        }

        var fiTrig = typeof(MessageTrigger).GetField("_messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var scenePath in _EnabledScenePaths()) {
            if (!scenePath.StartsWith("Assets/")) continue;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            var stageId = _StageIdFromName(Path.GetFileNameWithoutExtension(scenePath));
            foreach (var trig in Object.FindObjectsByType<MessageTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var so = new SerializedObject(trig);
                var arr = so.FindProperty("_messageDatas")?.FindPropertyRelative("messageDatas");
                if (arr == null) continue;
                for (int idx = 0; idx < arr.arraySize; idx++) {
                    var keyProp = arr.GetArrayElementAtIndex(idx).FindPropertyRelative("key");
                    if (!string.IsNullOrEmpty(keyProp.stringValue)) continue;
                    var key = NewKey(stageId);
                    keyProp.stringValue = key;
                    table[key] = new Dictionary<string, string> { { "ja", "" }, { "en", "" } };
                    added++;
                    changed = true;
                }
                if (changed) so.ApplyModifiedProperties();
            }
            if (changed) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        var fiPlayable = typeof(MessagePlayableAsset).GetField("messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var guid in AssetDatabase.FindAssets("t:TimelineAsset")) {
            var tlPath = AssetDatabase.GUIDToAssetPath(guid);
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tlPath);
            var stageId = _StageIdFromName(Path.GetFileNameWithoutExtension(tlPath));
            bool changed = false;
            foreach (var track in timeline.GetOutputTracks()) {
                foreach (var clip in track.GetClips()) {
                    if (!(clip.asset is MessagePlayableAsset mpa)) continue;
                    var so = new SerializedObject(mpa);
                    var arr = so.FindProperty("messageDatas");
                    for (int idx = 0; idx < arr.arraySize; idx++) {
                        var keyProp = arr.GetArrayElementAtIndex(idx).FindPropertyRelative("key");
                        if (string.IsNullOrEmpty(keyProp.stringValue)) {
                            var key = NewKey(stageId);
                            keyProp.stringValue = key;
                            table[key] = new Dictionary<string, string> { { "ja", "" }, { "en", "" } };
                            added++;
                            changed = true;
                        }
                    }
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mpa);
                }
            }
            if (changed) AssetDatabase.SaveAssets();
        }

        _SaveTable(table);
        Debug.Log($"未設定キーへの自動採番が完了しました。新規キー追加数: {added}");
    }

    [MenuItem("Lulu/Localization/整合性チェック")]
    public static void CheckIntegrity() {
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("未保存のシーンがあります。保存してから実行してください。"); return; }
        }

        var table = _LoadTable();
        var usedKeys = new HashSet<string>();
        int emptyKeyCount = 0, missingCount = 0, duplicateCount = 0, missingLangCount = 0;

        void CheckArray(MessageData[] arr, string where) {
            if (arr == null) return;
            foreach (var md in arr) {
                if (string.IsNullOrEmpty(md.key)) { Debug.LogError($"キー未設定: {where}"); emptyKeyCount++; continue; }
                if (!usedKeys.Add(md.key)) { Debug.LogError($"重複キー: {md.key} ({where})"); duplicateCount++; }
                if (!table.TryGetValue(md.key, out var entry)) { Debug.LogError($"JSONに存在しないキー: {md.key} ({where})"); missingCount++; continue; }
                if (!entry.TryGetValue("ja", out var ja) || string.IsNullOrEmpty(ja) || !entry.TryGetValue("en", out var en) || string.IsNullOrEmpty(en)) {
                    Debug.LogWarning($"未翻訳のテキストがあります: {md.key} ({where})");
                    missingLangCount++;
                }
            }
        }

        var fiTrig = typeof(MessageTrigger).GetField("_messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var scenePath in _EnabledScenePaths()) {
            if (!scenePath.StartsWith("Assets/")) continue;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var trig in Object.FindObjectsByType<MessageTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var mdl = fiTrig.GetValue(trig) as MessageDataList;
                CheckArray(mdl?.messageDatas, $"{scenePath}/{trig.gameObject.name}");
            }
        }

        var fiPlayable = typeof(MessagePlayableAsset).GetField("messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var guid in AssetDatabase.FindAssets("t:TimelineAsset")) {
            var tlPath = AssetDatabase.GUIDToAssetPath(guid);
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tlPath);
            foreach (var track in timeline.GetOutputTracks()) {
                foreach (var clip in track.GetClips()) {
                    if (clip.asset is MessagePlayableAsset mpa) {
                        CheckArray(fiPlayable.GetValue(mpa) as MessageData[], tlPath);
                    }
                }
            }
        }

        var orphanKeys = table.Keys.Where(k => !usedKeys.Contains(k)).ToList();
        foreach (var k in orphanKeys) Debug.LogWarning($"どのメッセージからも参照されていないキー(JSON内): {k}");

        Debug.Log($"整合性チェック完了。 空キー:{emptyKeyCount} JSON不在:{missingCount} 重複:{duplicateCount} 未翻訳:{missingLangCount} 孤立キー:{orphanKeys.Count}");
    }
}
