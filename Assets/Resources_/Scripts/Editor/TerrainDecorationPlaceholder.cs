using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Placement = TerrainDecorationProfile.ePlacement;

/// <summary>
/// 地形装飾の仮素材(16x16のドット絵タイル)と初期プロファイルを生成する。
/// 正式な絵ができたら、同じ名前のpngを上書きするか、プロファイルのタイルを差し替える。
/// </summary>
public static class TerrainDecorationPlaceholder {
    public const string Folder = "Assets/Resources_/TilePalettes/TerrainDecoration";
    public const string ProfilePath = Folder + "/TerrainDecorationProfile.asset";
    private const string SpriteFolder = Folder + "/Placeholder";
    private const string TileFolder = Folder + "/Tiles";
    private const int Size = 16;

    private static readonly string[] TerrainTilePaths = {
        "Assets/Resources_/TilePalettes/AutoTile_Building.asset",
        "Assets/Resources_/TilePalettes/AutoTile_BuildingGreen.asset",
    };

    private static readonly Color Clear = new Color(0, 0, 0, 0);
    private static readonly Color Green = new Color32(92, 184, 84, 255);
    private static readonly Color DarkGreen = new Color32(52, 120, 60, 255);
    private static readonly Color Red = new Color32(230, 80, 90, 255);
    private static readonly Color Yellow = new Color32(250, 220, 90, 255);
    private static readonly Color Stone = new Color32(150, 150, 160, 255);
    private static readonly Color StoneDark = new Color32(100, 100, 112, 255);
    private static readonly Color Ice = new Color32(150, 200, 230, 255);
    private static readonly Color Crack = new Color32(40, 40, 48, 255);
    private static readonly Color Window = new Color32(255, 210, 120, 255);
    private static readonly Color Frame = new Color32(70, 70, 80, 255);

    [MenuItem("Tools/Lulu/地形装飾の仮素材を作成")]
    public static TerrainDecorationProfile Create() {
        Directory.CreateDirectory(SpriteFolder);
        Directory.CreateDirectory(TileFolder);

        var grassA = MakeTile("Grass_A", DrawGrassA);
        var grassB = MakeTile("Grass_B", DrawGrassB);
        var flower = MakeTile("Flower", DrawFlower);
        var stone = MakeTile("Stone", DrawStone);
        var vine = MakeTile("Vine", DrawVine);
        var icicle = MakeTile("Icicle", DrawIcicle);
        var mossL = MakeTile("Moss_WallLeft", p => DrawMoss(p, true));
        var mossR = MakeTile("Moss_WallRight", p => DrawMoss(p, false));
        var crack = MakeTile("Crack", DrawCrack);
        var window = MakeTile("Window", DrawWindow);

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
        // 同じ配置先では上のルールが優先される(小石→草の順にして、小石が草に埋もれないようにする)
        profile.rules = new List<TerrainDecorationProfile.Rule> {
            Rule("小石", Placement.Top, 0.08f, 4, (stone, 1f)),
            Rule("草花", Placement.Top, 0.35f, 0, (grassA, 3f), (grassB, 2f), (flower, 1f)),
            Rule("天井のツタ", Placement.Ceiling, 0.15f, 1, (vine, 2f), (icicle, 1f)),
            Rule("壁の苔(左面)", Placement.WallLeft, 0.12f, 1, (mossL, 1f)),
            Rule("壁の苔(右面)", Placement.WallRight, 0.12f, 1, (mossR, 1f)),
            Rule("内部のひび・窓", Placement.Inner, 0.03f, 3, (crack, 2f), (window, 1f)),
        };
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

    private static Tile MakeTile(string name, System.Action<Color[]> draw) {
        string pngPath = $"{SpriteFolder}/{name}.png";
        string tilePath = $"{TileFolder}/{name}.asset";

        // 既にpngがあれば描き直さない(正式な絵に差し替え済みの場合を壊さないため)
        if (!File.Exists(pngPath)) {
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
            draw(pixels);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Size;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
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

    // ------------------------------------------------------------------
    // 仮の絵(左下原点、16x16)
    // ------------------------------------------------------------------

    private static void Set(Color[] p, int x, int y, Color c) {
        if (x < 0 || x >= Size || y < 0 || y >= Size) return;
        p[y * Size + x] = c;
    }

    private static void VLine(Color[] p, int x, int y0, int y1, Color c) {
        for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++) Set(p, x, y, c);
    }

    private static void DrawGrassA(Color[] p) {
        VLine(p, 3, 0, 4, Green); VLine(p, 4, 0, 6, DarkGreen);
        VLine(p, 8, 0, 5, Green); VLine(p, 9, 0, 3, DarkGreen);
        VLine(p, 12, 0, 4, Green);
    }

    private static void DrawGrassB(Color[] p) {
        for (int x = 1; x < 15; x++) VLine(p, x, 0, 1 + (x * 7 % 5), x % 2 == 0 ? Green : DarkGreen);
    }

    private static void DrawFlower(Color[] p) {
        VLine(p, 7, 0, 6, DarkGreen);
        Set(p, 6, 2, Green); Set(p, 8, 3, Green);
        Set(p, 7, 7, Yellow);
        Set(p, 6, 7, Red); Set(p, 8, 7, Red); Set(p, 7, 8, Red); Set(p, 7, 6, Red);
    }

    private static void DrawStone(Color[] p) {
        for (int x = 4; x <= 11; x++) VLine(p, x, 0, x >= 6 && x <= 9 ? 3 : 2, Stone);
        for (int x = 5; x <= 10; x++) Set(p, x, 0, StoneDark);
        Set(p, 7, 3, Color.white);
    }

    private static void DrawVine(Color[] p) {
        VLine(p, 3, 15, 8, DarkGreen); VLine(p, 4, 15, 11, Green);
        VLine(p, 9, 15, 5, DarkGreen); Set(p, 10, 9, Green); Set(p, 8, 7, Green);
        VLine(p, 13, 15, 10, Green);
    }

    private static void DrawIcicle(Color[] p) {
        for (int i = 0; i < 6; i++) VLine(p, 5 + i, 15, 15 - (i < 3 ? i * 3 : (5 - i) * 3) - 2, Ice);
    }

    private static void DrawMoss(Color[] p, bool hugRight) {
        // WallLeft = 壁の左面 = 装飾マスの右端に貼り付く
        for (int y = 2; y < 14; y++) {
            int w = 1 + (y * 5 % 3);
            for (int i = 0; i < w; i++) Set(p, hugRight ? Size - 1 - i : i, y, i == 0 ? DarkGreen : Green);
        }
    }

    private static void DrawCrack(Color[] p) {
        int x = 4;
        for (int y = 13; y >= 3; y--) {
            Set(p, x, y, Crack);
            if (y % 3 == 0) x++;
        }
        Set(p, 7, 8, Crack); Set(p, 8, 9, Crack); Set(p, 9, 9, Crack);
    }

    private static void DrawWindow(Color[] p) {
        for (int x = 4; x <= 11; x++)
            for (int y = 4; y <= 11; y++)
                Set(p, x, y, (x == 4 || x == 11 || y == 4 || y == 11 || x == 7 || y == 7) ? Frame : Window);
    }
}
