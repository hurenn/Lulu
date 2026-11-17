using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Placement = TerrainDecorationProfile.ePlacement;

/// <summary>
/// 地形Tilemapを走査し、TerrainDecorationProfileのルールに従って装飾タイルをランダム配置する。
/// 結果はシーン直下の「TerrainDecoration」に装飾専用Tilemapとして焼き込む(当たり判定なし)。
/// 乱数は「シード+シーン名+ルール番号+マス座標」から決まるので、同じ設定なら何度生成しても同じ配置になる。
/// </summary>
public static class TerrainDecorator {
    public const string RootName = "TerrainDecoration";

    // 配置先ごとに装飾Tilemapを分ける(1マスに1タイルしか置けないため)
    private static readonly (string name, Placement[] placements)[] Layers = {
        ("Deco_Inner", new[] { Placement.Inner }),
        ("Deco_Wall", new[] { Placement.WallLeft, Placement.WallRight }),
        ("Deco_Ceiling", new[] { Placement.Ceiling }),
        ("Deco_Top", new[] { Placement.Top }),
    };

    /// <summary>地形マス1つ分の情報(重なっている場合は手前に描画される側)</summary>
    private struct TerrainCell {
        public TileBase tile;
        public int sortKey;
    }

    public class Result {
        public int total;
        public readonly List<string> lines = new List<string>();
        public override string ToString() => string.Join("\n", lines);
    }

    // ------------------------------------------------------------------
    // 生成
    // ------------------------------------------------------------------

