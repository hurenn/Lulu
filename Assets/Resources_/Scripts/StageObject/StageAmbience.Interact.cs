using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// StageAmbience のうち「ルルの動きへの反応」を担当する部分。
/// 草・配線の押しのけ(揺れシェーダーへルルの位置を渡す)、草むらの葉っぱ、配線の火花、ガラスのきらめき、蝶が逃げる、
/// 物理で動く小物・とまっている鳥・水たまりの自動配置を行う。
/// </summary>
public partial class StageAmbience {
    [Header("ルルへの反応: 草・配線を押しのける")]
    [SerializeField] private bool _pushPlants = true;
    [Tooltip("この速さで押しのけが最大になる(ダッシュ程度)")]
    [SerializeField] private float _fullPushSpeed = 12f;

    [Header("ルルへの反応: 草むら・配線・ガラス・蝶")]
    [Tooltip("草むらを走り抜けた時に舞う葉っぱ")]
    [SerializeField] private GameObject _grassLeafPrefab;
    [Tooltip("葉っぱが舞う最低の速さ")]
    [SerializeField] private float _grassLeafMinSpeed = 3.5f;
    [Tooltip("垂れた配線に触れた時に火花を出す")]
    [SerializeField] private bool _cableTouchSpark = true;
    [Tooltip("ガラス片に近づいた時のきらめき")]
    [SerializeField] private GameObject _glintPrefab;
    [Tooltip("蝶がルルから逃げる強さ(0でオフ)")]
    [SerializeField] private float _butterflyRepel = 3f;

    [Header("自動で置く物")]
    [Tooltip("床に置く蹴れる小物(小石・空き缶・紙くずなど)")]
    [SerializeField] private GameObject[] _floorProps = new GameObject[0];
    [Tooltip("コンクリートの床100マスあたりの小物の数")]
    [SerializeField] private float _floorPropsPer100 = 6f;
    [SerializeField] private GameObject _perchedBirdsPrefab;
    [Tooltip("とまっている鳥どうしの最低間隔(マス)")]
    [SerializeField] private int _perchedBirdsSpacing = 50;
    [Tooltip("置ける床のうち、実際に鳥を置く確率")]
    [Range(0f, 1f)] [SerializeField] private float _perchedBirdsChance = 0.12f;
    [SerializeField] private GameObject _puddlePrefab;
    [Tooltip("水たまりどうしの最低間隔(マス)")]
    [SerializeField] private int _puddleSpacing = 22;

    private static readonly int LuluPosId = Shader.PropertyToID("_LuluPos");
    private readonly List<Vector3> _grassSpots = new List<Vector3>();
    private readonly List<Vector3> _glassSpots = new List<Vector3>();
    private readonly List<Vector3> _concreteFloors = new List<Vector3>();
    private readonly Dictionary<int, float> _cooldowns = new Dictionary<int, float>();
    private ParticleSystemForceField _repelField;

    private void _InteractStart() {
        _ScanDecorations();
        _ScanFloors();
        var rng = new System.Random(gameObject.scene.name.GetHashCode());
        _PlaceFloorProps(rng);
        _PlaceSpaced(_perchedBirdsPrefab, _concreteFloors, _perchedBirdsSpacing, rng, _perchedBirdsChance);
        _PlacePuddles(rng);
        if (_butterflyRepel > 0f) {
            var go = new GameObject("ButterflyRepel");
            go.transform.SetParent(transform, false);
            _repelField = go.AddComponent<ParticleSystemForceField>();
            _repelField.shape = ParticleSystemForceFieldShape.Sphere;
            _repelField.endRange = 2.5f;
            _repelField.gravity = -_butterflyRepel; // マイナス = 中心から遠ざける
        }
    }

    private void OnDisable() {
        Shader.SetGlobalVector(LuluPosId, Vector4.zero); // ルルがいない時は押しのけを無効にする(w=0)
    }

    private void _InteractUpdate() {
        var lulu = PlayerCharacterManager.Current;
        if (lulu == null) { Shader.SetGlobalVector(LuluPosId, Vector4.zero); return; }
        Vector3 pos = lulu.transform.position;
        Vector2 vel = lulu.Velocity;
        float speed = vel.magnitude;
        float half = lulu.GetCharacterSize().y * 0.5f;

        // 草・配線の押しのけ(体の中心を渡す。草は足元、配線は頭の高さで反応する)
        if (_pushPlants) Shader.SetGlobalVector(LuluPosId, new Vector4(pos.x, pos.y, Mathf.Clamp01(speed / _fullPushSpeed), 1f));
        if (_repelField != null) _repelField.transform.position = pos;

        float now = Time.time;
        // 草むらを走り抜けると葉っぱが舞う
        if (_grassLeafPrefab != null && Mathf.Abs(vel.x) >= _grassLeafMinSpeed)
            _TouchSpots(_grassSpots, pos + Vector3.down * half, 0.6f, 0.6f, 1.2f, p => Instantiate(_grassLeafPrefab, p, Quaternion.identity, transform));
        // 垂れた配線に触れると火花
        if (_cableTouchSpark && _sparks.prefab != null)
            _TouchSpots(_cables, pos, 0.6f, half + 0.4f, 0.7f, p => Instantiate(_sparks.prefab, p, Quaternion.identity, transform));
        // ガラス片に近づくときらっと光る
        if (_glintPrefab != null && speed > 0.5f)
            _TouchSpots(_glassSpots, pos + Vector3.down * half, 1.2f, 0.8f, 2.5f, p => Instantiate(_glintPrefab, p + Vector3.up * 0.1f, Quaternion.identity, transform));
    }

