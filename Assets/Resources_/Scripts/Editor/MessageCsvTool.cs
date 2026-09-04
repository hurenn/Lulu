using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// メッセージ用String Table("Message")をCSVで書き出し/取り込みする。
/// 翻訳を外部(翻訳者)に依頼する際に、Excel/Googleスプレッドシート等で編集できる形にするためのツール。
/// </summary>
public static class MessageCsvTool {
    private const string CollectionName = "Message";
    private const string CsvRelativePath = "Translations/Message.csv"; // プロジェクトルート直下(Assetsの外)

    private static string FullCsvPath =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName, CsvRelativePath);

    [MenuItem("Lulu/Localization/Message CSVをエクスポート")]
    public static void Export() {
        var collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
        if (collection == null) {
            Debug.LogError("Stringテーブルコレクション'" + CollectionName + "'が見つかりません。");
            return;
        }

        string fullPath = FullCsvPath;
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        using (var writer = new StreamWriter(fullPath, false, new UTF8Encoding(true))) {
            Csv.Export(writer, collection, null);
        }

        Debug.Log("Message CSVを書き出しました: " + fullPath);
        EditorUtility.RevealInFinder(fullPath);
    }

    [MenuItem("Lulu/Localization/Message CSVをインポート")]
    public static void Import() {
        var collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);
        if (collection == null) {
            Debug.LogError("Stringテーブルコレクション'" + CollectionName + "'が見つかりません。");
            return;
        }

        string fullPath = FullCsvPath;
        if (!File.Exists(fullPath)) {
            Debug.LogError("CSVファイルが見つかりません: " + fullPath);
            return;
        }

        using (var reader = new StreamReader(fullPath, Encoding.UTF8)) {
            Csv.ImportInto(reader, collection, true, null, false);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Message CSVを取り込みました: " + fullPath);

        _VerifyKeyReferences(collection);
    }

    // インポート後、各MessageSequenceAssetのLocalizedStringが
    // ファイルパス命名規則から期待されるキーを正しく参照し続けているか検証する。
    // (CSVインポートの副作用でキー参照が別エントリにすり替わる事故が過去に発生したための安全策)
    private static void _VerifyKeyReferences(StringTableCollection collection) {
        var keyIdField = typeof(TableEntryReference).GetField("m_KeyId", BindingFlags.NonPublic | BindingFlags.Instance);
        var assetGuids = AssetDatabase.FindAssets("t:MessageSequenceAsset");
        int mismatchCount = 0;

        foreach (var guid in assetGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<MessageSequenceAsset>(path);
            if (asset == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            string parentFolder = Path.GetFileName(Path.GetDirectoryName(path));
            string expectedPrefix = path.Contains("/Timelines/")
                ? parentFolder + ".clip" + fileName.Replace("clip_", "")
                : parentFolder.Replace("-", "_") + "." + fileName;

            for (int i = 0; i < asset.messages.Length; i++) {
                var reference = asset.messages[i].text.TableEntryReference;
                string actualKey = reference.Key;
                if (string.IsNullOrEmpty(actualKey)) {
                    long keyId = (long)keyIdField.GetValue(reference);
                    actualKey = collection.SharedData.GetKey(keyId);
                }
                string expectedKey = expectedPrefix + "." + i;
                if (actualKey != expectedKey) {
                    mismatchCount++;
                    Debug.LogError("Message CSVインポート後のキー参照不整合: " + path + " [" + i + "] expected=" + expectedKey + " actual=" + actualKey);
                }
            }
        }

        if (mismatchCount == 0) {
            Debug.Log("Message CSVインポート後のキー参照検証: 異常なし(" + assetGuids.Length + "アセット)");
        } else {
            Debug.LogError("Message CSVインポート後のキー参照検証: " + mismatchCount + "件の不整合を検出しました。上記のエラーを確認してください。");
        }
    }
}
