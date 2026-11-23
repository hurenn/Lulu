using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// スプライトシートの各コマの周りに、アウトラインを描くための透明な余白を作る。
/// アウトライン(AllIn1SpriteShaderなど)は絵を描く範囲(切り抜き枠)の内側にしか描けないため、
/// 絵の中身が枠の端に接していると線が欠ける。
/// - 枠を外側へ広げられるシートは、枠を広げるだけ(画像は変えない)
/// - テクスチャの端や隣のコマに当たって広げられないシートは、各コマの周りに余白を付けて並べ直した画像に作り直す
/// どちらもピボットをテクスチャ上の絵の同じ位置に保つので、表示位置は変わらない。メッシュはFull Rectにする。
/// 対象は、プレハブ内でSpriteOutlineWidthを付けたSpriteRendererが使う全シート(Animatorで切り替える絵と、今の絵)。
/// シートを書き出し直した時や、アウトラインを付けるキャラを増やした時は、メニューからもう一度実行する
/// </summary>
public static class SpriteOutlinePadding {
    public const int PADDING = SpriteOutlinePaddingLimit.PADDING; // 余白(取り込み後のテクスチャのピクセル数)。アウトラインの太さの上限になる
    private static readonly string[] PREFAB_FOLDERS = { "Assets/Resources_/Prefabs", "Assets/Resources/Prefabs" };

    [MenuItem("Tools/Lulu/アウトラインを付けたキャラのシートに余白を付ける")]
    private static void _PadOutlinedSheets() {
        var sprites = GetOutlinedSprites();
        var textures = sprites.Select(s => s.texture).Distinct().OrderBy(t => t.name).ToList();
        // 元のPNGを直接書き換えてUndoもできないので、何をするかを見せて確認してから実行する
        var plan = PadSheets(textures, true, sprites);
        const int MAX_PLAN_LENGTH = 1500;
        string plan_text = plan.Length > MAX_PLAN_LENGTH ? plan.Substring(0, MAX_PLAN_LENGTH) + "\n…(続きはコンソール)" : plan;
        Debug.Log(plan);
        if (!EditorUtility.DisplayDialog("シートに余白を付ける",
                "元のPNGを直接書き換えます(Undoできません。戻す時はgitで戻してください)。\n\n" + plan_text,
                "実行", "キャンセル")) {
            return;
        }
        var log = PadSheets(textures, false, sprites);
        Debug.Log(log);
    }

    /// <summary>
    /// プレハブ内でSpriteOutlineWidthを付けたSpriteRendererが使う全シート
    /// </summary>
    public static List<Texture2D> GetOutlinedTextures() {
        return GetOutlinedSprites().Select(s => s.texture).Distinct().OrderBy(t => t.name).ToList();
    }

