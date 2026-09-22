using UnityEditor;
using UnityEngine;

/// <summary>
/// MessageChoiceOption用PropertyDrawer。labelKeyにMessageDataDrawerと同じ
/// 「IDを新規発行」ボタン+JA/EN編集UIを表示する。
/// </summary>
[CustomPropertyDrawer(typeof(MessageChoiceOption))]
public class MessageChoiceOptionDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        if (!property.isExpanded) {
            return EditorGUIUtility.singleLineHeight;
        }

        var labelKeyProp = property.FindPropertyRelative("labelKey");
        var branchMessagesProp = property.FindPropertyRelative("branchMessages");
        var nextTimelineProp = property.FindPropertyRelative("nextTimeline");
        var endsSequenceProp = property.FindPropertyRelative("endsSequence");

        float height = EditorGUIUtility.singleLineHeight; // foldoutヘッダー行
        height += 2f + MessageDataDrawer.GetKeyFieldHeight(labelKeyProp, new GUIContent("選択肢テキスト"), true);
        height += 2f + EditorGUI.GetPropertyHeight(branchMessagesProp, true);
        height += 2f + EditorGUI.GetPropertyHeight(nextTimelineProp, true);
        height += 2f + EditorGUI.GetPropertyHeight(endsSequenceProp, true);
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        var labelKeyProp = property.FindPropertyRelative("labelKey");
        var branchMessagesProp = property.FindPropertyRelative("branchMessages");
        var nextTimelineProp = property.FindPropertyRelative("nextTimeline");
        var endsSequenceProp = property.FindPropertyRelative("endsSequence");

        float y = position.y;
        var foldoutRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        y += EditorGUIUtility.singleLineHeight;

        if (property.isExpanded) {
            EditorGUI.indentLevel++;

            y += 2f;
            var keyHeaderLabel = new GUIContent("選択肢テキスト");
            float keyFieldHeight = MessageDataDrawer.GetKeyFieldHeight(labelKeyProp, keyHeaderLabel, true);
            MessageDataDrawer.DrawKeyField(new Rect(position.x, y, position.width, keyFieldHeight), labelKeyProp, keyHeaderLabel, true);
            y += keyFieldHeight;

            y += 2f;
            float branchHeight = EditorGUI.GetPropertyHeight(branchMessagesProp, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, branchHeight), branchMessagesProp, true);
            y += branchHeight;

            y += 2f;
            float timelineHeight = EditorGUI.GetPropertyHeight(nextTimelineProp, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, timelineHeight), nextTimelineProp, true);
            y += timelineHeight;

            y += 2f;
            float endsHeight = EditorGUI.GetPropertyHeight(endsSequenceProp, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, endsHeight), endsSequenceProp, true);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}
