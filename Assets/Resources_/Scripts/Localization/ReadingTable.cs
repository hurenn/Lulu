using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// メインメニューの「よみもの」で読める資料の一覧(Assets/Resources/Localization/Readings.json)を読み込む静的クラス。
/// JSONは配列で、並び順がそのまま一覧の順番になる。
/// [ { "id": "...", "title": { "ja": "...", "en": "..." }, "body": { "ja": "...", "en": "..." } }, ... ]
/// 本文の言語の扱いはMessageTextTableと同じ(見つからない言語は日本語で代用する)
/// </summary>
public static class ReadingTable {
    private const string _RESOURCE_PATH = "Localization/Readings";

    public class Entry {
        public string id;
        public Dictionary<string, string> title;
        public Dictionary<string, string> body;

        public string GetTitle(PlayerParameter.eLanguage language) => _Pick(title, language);
        public string GetBody(PlayerParameter.eLanguage language) => _Pick(body, language);
    }

    private static List<Entry> _entries;

    /// <summary>
    /// 資料の一覧(初回に読み込む)
    /// </summary>
    public static IReadOnlyList<Entry> Entries {
        get {
            if (_entries == null) Load();
            return _entries;
        }
    }

    /// <summary>
    /// 一覧を(再)読み込みする
    /// </summary>
    public static void Load() {
        var json = Resources.Load<TextAsset>(_RESOURCE_PATH);
        if (json == null) {
            Debug.LogError($"読み物の一覧が見つかりません: Resources/{_RESOURCE_PATH}.json");
            _entries = new List<Entry>();
            return;
        }
        _entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Entry>>(json.text) ?? new List<Entry>();
    }

    private static string _ToLanguageCode(PlayerParameter.eLanguage language) {
        switch (language) {
            case PlayerParameter.eLanguage.English: return "en";
            case PlayerParameter.eLanguage.Japanese:
            default: return "ja";
        }
    }

    // 指定の言語が無い・空なら日本語で代用する
    private static string _Pick(Dictionary<string, string> texts, PlayerParameter.eLanguage language) {
        if (texts == null) return string.Empty;
        if (texts.TryGetValue(_ToLanguageCode(language), out var text) && !string.IsNullOrEmpty(text)) return text;
        return texts.TryGetValue("ja", out var ja) ? ja : string.Empty;
    }
}
