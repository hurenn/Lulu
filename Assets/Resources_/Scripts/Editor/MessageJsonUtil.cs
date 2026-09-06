using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// メッセージ本文テーブル(Assets/Resources/Localization/Messages.json)の読み書きと
/// キー採番ルールを共有するユーティリティ。MessageTextTool/MessageDataDrawer/MessageIdOrganizerWindowから使う。
/// </summary>
public static class MessageJsonUtil {
    public const string JsonAssetPath = "Assets/Resources/Localization/Messages.json";

    // キーは「ステージID2桁+メッセージID3桁」の5桁数値文字列(例: 01001)。
    // シーン名/Timeline名の"Stage(\d+)"または先頭の"数字_"からステージ番号を判定し、無ければ00とする。
    public static string StageIdFromName(string name) {
        var m = Regex.Match(name, @"Stage(\d+)");
        if (!m.Success) m = Regex.Match(name, @"^(\d+)_");
        return m.Success ? int.Parse(m.Groups[1].Value).ToString("D2") : "00";
    }

    public static bool IsValidKey(string key) => !string.IsNullOrEmpty(key) && Regex.IsMatch(key, @"^\d{5}$");

    public static Dictionary<string, Dictionary<string, string>> LoadTable() {
        if (!File.Exists(JsonAssetPath)) return new Dictionary<string, Dictionary<string, string>>();
        var json = File.ReadAllText(JsonAssetPath);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
               ?? new Dictionary<string, Dictionary<string, string>>();
    }

    public static void SaveTable(Dictionary<string, Dictionary<string, string>> table) {
        var sorted = new SortedDictionary<string, Dictionary<string, string>>(table);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(sorted, Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(JsonAssetPath));
        File.WriteAllText(JsonAssetPath, json, new System.Text.UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    // 指定ステージの既存最大番号+1を返す(採番だけ行い、テーブルへの登録は呼び出し側で行う)
    public static int NextNumberForStage(Dictionary<string, Dictionary<string, string>> table, string stageId) {
        int max = 0;
        foreach (var k in table.Keys) {
            if (!IsValidKey(k) || !k.StartsWith(stageId)) continue;
            var num = int.Parse(k.Substring(2));
            if (num > max) max = num;
        }
        return max + 1;
    }

    public static string NewKeyForStage(Dictionary<string, Dictionary<string, string>> table, string stageId) {
        return stageId + NextNumberForStage(table, stageId).ToString("D3");
    }

    // プロジェクト内の全シーン(Assets_OLD等を除く)を列挙する
    public static IEnumerable<string> AllScenePaths() {
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Resources_/Scenes" })) {
            yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }
}