    /// <summary>
    /// プレハブ内でSpriteOutlineWidthを付けたSpriteRendererが使う全ての絵(Animatorで切り替える絵と、今の絵)
    /// </summary>
    public static HashSet<Sprite> GetOutlinedSprites() {
        var sprites = new HashSet<Sprite>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", PREFAB_FOLDERS)) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var outline in prefab.GetComponentsInChildren<SpriteOutlineWidth>(true)) {
                var renderer = outline.GetComponent<SpriteRenderer>();
                if (renderer.sprite != null) sprites.Add(renderer.sprite);
                var animator = renderer.GetComponentInParent<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null) continue;
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, animator.transform);
                foreach (var clip in animator.runtimeAnimatorController.animationClips) {
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)) {
                        if (binding.type != typeof(SpriteRenderer) || binding.path != path) continue;
                        foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding)) {
                            if (key.value is Sprite sprite) sprites.Add(sprite);
                        }
                    }
                }
            }
        }
        return sprites;
    }

    /// <summary>
    /// 指定したシートに余白を付ける。処理内容をまとめた文字列を返す(dry_runなら変更せずに、何をするかだけ返す)。
    /// used_spritesを渡すと、枠を広げるのはその絵だけにする(他の物と共有しているシートで、関係ない絵の見た目を変えないため)
    /// </summary>
    public static string PadSheets(IEnumerable<Texture2D> textures, bool dry_run = false, ICollection<Sprite> used_sprites = null) {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var log = new System.Text.StringBuilder();
        foreach (var texture in textures) {
            string path = AssetDatabase.GetAssetPath(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(path));

            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();

            // 当たり判定や輪郭の形を保存しているシートは、枠を動かすと形がずれるため処理しない
            var physics = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            var outlines = provider.GetDataProvider<ISpriteOutlineDataProvider>();
            if (rects.Any(r => (physics?.GetOutlines(r.spriteID)?.Count ?? 0) > 0 || (outlines?.GetOutlines(r.spriteID)?.Count ?? 0) > 0)) {
                log.AppendLine($"{path}: 当たり判定・輪郭の形が保存されているため、処理しなかった");
                Object.DestroyImmediate(image);
                continue;
            }

            // 枠は元画像のピクセルで扱う。取り込み時に縮小されるシートは、その分だけ余白を大きく取る
            float scale = Mathf.Max(1f, Mathf.Max(image.width, image.height) / (float)importer.maxTextureSize);
            int pad = Mathf.CeilToInt(PADDING * scale);

            var used_names = used_sprites == null ? null : new HashSet<string>(used_sprites.Where(s => s.texture == texture).Select(s => s.name));
            bool IsUsed(SpriteRect r) => used_names == null || used_names.Contains(r.name);
            var expanded = rects.Select(r => IsUsed(r) ? _ExpandRect(r, rects, image.width, image.height, pad) : r.rect).ToArray();
            bool needs_repack = rects.Select((r, i) => IsUsed(r) ? _ContentMargin(image, r.rect, expanded[i], pad) : pad).Any(m => m < pad);
            if (dry_run) {
                log.AppendLine($"{path}: {(needs_repack ? "並べ直す" : "枠を広げる")} (コマ{rects.Length})");
                Object.DestroyImmediate(image);
                continue;
            }
            var size = needs_repack ? _Repack(path, importer, image, rects, pad, scale) : Vector2Int.zero;
            if (size != Vector2Int.zero) {
                log.AppendLine($"{texture.name}: 余白を付けて並べ直した ({image.width}x{image.height} → {size.x}x{size.y})");
            } else {
                // 並べ直すと画像が元より大きくなるシート(他と共有する大きなシートなど)は、枠を広げるだけにする
                if (needs_repack) log.AppendLine($"{texture.name}: 並べ直すと画像が大きくなるため、枠を広げるだけにした(線が欠けるコマがあり得る)");
                for (int i = 0; i < rects.Length; i++) if (IsUsed(rects[i])) _SetRectKeepPivot(rects[i], expanded[i]);
                log.AppendLine($"{texture.name}: 枠を広げた");
            }
            Object.DestroyImmediate(image);

            provider.SetSpriteRects(rects);
            provider.Apply();
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return log.ToString();
    }

    // 枠を、他の枠との隙間の半分・テクスチャの端・padのうち最も小さい量だけ外側へ広げる
    private static Rect _ExpandRect(SpriteRect target, SpriteRect[] all, int width, int height, int pad) {
        var r = target.rect;
        float left = Mathf.Min(pad, r.xMin), right = Mathf.Min(pad, width - r.xMax);
        float down = Mathf.Min(pad, r.yMin), up = Mathf.Min(pad, height - r.yMax);
        foreach (var other in all) {
            if (other == target) continue;
            var q = other.rect;
            bool overlap_y = q.yMin < r.yMax + pad && q.yMax > r.yMin - pad;
            bool overlap_x = q.xMin < r.xMax + pad && q.xMax > r.xMin - pad;
            if (overlap_y && q.xMin >= r.xMax) right = Mathf.Min(right, Mathf.Floor((q.xMin - r.xMax) / 2f));
            if (overlap_y && q.xMax <= r.xMin) left = Mathf.Min(left, Mathf.Floor((r.xMin - q.xMax) / 2f));
            if (overlap_x && q.yMin >= r.yMax) up = Mathf.Min(up, Mathf.Floor((q.yMin - r.yMax) / 2f));
            if (overlap_x && q.yMax <= r.yMin) down = Mathf.Min(down, Mathf.Floor((r.yMin - q.yMax) / 2f));
        }
        left = Mathf.Max(0, left); right = Mathf.Max(0, right); down = Mathf.Max(0, down); up = Mathf.Max(0, up);
        return new Rect(r.xMin - left, r.yMin - down, r.width + left + right, r.height + down + up);
    }

    // 広げた枠の端から、元の枠の中の絵(不透明な部分)までの最小距離。絵が無ければpad
    private static int _ContentMargin(Texture2D image, Rect original, Rect expanded, int pad) {
        int min_x = int.MaxValue, max_x = -1, min_y = int.MaxValue, max_y = -1;
        for (int y = (int)original.yMin; y < (int)original.yMax; y++) {
            for (int x = (int)original.xMin; x < (int)original.xMax; x++) {
                if (image.GetPixel(x, y).a <= 0.04f) continue;
                min_x = Mathf.Min(min_x, x); max_x = Mathf.Max(max_x, x);
                min_y = Mathf.Min(min_y, y); max_y = Mathf.Max(max_y, y);
            }
        }
        if (max_x < 0) return pad;
        float margin = Mathf.Min(Mathf.Min(min_x - expanded.xMin, expanded.xMax - 1 - max_x),
                                 Mathf.Min(min_y - expanded.yMin, expanded.yMax - 1 - max_y));
        return (int)margin;
    }

    // ピボットをテクスチャ上の同じ位置に保ったまま枠を変える
    private static void _SetRectKeepPivot(SpriteRect sprite, Rect rect, Vector2 offset = default) {
        Vector2 pivot_abs = sprite.rect.position + Vector2.Scale(sprite.pivot, sprite.rect.size) + offset;
        sprite.alignment = SpriteAlignment.Custom;
        sprite.pivot = new Vector2((pivot_abs.x - rect.xMin) / rect.width, (pivot_abs.y - rect.yMin) / rect.height);
        sprite.rect = rect;
    }

    // 各コマの周りにpadの余白を付け、元の並び順(上の段から左→右)で詰め直した画像に作り直す。作り直した画像の大きさを返す。
    // 縦横の長い方が元の画像より大きくなる場合は、作り直さずにVector2Int.zeroを返す
    private static Vector2Int _Repack(string path, TextureImporter importer, Texture2D image, SpriteRect[] rects, int pad, float scale) {
        var order = rects.OrderByDescending(r => r.rect.yMax).ThenBy(r => r.rect.xMin).ToList();
        // 枠が小数の位置にある場合も、ピクセル単位で丸ごと写す
        RectInt PixelRect(Rect r) {
            int x_min = Mathf.FloorToInt(r.xMin), y_min = Mathf.FloorToInt(r.yMin);
            return new RectInt(x_min, y_min, Mathf.CeilToInt(r.xMax) - x_min, Mathf.CeilToInt(r.yMax) - y_min);
        }
        // 段ごとに並べる位置を決める(テクスチャ上はyが上向きなので、上の段から下へ置いた後で反転する)
        Dictionary<SpriteRect, Vector2Int> Layout(int max_width, out int used_width, out int used_height) {
            var result = new Dictionary<SpriteRect, Vector2Int>();
            int x = 0, y = 0, row_height = 0;
            used_width = 0;
            foreach (var sprite in order) {
                var src = PixelRect(sprite.rect);
                int w = src.width + pad * 2, h = src.height + pad * 2;
                if (x + w > max_width && x > 0) { x = 0; y += row_height; row_height = 0; }
                result[sprite] = new Vector2Int(x, y);
                x += w; row_height = Mathf.Max(row_height, h);
                used_width = Mathf.Max(used_width, x);
            }
            used_height = y + row_height;
            return result;
        }
        // 元の幅から始めて幅を広げていき、縦横の長い方が最も短くなる並べ方を選ぶ(画像の大きさの上限を超えないように)
        int cell_width = order.Max(s => PixelRect(s.rect).width + pad * 2);
        var placed = Layout(image.width, out int width, out int height);
        for (int max_width = image.width + cell_width; max_width <= image.width + cell_width * order.Count; max_width += cell_width) {
            var candidate = Layout(max_width, out int w, out int h);
            if (Mathf.Max(w, h) < Mathf.Max(width, height)) { placed = candidate; width = w; height = h; }
        }
        width = Mathf.Max(width, image.width);
        int original_max = Mathf.Max(image.width, image.height);
        if (Mathf.Max(width, height) > original_max + Mathf.CeilToInt(4 * scale)) return Vector2Int.zero;
        // 取り込み時に縮小された後も4の倍数になるようにする(4の倍数でないと圧縮されない)。
        // 縮小されるシートは、長い方を元の画像の長さに揃えて縮小率を保ち、短い方は縮小後に4の倍数になる長さまで伸ばす
        if (scale > 1f) {
            int long_side = Mathf.Max(original_max, Mathf.Max(width, height));
            float shrink = importer.maxTextureSize / (float)long_side;
            int Fit(int length) {
                while (Mathf.RoundToInt(length * shrink) % 4 != 0) length++;
                return length;
            }
            if (width >= height) { width = long_side; height = Fit(height); } else { height = long_side; width = Fit(width); }
        } else {
            width = (width + 3) / 4 * 4; height = (height + 3) / 4 * 4;
        }

        var packed = new Texture2D(width, height, TextureFormat.RGBA32, false);
        packed.SetPixels32(new Color32[width * height]);
        foreach (var sprite in order) {
            var src = PixelRect(sprite.rect);
            var pos = placed[sprite];
            int dst_x = pos.x + pad;
            int dst_y = height - pos.y - src.height - pad; // 上から置いた位置を、下が原点の座標へ
            packed.SetPixels(dst_x, dst_y, src.width, src.height, image.GetPixels(src.x, src.y, src.width, src.height));
            var cell = new Rect(dst_x - pad, dst_y - pad, src.width + pad * 2, src.height + pad * 2);
            _SetRectKeepPivot(sprite, cell, new Vector2(dst_x - src.x, dst_y - src.y));
        }
        packed.Apply();
        File.WriteAllBytes(path, packed.EncodeToPNG());
        Object.DestroyImmediate(packed);

        // 取り込み時の縮小率が変わらないように、最大サイズを合わせる
        int needed = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Max(width, height) / scale));
        if (importer.maxTextureSize < needed) importer.maxTextureSize = needed;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return new Vector2Int(width, height);
    }
}