    public static Result Generate(Scene scene, TerrainDecorationProfile profile) {
        var result = new Result();
        if (profile == null) {
            result.lines.Add("プロファイルが未設定です");
            return result;
        }

        // 地形Tilemapと除外Tilemap(扉など)を集める
        var terrainMaps = new List<Tilemap>();
        var excludedMaps = new List<Tilemap>();
        foreach (var root in scene.GetRootGameObjects()) {
            if (root.name == RootName) continue;
            foreach (var tm in root.GetComponentsInChildren<Tilemap>(true)) {
                if (profile.excludedTilemapNames.Contains(tm.name)) {
                    excludedMaps.Add(tm);
                } else if (tm.gameObject.activeInHierarchy && ContainsTerrainTile(tm, profile)) {
                    terrainMaps.Add(tm);
                }
            }
        }
        if (terrainMaps.Count == 0) {
            result.lines.Add($"{scene.name}: 地形Tilemapが見つかりません");
            return result;
        }

        // 装飾の出力先を用意(グリッドは地形の1枚目に合わせる)
        Grid refGrid = terrainMaps[0].layoutGrid;
        GameObject decoRoot = GetOrCreateRoot(scene, refGrid);
        Grid decoGrid = decoRoot.GetComponent<Grid>();

        // 地形マスを装飾グリッドの座標に集約。重なりは手前に描画される方を採用
        var solid = new Dictionary<Vector3Int, TerrainCell>();
        int maxSortKey = int.MinValue;
        TilemapRenderer topRenderer = null;
        foreach (var tm in terrainMaps) {
            var r = tm.GetComponent<TilemapRenderer>();
            int sortKey = r ? SortingLayer.GetLayerValueFromID(r.sortingLayerID) * 100000 + r.sortingOrder : 0;
            if (sortKey > maxSortKey) {
                maxSortKey = sortKey;
                topRenderer = r;
            }
            ForEachTile(tm, (pos, tile) => {
                if (!IsTerrainTile(tile, profile)) return;
                var cell = decoGrid.WorldToCell(tm.GetCellCenterWorld(pos));
                if (!solid.TryGetValue(cell, out var cur) || sortKey >= cur.sortKey) {
                    solid[cell] = new TerrainCell { tile = tile, sortKey = sortKey };
                }
            });
        }

        // 置いてはいけないマス(扉の周囲、除外範囲)
        var blocked = new HashSet<Vector3Int>();
        var occupiedByExcluded = new HashSet<Vector3Int>();
        int m = profile.excludedTilemapMargin;
        foreach (var tm in excludedMaps) {
            ForEachTile(tm, (pos, tile) => {
                var cell = decoGrid.WorldToCell(tm.GetCellCenterWorld(pos));
                occupiedByExcluded.Add(cell);
                for (int dx = -m; dx <= m; dx++)
                    for (int dy = -m; dy <= m; dy++)
                        blocked.Add(cell + new Vector3Int(dx, dy, 0));
            });
        }
        foreach (var root in scene.GetRootGameObjects()) {
            foreach (var ex in root.GetComponentsInChildren<TerrainDecorationExclusion>(true)) {
                Rect rect = ex.WorldRect;
                var min = decoGrid.WorldToCell(rect.min);
                var max = decoGrid.WorldToCell(rect.max);
                for (int x = min.x; x <= max.x; x++)
                    for (int y = min.y; y <= max.y; y++)
                        blocked.Add(new Vector3Int(x, y, 0));
            }
        }

        bool IsSolid(Vector3Int c) => solid.ContainsKey(c) || occupiedByExcluded.Contains(c);
        bool IsEmpty(Vector3Int c) => !IsSolid(c);

        int sceneHash = StableHash(scene.name);
        var placed = Layers.ToDictionary(l => l.name, l => new Dictionary<Vector3Int, TileBase>());

        for (int ruleIndex = 0; ruleIndex < profile.rules.Count; ruleIndex++) {
            var rule = profile.rules[ruleIndex];
            if (!rule.enabled) continue;
            var variants = rule.variants.Where(v => v.tile != null && v.weight > 0f).ToList();
            if (variants.Count == 0) continue;

            string layerName = Layers.First(l => l.placements.Contains(rule.placement)).name;
            var layerPlaced = placed[layerName];

            var candidates = CollectCandidates(rule, solid, IsSolid, IsEmpty);
            // 走査順を固定して結果を安定させる
            candidates.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            int count = 0;
            foreach (var cell in candidates) {
                if (blocked.Contains(cell) || layerPlaced.ContainsKey(cell)) continue;
                if (Hash01(profile.seed, sceneHash, ruleIndex, cell, 0) >= rule.density) continue;
                if (rule.minSpacing > 0 && HasNeighbor(layerPlaced, cell, rule.minSpacing)) continue;

                layerPlaced[cell] = PickVariant(variants, Hash01(profile.seed, sceneHash, ruleIndex, cell, 1));
                count++;
            }
            result.lines.Add($"{scene.name}: {rule.name} ({rule.placement}) 候補{candidates.Count}マス → {count}個");
            result.total += count;
        }

        // 書き込み
        Undo.RegisterFullObjectHierarchyUndo(decoRoot, "Generate Terrain Decoration");
        int sortingOrder = topRenderer ? topRenderer.sortingOrder + profile.sortingOrderOffset : profile.sortingOrderOffset;
        int sortingLayerId = topRenderer ? topRenderer.sortingLayerID : 0;
        foreach (var layer in Layers) {
            var tm = GetOrCreateLayer(decoRoot, layer.name, sortingLayerId, sortingOrder);
            tm.ClearAllTiles();
            var cells = placed[layer.name];
            if (cells.Count > 0) {
                tm.SetTiles(cells.Keys.ToArray(), cells.Values.ToArray());
            }
            tm.CompressBounds();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        return result;
    }

    public static bool Clear(Scene scene) {
        foreach (var root in scene.GetRootGameObjects()) {
            if (root.name != RootName) continue;
            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // 候補マスの抽出
    // ------------------------------------------------------------------

    private static List<Vector3Int> CollectCandidates(
        TerrainDecorationProfile.Rule rule,
        Dictionary<Vector3Int, TerrainCell> solid,
        System.Func<Vector3Int, bool> isSolid,
        System.Func<Vector3Int, bool> isEmpty) {

        var list = new List<Vector3Int>();
        bool Matches(Vector3Int c) =>
            solid.TryGetValue(c, out var t) &&
            (rule.targetTerrainTiles.Count == 0 || rule.targetTerrainTiles.Contains(t.tile));

        if (rule.placement == Placement.Inner) {
            int d = rule.innerDepth - 1;
            foreach (var c in solid.Keys) {
                if (!Matches(c)) continue;
                bool deep = true;
                for (int dx = -d; dx <= d && deep; dx++)
                    for (int dy = -d; dy <= d && deep; dy++)
                        if (!isSolid(c + new Vector3Int(dx, dy, 0))) deep = false;
                if (deep) list.Add(c);
            }
            return list;
        }

        // toWall: 装飾マスから地形へ向かう方向 / along: 面に沿った方向
        Vector3Int toWall, along;
        switch (rule.placement) {
            case Placement.Top: toWall = Vector3Int.down; along = Vector3Int.right; break;
            case Placement.Ceiling: toWall = Vector3Int.up; along = Vector3Int.right; break;
            case Placement.WallLeft: toWall = Vector3Int.right; along = Vector3Int.up; break;
            default: toWall = Vector3Int.left; along = Vector3Int.up; break;
        }

        bool IsFace(Vector3Int c) => isEmpty(c) && isSolid(c + toWall);

        foreach (var wallCell in solid.Keys) {
            var c = wallCell - toWall;
            if (!IsFace(c)) continue;
            // 面の先頭マスからだけ連続長を数える
            if (IsFace(c - along)) continue;

            var run = new List<Vector3Int>();
            for (var p = c; IsFace(p); p += along) run.Add(p);
            if (run.Count < rule.minSurfaceLength) continue;

            for (int i = rule.edgeMargin; i < run.Count - rule.edgeMargin; i++) {
                var p = run[i];
                if (!Matches(p + toWall)) continue;
                bool clear = true;
                for (int k = 1; k < rule.clearance && clear; k++)
                    if (!isEmpty(p - toWall * k)) clear = false;
                if (clear) list.Add(p);
            }
        }
        return list;
    }

    // ------------------------------------------------------------------
    // ヘルパー
    // ------------------------------------------------------------------

    private static bool ContainsTerrainTile(Tilemap tm, TerrainDecorationProfile profile) {
        bool found = false;
        ForEachTile(tm, (pos, tile) => { if (!found && IsTerrainTile(tile, profile)) found = true; });
        return found;
    }

    private static bool IsTerrainTile(TileBase tile, TerrainDecorationProfile profile) =>
        tile != null && (profile.terrainTiles.Count == 0 || profile.terrainTiles.Contains(tile));

    private static void ForEachTile(Tilemap tm, System.Action<Vector3Int, TileBase> action) {
        var bounds = tm.cellBounds;
        var tiles = tm.GetTilesBlock(bounds);
        int i = 0;
        for (int z = bounds.zMin; z < bounds.zMax; z++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                for (int x = bounds.xMin; x < bounds.xMax; x++, i++)
                    if (tiles[i] != null) action(new Vector3Int(x, y, z), tiles[i]);
    }

    private static bool HasNeighbor(Dictionary<Vector3Int, TileBase> placed, Vector3Int c, int r) {
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                if (placed.ContainsKey(c + new Vector3Int(dx, dy, 0))) return true;
        return false;
    }

    private static TileBase PickVariant(List<TerrainDecorationProfile.Variant> variants, float t) {
        float total = variants.Sum(v => v.weight);
        float acc = 0f;
        foreach (var v in variants) {
            acc += v.weight / total;
            if (t < acc) return v.tile;
        }
        return variants[variants.Count - 1].tile;
    }

    /// <summary>座標から決まる0〜1の疑似乱数(UnityEngine.Randomの状態に依存しない)</summary>
    private static float Hash01(int seed, int sceneHash, int ruleIndex, Vector3Int c, int salt) {
        unchecked {
            uint h = 2166136261u;
            void Mix(int v) { h = (h ^ (uint)v) * 16777619u; h ^= h >> 15; h *= 0x2c1b3c6du; h ^= h >> 12; }
            Mix(seed); Mix(sceneHash); Mix(ruleIndex); Mix(c.x); Mix(c.y); Mix(salt);
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    private static int StableHash(string s) {
        unchecked {
            int h = 23;
            foreach (char ch in s) h = h * 31 + ch;
            return h;
        }
    }

    private static GameObject GetOrCreateRoot(Scene scene, Grid refGrid) {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null) {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Terrain Decoration");
        }
        var grid = root.GetComponent<Grid>();
        if (grid == null) grid = root.AddComponent<Grid>();
        grid.cellSize = refGrid.cellSize;
        grid.cellGap = refGrid.cellGap;
        grid.cellLayout = refGrid.cellLayout;
        grid.cellSwizzle = refGrid.cellSwizzle;
        root.transform.SetPositionAndRotation(refGrid.transform.position, refGrid.transform.rotation);
        root.transform.localScale = refGrid.transform.lossyScale;
        return root;
    }

    private static Tilemap GetOrCreateLayer(GameObject root, string name, int sortingLayerId, int sortingOrder) {
        var t = root.transform.Find(name);
        GameObject go;
        if (t == null) {
            go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(root.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Terrain Decoration Layer");
        } else {
            go = t.gameObject;
        }
        var r = go.GetComponent<TilemapRenderer>();
        r.sortingLayerID = sortingLayerId;
        r.sortingOrder = sortingOrder;
        r.mode = TilemapRenderer.Mode.Chunk;
        return go.GetComponent<Tilemap>();
    }
}

/// <summary>地形装飾の生成ウィンドウ(Tools/Lulu/地形の自動装飾)</summary>
public class TerrainDecoratorWindow : EditorWindow {
    private const string ProfilePrefKey = "Lulu.TerrainDecorator.Profile";

    private static readonly string[] DefaultScenes = {
        "Assets/Resources_/Scenes/Stages/Stage1-1.unity",
        "Assets/Resources_/Scenes/Stages/Stage1-2.unity",
        "Assets/Resources_/Scenes/Stages/Stage1-3.unity",
        "Assets/Resources_/Scenes/Stages/Stage1-4.unity",
    };

    private TerrainDecorationProfile _profile;
    private string _log = "";
    private Vector2 _scroll;

    [MenuItem("Tools/Lulu/地形の自動装飾")]
    public static void Open() => GetWindow<TerrainDecoratorWindow>("地形の自動装飾");

    private void OnEnable() {
        string path = EditorPrefs.GetString(ProfilePrefKey, TerrainDecorationPlaceholder.ProfilePath);
        _profile = AssetDatabase.LoadAssetAtPath<TerrainDecorationProfile>(path);
    }

    private void OnGUI() {
        EditorGUI.BeginChangeCheck();
        _profile = (TerrainDecorationProfile)EditorGUILayout.ObjectField("プロファイル", _profile, typeof(TerrainDecorationProfile), false);
        if (EditorGUI.EndChangeCheck() && _profile != null) {
            EditorPrefs.SetString(ProfilePrefKey, AssetDatabase.GetAssetPath(_profile));
        }

        if (_profile == null) {
            EditorGUILayout.HelpBox("プロファイルがありません。仮の装飾タイル一式とプロファイルを作成できます。", MessageType.Info);
            if (GUILayout.Button("仮の装飾タイルとプロファイルを作成")) {
                _profile = TerrainDecorationPlaceholder.Create();
                EditorPrefs.SetString(ProfilePrefKey, AssetDatabase.GetAssetPath(_profile));
            }
            return;
        }

        if (GUILayout.Button("シード値をランダムに変更")) {
            Undo.RecordObject(_profile, "Change Seed");
            _profile.seed = Random.Range(0, int.MaxValue);
            EditorUtility.SetDirty(_profile);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("開いているシーン", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("生成")) {
                var r = TerrainDecorator.Generate(SceneManager.GetActiveScene(), _profile);
                _log = r + $"\n合計 {r.total}個";
            }
            if (GUILayout.Button("削除")) {
                _log = TerrainDecorator.Clear(SceneManager.GetActiveScene()) ? "削除しました" : "装飾がありません";
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage1-1〜1-4", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("一括生成して保存")) RunBatch(true);
            if (GUILayout.Button("一括削除して保存")) RunBatch(false);
        }

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.HelpBox(string.IsNullOrEmpty(_log) ? "結果がここに表示されます" : _log, MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private void RunBatch(bool generate) {
        string profilePath = AssetDatabase.GetAssetPath(_profile);
        _log = RunBatch(profilePath, DefaultScenes, generate);
        // シーンを開き直すと未参照アセットが解放されるので読み直す
        _profile = AssetDatabase.LoadAssetAtPath<TerrainDecorationProfile>(profilePath);
    }

    /// <summary>シーンを順に開いて生成/削除し、保存する。最後に元のシーン構成へ戻す</summary>
    public static string RunBatch(string profilePath, IEnumerable<string> scenePaths, bool generate) {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "中止しました";
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var sb = new StringBuilder();
        try {
            foreach (var path in scenePaths) {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                // OpenScene(Single)で参照の無いアセットはアンロードされるため、毎回パスから読み直す
                var profile = AssetDatabase.LoadAssetAtPath<TerrainDecorationProfile>(profilePath);
                bool changed;
                if (generate) {
                    var r = TerrainDecorator.Generate(scene, profile);
                    sb.AppendLine(r.ToString());
                    sb.AppendLine($"{scene.name}: 合計 {r.total}個");
                    changed = profile != null;
                } else {
                    changed = TerrainDecorator.Clear(scene);
                    sb.AppendLine($"{scene.name}: " + (changed ? "削除" : "装飾なし"));
                }
                if (changed) EditorSceneManager.SaveScene(scene);
            }
        } finally {
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        return sb.ToString();
    }
}
