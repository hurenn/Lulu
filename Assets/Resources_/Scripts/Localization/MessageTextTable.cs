using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// メッセージ本文の外部テキストテーブル(Assets/Resources/Localization/Messages.json)を読み込み、
/// キー+言語からテキストを引くための静的クラス。
/// </summary>
public static class MessageTextTable {
    private const string _RESOURCE_PATH = "Localization/Messages";

    private static Dictionary<string, Dictionary<string, string>> _table;

    private static Dictionary<string, Dictionary<string, string>> _Table {
        get {
            if (_table == null) {
                Load();
            }
            return _table;
        }
    }

    /// <summary>
    /// テキストテーブルを(再)読み込みする
    /// </summary>
    public static void Load() {
        var json = Resources.Load<TextAsset>(_RESOURCE_PATH);
        if (json == null) {
            Debug.LogError($"メッセージテキストテーブルが見つかりません: Resources/{_RESOURCE_PATH}.json");
            _table = new Dictionary<string, Dictionary<string, string>>();
            return;
        }
        _table = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json.text)
                 ?? new Dictionary<string, Dictionary<string, string>>();
    }

    // PlayerParameter.eLanguageと言語コードの対応。将来言語を増やす場合はここに追加する。
    private static string _ToLanguageCode(PlayerParameter.eLanguage language) {
        switch (language) {
            case PlayerParameter.eLanguage.English: return "en";
            case PlayerParameter.eLanguage.Japanese:
            default: return "ja";
        }
    }

    /// <summary>
    /// キーと言語からメッセージ本文を取得する。見つからない場合はキー自体を返す(無音の空表示を避けるため)。
    /// </summary>
    public static string GetText(string key, PlayerParameter.eLanguage language) {
        if (string.IsNullOrEmpty(key)) {
            Debug.LogWarning("MessageTextTable.GetText: キーが空です");
            return string.Empty;
        }
        if (!_Table.TryGetValue(key, out var entry)) {
            Debug.LogWarning($"MessageTextTable: キーが見つかりません: {key}");
            return key;
        }
        var code = _ToLanguageCode(language);
        if (!entry.TryGetValue(code, out var text)) {
            Debug.LogWarning($"MessageTextTable: キー'{key}'に言語'{code}'のテキストがありません");
            return key;
        }
        return text;
    }
}
