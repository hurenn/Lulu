using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MessageData.keyから外部テキストテーブル(Assets/Resources/Localization/Messages.json)を引き、
/// Inspector上に本文プレビューを表示するPropertyDrawer。
/// </summary>
[CustomPropertyDrawer(typeof(MessageData))]
public class MessageDataDrawer : PropertyDrawer {
    private const string _JSON_PATH = "Assets/Resources/Localization/Messages.json";
    private static Dictionary<string, Dictionary<string, string>> _table;
    private static DateTime _tableLastWrite;

    private static Dictionary<string, Dictionary<string, string>> _GetTable() {
        var lastWrite = File.Exists(_JSON_PATH) ? File.GetLastWriteTimeUtc(_JSON_PATH) : DateTime.MinValue;
        if (_table == null || lastWrite != _tableLastWrite) {
            _table = File.Exists(_JSON_PATH)
                ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(_JSON_PATH))
                : new Dictionary<string, Dictionary<string, string>>();
            _tableLastWrite = lastWrite;
        }
        return _table ?? new Dictionary<string, Dictionary<string, string>>();
    }

    private static string _Preview(string key, out bool found) {
        found = false;
        if (string.IsNullOrEmpty(key)) return "(キー未設定)";
        if (!_GetTable().TryGetValue(key, out var entry)) return "(JSONに未登録)";
        found = true;
        return entry.GetValueOrDefault("ja", "");
    }

    private static GUIStyle _PreviewBoxStyle() {
        var style = new GUIStyle(EditorStyles.helpBox) { wordWrap = true, richText = false };
        return style;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        var keyProp = property.FindPropertyRelative("key");
        var previewLabel = _BuildLabel(label, keyProp.stringValue);
        float height = EditorGUI.GetPropertyHeight(property, previewLabel, true);

        if (property.isExpanded) {
            var entry = _EntryOrNull(keyProp.stringValue);
            var text = entry != null
                ? "JA: " + entry.GetValueOrDefault("ja", "") + "\nEN: " + entry.GetValueOrDefault("en", "")
                : "(JSONに未登録のキーです: " + keyProp.stringValue + ")";
            float width = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 60f);
            height += _PreviewBoxStyle().CalcHeight(new GUIContent(text), width) + 4f;
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        var keyProp = property.FindPropertyRelative("key");
        var previewLabel = _BuildLabel(label, keyProp.stringValue);
        float childHeight = EditorGUI.GetPropertyHeight(property, previewLabel, true);
        var fieldRect = new Rect(position.x, position.y, position.width, childHeight);
        EditorGUI.PropertyField(fieldRect, property, previewLabel, true);

        if (property.isExpanded) {
            var entry = _EntryOrNull(keyProp.stringValue);
            var text = entry != null
                ? "JA: " + entry.GetValueOrDefault("ja", "") + "\nEN: " + entry.GetValueOrDefault("en", "")
                : "(JSONに未登録のキーです: " + keyProp.stringValue + ")";
            var boxRect = new Rect(position.x, position.y + childHeight + 2f, position.width,
                position.height - childHeight - 2f);
            EditorGUI.LabelField(boxRect, text, _PreviewBoxStyle());
        }

        EditorGUI.EndProperty();
    }

    private static Dictionary<string, string> _EntryOrNull(string key) {
        if (string.IsNullOrEmpty(key)) return null;
        _GetTable().TryGetValue(key, out var entry);
        return entry;
    }

    private static GUIContent _BuildLabel(GUIContent label, string key) {
        var preview = _Preview(key, out _);
        if (string.IsNullOrEmpty(preview)) return label;
        preview = preview.Replace("\n", " / ");
        if (preview.Length > 24) preview = preview.Substring(0, 24) + "…";
        return new GUIContent(label.text + "   " + preview, label.tooltip);
    }
}
