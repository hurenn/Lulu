using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MessageData.keyから外部テキストテーブル(Assets/Resources/Localization/Messages.json)を引き、
/// Inspector上に本文プレビュー・新規ID発行・本文の直接編集を提供するPropertyDrawer。
/// </summary>
[CustomPropertyDrawer(typeof(MessageData))]
public class MessageDataDrawer : PropertyDrawer {
    private static Dictionary<string, Dictionary<string, string>> _table;
    private static DateTime _tableLastWrite;

    private static Dictionary<string, Dictionary<string, string>> _GetTable() {
        var path = MessageJsonUtil.JsonAssetPath;
        var lastWrite = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        if (_table == null || lastWrite != _tableLastWrite) {
            _table = MessageJsonUtil.LoadTable();
            _tableLastWrite = lastWrite;
        }
        return _table;
    }

    private static Dictionary<string, string> _EntryOrNull(string key) {
        if (string.IsNullOrEmpty(key)) return null;
        _GetTable().TryGetValue(key, out var entry);
        return entry;
    }

    private static GUIStyle _PreviewBoxStyle() => new GUIStyle(EditorStyles.helpBox) { wordWrap = true, richText = false };

    private static GUIContent _BuildLabel(GUIContent label, string key) {
        var entry = _EntryOrNull(key);
        string preview = entry != null ? entry.GetValueOrDefault("ja", "") : (string.IsNullOrEmpty(key) ? "" : "(JSONに未登録)");
        if (string.IsNullOrEmpty(preview)) return label;
        preview = preview.Replace("\n", " / ");
        if (preview.Length > 24) preview = preview.Substring(0, 24) + "…";
        return new GUIContent(label.text + "   " + preview, label.tooltip);
    }

    // --- 所属ステージの判定・新規ID発行・本文保存(reflection経由のテストからも呼べるようpublic static) ---

    public static string ContextStageId(UnityEngine.Object target) {
        string name;
        if (target is Component comp) {
            name = comp.gameObject.scene.name;
        } else if (target != null) {
            var path = AssetDatabase.GetAssetPath(target);
            name = string.IsNullOrEmpty(path) ? target.name : Path.GetFileNameWithoutExtension(path);
        } else {
            name = "";
        }
        return MessageJsonUtil.StageIdFromName(name);
    }

    public static string AssignNewKey(SerializedProperty property) {
        var keyProp = property.FindPropertyRelative("key");
        if (!string.IsNullOrEmpty(keyProp.stringValue)) return keyProp.stringValue;
        var stageId = ContextStageId(property.serializedObject.targetObject);
        var table = MessageJsonUtil.LoadTable();
        var newKey = MessageJsonUtil.NewKeyForStage(table, stageId);
        table[newKey] = new Dictionary<string, string> { { "ja", "" }, { "en", "" } };
        MessageJsonUtil.SaveTable(table);
        keyProp.stringValue = newKey;
        property.serializedObject.ApplyModifiedProperties();
        return newKey;
    }

    public static void SaveText(string key, string ja, string en) {
        if (string.IsNullOrEmpty(key)) return;
        var table = MessageJsonUtil.LoadTable();
        table[key] = new Dictionary<string, string> { { "ja", ja ?? "" }, { "en", en ?? "" } };
        MessageJsonUtil.SaveTable(table);
    }

    // --- 高さ計算 ---

    private static float _EditableAreaHeight(Dictionary<string, string> entry) {
        float w = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 60f);
        var style = EditorStyles.textArea;
        float jaH = Mathf.Max(EditorGUIUtility.singleLineHeight * 2, style.CalcHeight(new GUIContent(entry.GetValueOrDefault("ja", "")), w));
        float enH = Mathf.Max(EditorGUIUtility.singleLineHeight * 2, style.CalcHeight(new GUIContent(entry.GetValueOrDefault("en", "")), w));
        return EditorGUIUtility.singleLineHeight + jaH + EditorGUIUtility.singleLineHeight + enH + 6f;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        var keyProp = property.FindPropertyRelative("key");
        var key = keyProp.stringValue;
        var previewLabel = _BuildLabel(label, key);
        float height = EditorGUI.GetPropertyHeight(property, previewLabel, true);

        if (property.isExpanded) {
            if (string.IsNullOrEmpty(key)) {
                height += EditorGUIUtility.singleLineHeight + 4f;
            } else {
                var entry = _EntryOrNull(key);
                height += entry == null ? EditorGUIUtility.singleLineHeight + 4f : _EditableAreaHeight(entry) + 4f;
            }
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        var keyProp = property.FindPropertyRelative("key");
        var key = keyProp.stringValue;
        var previewLabel = _BuildLabel(label, key);
        float childHeight = EditorGUI.GetPropertyHeight(property, previewLabel, true);
        var fieldRect = new Rect(position.x, position.y, position.width, childHeight);
        EditorGUI.PropertyField(fieldRect, property, previewLabel, true);

        if (property.isExpanded) {
            var areaRect = new Rect(position.x, position.y + childHeight + 2f, position.width,
                position.height - childHeight - 2f);
            if (string.IsNullOrEmpty(key)) {
                var buttonRect = new Rect(areaRect.x, areaRect.y, areaRect.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(buttonRect, "IDを新規発行")) {
                    AssignNewKey(property);
                }
            } else {
                var entry = _EntryOrNull(key);
                if (entry == null) {
                    EditorGUI.LabelField(areaRect, "(JSONに未登録のキーです: " + key + ")", _PreviewBoxStyle());
                } else {
                    _DrawEditableFields(areaRect, key, entry);
                }
            }
        }

        EditorGUI.EndProperty();
    }

    private static void _DrawEditableFields(Rect rect, string key, Dictionary<string, string> entry) {
        var ja = entry.GetValueOrDefault("ja", "");
        var en = entry.GetValueOrDefault("en", "");
        var style = EditorStyles.textArea;
        float jaH = Mathf.Max(EditorGUIUtility.singleLineHeight * 2, style.CalcHeight(new GUIContent(ja), rect.width));
        float enH = Mathf.Max(EditorGUIUtility.singleLineHeight * 2, style.CalcHeight(new GUIContent(en), rect.width));

        float y = rect.y;
        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "JA");
        y += EditorGUIUtility.singleLineHeight;
        EditorGUI.BeginChangeCheck();
        var newJa = EditorGUI.TextArea(new Rect(rect.x, y, rect.width, jaH), ja, style);
        bool jaChanged = EditorGUI.EndChangeCheck();
        y += jaH;

        EditorGUI.LabelField(new Rect(rect.x, y, rect.width, EditorGUIUtility.singleLineHeight), "EN");
        y += EditorGUIUtility.singleLineHeight;
        EditorGUI.BeginChangeCheck();
        var newEn = EditorGUI.TextArea(new Rect(rect.x, y, rect.width, enH), en, style);
        bool enChanged = EditorGUI.EndChangeCheck();

        if (jaChanged || enChanged) {
            SaveText(key, newJa, newEn);
        }
    }
}
