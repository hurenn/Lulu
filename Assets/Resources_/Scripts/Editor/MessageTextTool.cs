using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [MenuItem("Lulu/Localization/未設定キーへ自動採番")]
    public static void AssignMissingKeys() {
        if (EditorApplication.isPlaying) { Debug.LogError("Play Mode中は実行できません。"); return; }
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("未保存のシーンがあります。保存してから実行してください。"); return; }
        }

        var table = MessageJsonUtil.LoadTable();
        int added = 0;

        var fiTrig = typeof(MessageTrigger).GetField("_messageDatas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var scenePath in MessageJsonUtil.AllScenePaths()) {
            if (!scenePath.StartsWith("Assets/")) continue;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool changed = false;
            var stageId = MessageJsonUtil.StageIdFromName(Path.GetFileNameWithoutExtension(scenePath));
            foreach (var trig in Object.FindObjectsByType<MessageTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var so = new SerializedObject(trig);
                var arr = so.FindProperty("_messageDatas")?.FindPropertyRelative("messageDatas");
                if (arr == null) continue;
                for (int idx = 0; idx < arr.arraySize; idx++) {
                    var keyProp = arr.GetArrayElementAtIndex(idx).FindPropertyRelative("key");
                    if (!string.IsNullOrEmpty(keyProp.stringValue)) continue;
                    var key = MessageJsonUtil.NewKeyForStage(table, stageId);
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
            var stageId = MessageJsonUtil.StageIdFromName(Path.GetFileNameWithoutExtension(tlPath));
            bool changed = false;
            foreach (var track in timeline.GetOutputTracks()) {
                foreach (var clip in track.GetClips()) {
                    if (!(clip.asset is MessagePlayableAsset mpa)) continue;
                    var so = new SerializedObject(mpa);
                    var arr = so.FindProperty("messageDatas");
                    for (int idx = 0; idx < arr.arraySize; idx++) {
                        var keyProp = arr.GetArrayElementAtIndex(idx).FindPropertyRelative("key");
                        if (string.IsNullOrEmpty(keyProp.stringValue)) {
                            var key = MessageJsonUtil.NewKeyForStage(table, stageId);
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

        MessageJsonUtil.SaveTable(table);
        Debug.Log($"未設定キーへの自動採番が完了しました。新規キー追加数: {added}");
    }

    [MenuItem("Lulu/Localization/整合性チェック")]
    public static void CheckIntegrity() {
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("未保存のシーンがあります。保存してから実行してください。"); return; }
        }

        var table = MessageJsonUtil.LoadTable();
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
        foreach (var scenePath in MessageJsonUtil.AllScenePaths()) {
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
