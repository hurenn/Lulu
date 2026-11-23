using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Placement = TerrainDecorationProfile.ePlacement;

/// <summary>
/// 地形装飾の仮素材(ドット絵タイル)と初期プロファイルを生成する。ステージ1(廃ビル内部)向け。
/// 正式な絵ができたら、同じ名前のpngを上書きするか、プロファイルのタイルを差し替える。
/// 2マス幅の絵は左端のマスが基準(ピボットは基準マスの中心)。
/// </summary>
public static class TerrainDecorationPlaceholder {
    public const string Folder = "Assets/Resources_/TilePalettes/TerrainDecoration";
    public const string ProfilePath = Folder + "/TerrainDecorationProfile.asset";
    public const string SpriteFolder = Folder + "/Placeholder";
    public const string TileFolder = Folder + "/Tiles";
    private const int Cell = 16;

    private static readonly string[] TerrainTilePaths = {
        "Assets/Resources_/TilePalettes/AutoTile_Building.asset",
        "Assets/Resources_/TilePalettes/AutoTile_BuildingGreen.asset",
    };

    // 地形(灰色のコンクリート)より少し明るくして見分けがつくようにする
    private static readonly Color Clear = new Color(0, 0, 0, 0);
    private static readonly Color Outline = new Color32(40, 40, 46, 255);
    private static readonly Color Concrete = new Color32(128, 128, 134, 255);
    private static readonly Color ConcreteLight = new Color32(168, 168, 174, 255);
    private static readonly Color ConcreteDark = new Color32(92, 92, 100, 255);
    private static readonly Color Rust = new Color32(140, 78, 46, 255);
    private static readonly Color RustDark = new Color32(96, 52, 32, 255);
    private static readonly Color Glass = new Color32(170, 220, 236, 255);
    private static readonly Color Cable = new Color32(30, 30, 34, 255);
    private static readonly Color Stain = new Color32(50, 54, 50, 110);
    // 緑化(BuildingGreen用)
    private static readonly Color Leaf = new Color32(84, 146, 64, 255);
    private static readonly Color LeafLight = new Color32(130, 190, 90, 255);
    private static readonly Color LeafDark = new Color32(46, 92, 44, 255);
    private static readonly Color Stem = new Color32(70, 100, 50, 255);
    private static readonly Color FlowerYellow = new Color32(236, 212, 104, 255);
    private static readonly Color FlowerPink = new Color32(222, 120, 150, 255);
    private static readonly Color Moss = new Color32(80, 128, 64, 150);

    /// <summary>絵の基準の置き方</summary>
    private enum eAnchor { Bottom, Top, Center }