    // 範囲内の場所ごとに、冷却時間を空けて反応を出す
    private void _TouchSpots(List<Vector3> spots, Vector3 center, float rx, float ry, float cooldown, System.Action<Vector3> spawn) {
        float now = Time.time;
        foreach (var s in spots) {
            if (Mathf.Abs(s.x - center.x) > rx || Mathf.Abs(s.y - center.y) > ry) continue;
            int key = (Mathf.RoundToInt(s.x * 10f) * 73856093) ^ (Mathf.RoundToInt(s.y * 10f) * 19349663) ^ spots.GetHashCode();
            if (_cooldowns.TryGetValue(key, out var t) && now - t < cooldown) continue;
            _cooldowns[key] = now;
            spawn(s);
        }
    }

    // ------------------------------------------------------------------
    // 場所の読み取り
    // ------------------------------------------------------------------

    private void _ScanDecorations() {
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None)) {
            if (!tm.name.StartsWith("Deco_Top")) continue;
            var b = tm.cellBounds; var tiles = tm.GetTilesBlock(b); int i = 0;
            for (int z = b.zMin; z < b.zMax; z++)
                for (int y = b.yMin; y < b.yMax; y++)
                    for (int x = b.xMin; x < b.xMax; x++, i++) {
                        var t = tiles[i]; if (t == null) continue;
                        var p = tm.GetCellCenterWorld(new Vector3Int(x, y, z)) + Vector3.down * 0.3f;
                        if (t.name.Contains("Grass") || t.name.Contains("Fern") || t.name.Contains("Flower")) _grassSpots.Add(p);
                        else if (t.name.Contains("Glass")) _glassSpots.Add(p);
                    }
        }
    }

    private void _ScanFloors() {
        if (_solidForShafts == null || _grid == null) return;
        float half = _grid.cellSize.y * 0.5f;
        foreach (var kv in _solidForShafts) {
            if (kv.Value) continue; // コンクリートの床だけ
            var above = kv.Key + Vector3Int.up;
            if (_solidForShafts.ContainsKey(above) || _solidForShafts.ContainsKey(above + Vector3Int.up)) continue; // 上に2マス以上の空き
            _concreteFloors.Add(_grid.GetCellCenterWorld(above) + Vector3.down * half);
        }
        _concreteFloors.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
    }

    // ------------------------------------------------------------------
    // 自動配置
    // ------------------------------------------------------------------

    private void _PlaceFloorProps(System.Random rng) {
        if (_floorProps == null || _floorProps.Length == 0 || _concreteFloors.Count == 0) return;
        int count = Mathf.RoundToInt(_concreteFloors.Count * _floorPropsPer100 / 100f);
        for (int i = 0; i < count; i++) {
            var p = _concreteFloors[rng.Next(_concreteFloors.Count)];
            var prefab = _floorProps[rng.Next(_floorProps.Length)];
            if (prefab == null) continue;
            var go = Instantiate(prefab, p + new Vector3((float)rng.NextDouble() * 0.8f - 0.4f, 0.2f, 0f), Quaternion.Euler(0, 0, rng.Next(-15, 16)), transform); // 立った向きにならないよう、傾きは少しだけ
            go.name = prefab.name;
        }
    }

    private void _PlaceSpaced(GameObject prefab, List<Vector3> spots, int spacing, System.Random rng, float chance) {
        if (prefab == null) return;
        var used = new List<Vector3>();
        float cell = _grid != null ? _grid.cellSize.x : 1f;
        foreach (var p in spots) {
            if (rng.NextDouble() > chance) continue;
            bool near = false;
            foreach (var u in used) if (Mathf.Abs(u.x - p.x) < spacing * cell && Mathf.Abs(u.y - p.y) < 6f) { near = true; break; }
            if (near) continue;
            used.Add(p);
            Instantiate(prefab, p, Quaternion.identity, transform);
        }
    }

    // 水たまり: 天井から水が垂れていそうな所(上に天井がある床)に置く
    private void _PlacePuddles(System.Random rng) {
        if (_puddlePrefab == null) return;
        var candidates = new List<Vector3>();
        foreach (var p in _concreteFloors) {
            var c = _grid.WorldToCell(p);
            for (int k = 2; k < 9; k++) if (_solidForShafts.ContainsKey(c + Vector3Int.up * k)) { candidates.Add(p); break; }
        }
        _PlaceSpaced(_puddlePrefab, candidates, _puddleSpacing, rng, 0.25f);
    }
}