    [MenuItem("Tools/Lulu/地形装飾の仮素材を作成")]
    public static TerrainDecorationProfile Create() {
        Directory.CreateDirectory(SpriteFolder);
        Directory.CreateDirectory(TileFolder);

        // 床
        var rubbleA = MakeTile("Rubble_A", 16, 16, eAnchor.Bottom, DrawRubbleA);
        var rebar = MakeTile("Debris_Rebar", 16, 16, eAnchor.Bottom, DrawRebar);
        var pebblesA = MakeTile("Pebbles_A", 16, 16, eAnchor.Bottom, p => DrawPebbles(p, 1));
        var pebblesB = MakeTile("Pebbles_B", 16, 16, eAnchor.Bottom, p => DrawPebbles(p, 2));
        var glass = MakeTile("Glass_Shards", 16, 16, eAnchor.Bottom, DrawGlass);
        // 天井
        var cable = MakeTile("Ceiling_Cable", 16, 16, eAnchor.Top, DrawCable);
        // 地形の内部
        var crackA = MakeTile("Crack_A", 16, 16, eAnchor.Center, DrawCrackA);
        var crackB = MakeTile("Crack_B", 16, 16, eAnchor.Center, DrawCrackB);
        var stain = MakeTile("Stain", 16, 16, eAnchor.Center, DrawStain);
        var stainTriangle = MakeTile("StainTriangle", 16, 16, eAnchor.Center, DrawStain); // 正式な絵(StainTriangle.png)があればそれを使う

        // 緑化(BuildingGreen用)
        var fern = MakeTile("Green_Fern", 16, 24, eAnchor.Bottom, DrawFern);
        var grassA = MakeTile("Green_Grass_A", 16, 16, eAnchor.Bottom, p => DrawGrass(p, 1));
        var grassB = MakeTile("Green_Grass_B", 16, 16, eAnchor.Bottom, p => DrawGrass(p, 2));
        var flower = MakeTile("Green_Flower", 16, 16, eAnchor.Bottom, DrawFlower);
        var mossA = MakeTile("Green_Moss_A", 16, 16, eAnchor.Center, p => DrawMossPatch(p, 1));
        var mossB = MakeTile("Green_Moss_B", 16, 16, eAnchor.Center, p => DrawMossPatch(p, 2));
        var mossC = MakeTile("Green_Moss_C", 16, 16, eAnchor.Center, p => DrawMossPatch(p, 1)); // 正式な絵(Green_Moss_C.png)があればそれを使う

        // 奥の壁(近景)の仮素材は作らない(機能はプロファイルの「奥の壁」でタイルを指定すれば使える)

        var profile = AssetDatabase.LoadAssetAtPath<TerrainDecorationProfile>(ProfilePath);
        if (profile != null) {
            AssetDatabase.SaveAssets();
            return profile; // 既存のプロファイルの設定は上書きしない
        }

        profile = ScriptableObject.CreateInstance<TerrainDecorationProfile>();
        foreach (var path in TerrainTilePaths) {
            var t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (t != null) profile.terrainTiles.Add(t);
        }
        var building = AssetDatabase.LoadAssetAtPath<TileBase>(TerrainTilePaths[0]);
        var green = AssetDatabase.LoadAssetAtPath<TileBase>(TerrainTilePaths[1]);

        // 同じ配置先では上のルールが優先される(大きい物→小さい物の順に置き、小さい物で隙間を埋める)
        // 2マス幅の装飾(footprint=2)や隅だけの配置(cornerOnly)はルール側で設定できる
        // ---- Building: 廃ビルの瓦礫 ----
        var buildingRules = new List<TerrainDecorationProfile.Rule> {
            Rule("瓦礫", Placement.Top, 0.10f, 3, (rubbleA, 2f), (rebar, 1f)),
            Rule("小石・ガラス片", Placement.Top, 0.25f, 1, (pebblesA, 3f), (pebblesB, 3f), (glass, 1f)),
            Rule("天井の垂れ下がり", Placement.Ceiling, 0.08f, 3, (cable, 1f)),
            Rule("壁のひび・染み", Placement.Inner, 0.03f, 3, (crackA, 2f), (crackB, 2f), (stain, 1f), (stainTriangle, 1f)),
        };
        foreach (var r in buildingRules) r.targetTerrainTiles.Add(building);

        // ---- BuildingGreen: 植物・蔦 ----
        var greenFern = Rule("シダ", Placement.Top, 0.08f, 3, (fern, 1f));
        greenFern.clearance = 2;
        var greenRules = new List<TerrainDecorationProfile.Rule> {
            greenFern,
            Rule("草花", Placement.Top, 0.45f, 0, (grassA, 3f), (grassB, 3f), (flower, 1f)),
            Rule("苔の張り付き", Placement.Inner, 0.06f, 2, (mossA, 1f), (mossB, 1f), (mossC, 1f)),
        };
        foreach (var r in greenRules) r.targetTerrainTiles.Add(green);

        profile.rules = new List<TerrainDecorationProfile.Rule>();
        profile.rules.AddRange(buildingRules);
        profile.rules.AddRange(greenRules);
        profile.backWallEnabled = false;
        AssetDatabase.CreateAsset(profile, ProfilePath);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static TerrainDecorationProfile.Rule Rule(string name, Placement placement, float density, int spacing,
        params (TileBase tile, float weight)[] variants) {
        var rule = new TerrainDecorationProfile.Rule {
            name = name,
            placement = placement,
            density = density,
            minSpacing = spacing,
        };
        foreach (var v in variants) {
            rule.variants.Add(new TerrainDecorationProfile.Variant { tile = v.tile, weight = v.weight });
        }
        return rule;
    }

    // ------------------------------------------------------------------
    // 素材の書き出し
    // ------------------------------------------------------------------

    private class Canvas {
        public readonly int w, h;
        public readonly Color[] px;
        public Canvas(int w, int h) {
            this.w = w;
            this.h = h;
            px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;
        }
        public void Set(int x, int y, Color c) {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            px[y * w + x] = c;
        }
        public Color Get(int x, int y) => (x < 0 || x >= w || y < 0 || y >= h) ? Clear : px[y * w + x];
        public void Fill(int x0, int y0, int x1, int y1, Color c) {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    Set(x, y, c);
        }
        public void Line(int x0, int y0, int x1, int y1, Color c) {
            int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= n; i++) {
                float t = n == 0 ? 0 : i / (float)n;
                Set(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), c);
            }
        }
        /// <summary>多角形(凸)を塗りつぶし、上側を明るく・縁を暗くする</summary>
        public void Chunk(Vector2Int[] poly, Color body) {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (Inside(poly, x + 0.5f, y + 0.5f)) Set(x, y, body);
            // 縁取り
            var copy = (Color[])px.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    if (copy[y * w + x] != body) continue;
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 ||
                                copy[y * w + x - 1].a == 0 || copy[y * w + x + 1].a == 0 ||
                                copy[(y - 1) * w + x].a == 0 || copy[(y + 1) * w + x].a == 0;
                    if (edge) Set(x, y, Outline);
                    else if (copy[(y + 1) * w + x] == Outline || copy[(y + 1) * w + x].a == 0 ||
                             (y + 2 < h && copy[(y + 2) * w + x].a == 0)) Set(x, y, ConcreteLight);
                }
        }
        private static bool Inside(Vector2Int[] poly, float x, float y) {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (float)(poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }
    }

    private static Tile MakeTile(string name, int w, int h, eAnchor anchor, System.Action<Canvas> draw) {
        string pngPath = $"{SpriteFolder}/{name}.png";
        string tilePath = $"{TileFolder}/{name}.asset";

        // 既にpngがあれば描き直さない(正式な絵に差し替え済みの場合を壊さないため)
        if (!File.Exists(pngPath)) {
            var canvas = new Canvas(w, h);
            draw(canvas);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(canvas.px);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath);

            // ピボット=基準マス(左端のマス)の中心。Tilemapはピボットをマスの中心に合わせて描く
            float pivotY = anchor == eAnchor.Bottom ? (Cell * 0.5f) / h
                         : anchor == eAnchor.Top ? (h - Cell * 0.5f) / h
                         : 0.5f;
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Cell;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2((Cell * 0.5f) / w, pivotY);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null) {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, tilePath);
        }
        return tile;
    }

    private static Vector2Int[] P(params int[] xy) {
        var r = new Vector2Int[xy.Length / 2];
        for (int i = 0; i < r.Length; i++) r[i] = new Vector2Int(xy[i * 2], xy[i * 2 + 1]);
        return r;
    }

    // ------------------------------------------------------------------
    // 仮の絵(左下原点)
    // ------------------------------------------------------------------

    private static void DrawRubbleA(Canvas c) {
        c.Chunk(P(2, 0, 11, 0, 10, 6, 5, 8, 2, 4), Concrete);
        c.Chunk(P(10, 0, 15, 0, 14, 4, 11, 4), ConcreteDark);
    }

    private static void DrawRebar(Canvas c) {
        // 曲がった鉄筋と小さな塊
        c.Line(1, 1, 7, 2, Rust); c.Line(7, 2, 10, 7, Rust); c.Line(10, 7, 14, 8, Rust);
        c.Line(1, 0, 7, 1, RustDark); c.Line(8, 1, 11, 6, RustDark);
        c.Chunk(P(3, 0, 8, 0, 7, 3, 4, 3), ConcreteDark);
    }

    private static void DrawPebbles(Canvas c, int variant) {
        int[] xs = variant == 1 ? new[] { 1, 5, 6, 11, 14 } : new[] { 2, 3, 8, 12, 13 };
        for (int i = 0; i < xs.Length; i++) {
            Color col = i % 2 == 0 ? Concrete : ConcreteDark;
            c.Set(xs[i], 0, col);
            if (i % 2 == 0) { c.Set(xs[i] + 1, 0, col); c.Set(xs[i], 1, ConcreteLight); }
        }
    }

    private static void DrawGlass(Canvas c) {
        c.Line(2, 0, 4, 2, Glass); c.Set(3, 0, Glass);
        c.Line(8, 0, 9, 3, Glass); c.Set(9, 0, Color.white);
        c.Set(12, 0, Glass); c.Set(13, 1, Glass); c.Set(13, 0, Glass);
        c.Set(6, 0, ConcreteDark); c.Set(15, 0, Concrete);
    }

    private static void DrawCable(Canvas c) {
        // 天井から垂れ下がった配線(上端が天井)
        for (int x = 2; x <= 13; x++) {
            int sag = Mathf.RoundToInt(Mathf.Sin((x - 2) / 11f * Mathf.PI) * 7f);
            c.Set(x, 15 - sag, Cable);
        }
        c.Line(9, 15, 10, 4, Cable);
        c.Set(10, 3, Rust); c.Set(11, 3, ConcreteLight);
    }

    private static void DrawCrackA(Canvas c) {
        c.Line(3, 14, 6, 10, Outline); c.Line(6, 10, 5, 6, Outline); c.Line(5, 6, 9, 2, Outline);
        c.Line(6, 9, 10, 8, Outline); c.Line(10, 8, 12, 10, Outline);
    }

    private static void DrawCrackB(Canvas c) {
        c.Line(2, 4, 7, 7, Outline); c.Line(7, 7, 13, 6, Outline);
        c.Line(7, 7, 8, 11, Outline); c.Line(13, 6, 14, 3, Outline);
    }

    private static void DrawStain(Canvas c) {
        // 雨漏りの染み(縦に流れる)
        for (int x = 4; x <= 11; x++) {
            int len = 4 + (x * 7 % 5) * 2;
            for (int y = 15; y > 15 - len; y--) c.Set(x, y, Stain);
        }
    }

    // ---- 緑化(BuildingGreen用) ----

    private static void DrawFern(Canvas c) {
        // 背の高いシダ(16x24、下端が床)
        int[][] fronds = { new[] { 7, 0, 2, 16 }, new[] { 8, 0, 13, 18 }, new[] { 7, 0, 7, 21 }, new[] { 8, 0, 11, 12 }, new[] { 7, 0, 4, 11 } };
        foreach (var f in fronds) {
            c.Line(f[0], f[1], f[2], f[3], Stem);
            int n = Mathf.Max(Mathf.Abs(f[2] - f[0]), f[3]);
            for (int i = 3; i < n; i += 2) {
                float t = i / (float)n;
                int x = Mathf.RoundToInt(Mathf.Lerp(f[0], f[2], t));
                int y = Mathf.RoundToInt(Mathf.Lerp(f[1], f[3], t));
                c.Set(x - 1, y, Leaf); c.Set(x + 1, y, LeafLight);
                if (i % 4 == 1) { c.Set(x - 2, y - 1, LeafDark); c.Set(x + 2, y - 1, Leaf); }
            }
        }
    }

    private static void DrawGrass(Canvas c, int variant) {
        int seed = variant * 5;
        for (int x = 1; x < 15; x++) {
            int h = 1 + ((x + seed) * 7 % 6);
            if ((x + variant) % 3 == 0) h += 2;
            for (int y = 0; y < h; y++) c.Set(x, y, y == h - 1 ? LeafLight : (x % 2 == 0 ? Leaf : LeafDark));
        }
    }

    private static void DrawFlower(Canvas c) {
        DrawGrass(c, 3);
        c.Line(5, 0, 5, 8, Stem); c.Line(11, 0, 11, 6, Stem);
        c.Set(5, 9, FlowerYellow); c.Set(4, 9, FlowerPink); c.Set(6, 9, FlowerPink); c.Set(5, 10, FlowerPink);
        c.Set(11, 7, FlowerYellow); c.Set(10, 7, Color.white); c.Set(12, 7, Color.white); c.Set(11, 8, Color.white);
    }

    private static void DrawMossPatch(Canvas c, int variant) {
        // 壁に張り付いた苔(半透明で地形の質感を残す)
        int cx = variant == 1 ? 6 : 9, cy = variant == 1 ? 9 : 6;
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++) {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy) * 1.6f);
                if (d < 4.5f + ((x * 3 + y * 5) % 3)) c.Set(x, y, Moss);
            }
        c.Set(cx, cy, LeafLight); c.Set(cx + 2, cy - 1, Leaf);
    }
}
