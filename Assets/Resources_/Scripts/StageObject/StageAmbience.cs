using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ステージの空気感を出す動く演出をまとめて管理する(ホコリ・光の筋・天井からの崩れ・火花・水滴・蝶・葉っぱ)。
/// 開始時に地形のTilemapを読み取り「天井の下」「垂れた配線」「緑の床」などの場所を覚え、
/// カメラの近くの場所を選んで、ときどき演出を出す。地形を変えても置き直しは要らない。
/// </summary>
public class StageAmbience : MonoBehaviour {
    [System.Serializable]
    public class Spawner {
        public bool enabled = true;
        [Tooltip("出す演出(使い終わったら自分で消えるプレハブ)")]
        public GameObject prefab;
        [Tooltip("次に出すまでの間隔(秒)の最小・最大")]
        public Vector2 interval = new Vector2(2f, 5f);
        [HideInInspector] public float timer;
    }

    [Header("場所の読み取り")]
    [Tooltip("この名前のTilemapは地形として扱わない(扉など)")]
    [SerializeField] private string[] _excludedTilemapNames = { "Door" };
    [Tooltip("この文字を名前に含む地形タイルを「緑の地形」として扱う")]
    [SerializeField] private string _greenTileKeyword = "Green";
    [Tooltip("天井から垂れた配線のタイル名(火花を出す場所)")]
    [SerializeField] private string _cableTileName = "Ceiling_Cable";
    [Tooltip("カメラの外側にこの距離までなら演出を出す")]
    [SerializeField] private float _viewMargin = 2f;

    [Header("ホコリ(カメラについて回る)")]
    [SerializeField] private GameObject _dustPrefab;
    [Tooltip("霧のような埃のもや(カメラについて回る)。空にするとオフ")]
    [SerializeField] private GameObject _hazePrefab;

    [Header("光の筋(広い天井の下に固定で置く)")]
    [SerializeField] private bool _lightShaftsEnabled = true;
    [SerializeField] private GameObject _lightShaftPrefab;
    [Tooltip("天井がこのマス数以上続き、下にこのマス数以上空きがある所に置く")]
    [SerializeField] private int _lightShaftMinWidth = 6;
    [SerializeField] private int _lightShaftMinHeight = 5;
    [Tooltip("光の筋どうしの最低間隔(マス)")]
    [SerializeField] private int _lightShaftSpacing = 14;

    [Header("ときどき出す演出")]
    [Tooltip("天井から砂ぼこりと小石が落ちる(コンクリートの天井)")]
    [SerializeField] private Spawner _debris = new Spawner { interval = new Vector2(2.5f, 6f) };
    [Tooltip("垂れた配線から火花が散る")]
    [SerializeField] private Spawner _sparks = new Spawner { interval = new Vector2(1.5f, 4f) };
    [Tooltip("天井から水滴が落ちる(コンクリートの天井)")]
    [SerializeField] private Spawner _drips = new Spawner { interval = new Vector2(0.8f, 2.2f) };
    [Tooltip("緑の天井から葉っぱが舞い落ちる")]
    [SerializeField] private Spawner _leaves = new Spawner { interval = new Vector2(1.5f, 3.5f) };

    [Header("蝶(緑の床の近くを飛ぶ)")]
    [SerializeField] private bool _butterfliesEnabled = true;
    [SerializeField] private GameObject _butterflyPrefab;
    [Tooltip("画面の近くに同時にいる蝶の群れの数")]
    [SerializeField] private int _butterflyGroups = 2;

    [Header("中景のビル(窓の明かり・煙)")]
    [Tooltip("建物の絵ごとの窓と屋上の位置。パララックス背景の「Middle」層で、この絵を表示している建物に付ける")]
    [SerializeField] private BuildingLifeData[] _buildingData = new BuildingLifeData[0];
    [Tooltip("窓の明かりに使う単色のスプライト")]
    [SerializeField] private Sprite _windowLightSprite;
    [SerializeField] private Color _windowLightColor = new Color(1f, 0.95f, 0.75f, 0.9f);
    [Tooltip("窓のうち明かりを点けたり消したりする割合")]
    [Range(0f, 1f)] [SerializeField] private float _windowFlickerRatio = 0.5f;
    [Tooltip("明かりが点いている時間(秒)の最小・最大")]
    [SerializeField] private Vector2 _windowOnTime = new Vector2(0.5f, 3f);
    [Tooltip("明かりが消えている時間(秒)の最小・最大")]
    [SerializeField] private Vector2 _windowOffTime = new Vector2(2f, 8f);
    [SerializeField] private GameObject _smokePrefab;

    [Header("鳥(遠景を横切る)")]
    [SerializeField] private bool _birdsEnabled = true;
    [Tooltip("羽ばたきのコマ")]
    [SerializeField] private Sprite[] _birdFrames = new Sprite[0];
    [SerializeField] private Color _birdColor = new Color(0.25f, 0.36f, 0.42f, 1f);
    [Tooltip("群れが出るまでの間隔(秒)の最小・最大")]
    [SerializeField] private Vector2 _birdInterval = new Vector2(10f, 22f);
    [Tooltip("群れの鳥の数の最小・最大")]
    [SerializeField] private Vector2Int _birdCount = new Vector2Int(3, 7);
    [SerializeField] private float _birdSpeed = 3.5f;
    [Tooltip("鳥の描画順(雲より手前、遠景のビルより奥)")]
    [SerializeField] private int _birdSortingOrder = -100;

    private Camera _cam;
    private Grid _grid;
    private readonly List<Vector3> _concreteCeilings = new List<Vector3>(); // 天井のすぐ下(コンクリート)
    private readonly List<Vector3> _greenCeilings = new List<Vector3>();    // 天井のすぐ下(緑)
    private readonly List<Vector3> _greenFloors = new List<Vector3>();      // 床のすぐ上(緑)
    private readonly List<Vector3> _cables = new List<Vector3>();           // 配線の先端
    private readonly List<GameObject> _butterflies = new List<GameObject>();
    private Transform _dust;
    private Transform _haze;

    private void Start() {
        _cam = Camera.main;
        _ScanStage();
        if (_dustPrefab != null) _dust = Instantiate(_dustPrefab, transform).transform;
        if (_hazePrefab != null) _haze = Instantiate(_hazePrefab, transform).transform;
        if (_lightShaftsEnabled && _lightShaftPrefab != null) _PlaceLightShafts();
        foreach (var s in new[] { _debris, _sparks, _drips, _leaves }) s.timer = Random.Range(s.interval.x, s.interval.y);
        _SetupBuildings();
        _birdTimer = Random.Range(2f, _birdInterval.x);
    }

    private void Update() {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
        if (_dust != null) { var p = _cam.transform.position; _dust.position = new Vector3(p.x, p.y, 0f); }
        if (_haze != null) { var p = _cam.transform.position; _haze.position = new Vector3(p.x, p.y, 0f); }

        _Tick(_debris, _concreteCeilings, Vector3.zero);
        _Tick(_sparks, _cables, Vector3.zero);
        _Tick(_drips, _concreteCeilings, Vector3.zero);
        _Tick(_leaves, _greenCeilings, Vector3.zero);
        if (_butterfliesEnabled) _UpdateButterflies();
        _UpdateWindows();
        _UpdateBirds();
    }

    // ------------------------------------------------------------------
    // 中景のビル: 窓の明かりと煙
    // ------------------------------------------------------------------

    private class WindowLight { public SpriteRenderer sr; public float timer; public bool on; }
    private readonly List<WindowLight> _windowLights = new List<WindowLight>();

    private void _SetupBuildings() {
        if (_buildingData == null || _buildingData.Length == 0) return;
        foreach (var bg in FindObjectsByType<BackGround>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            foreach (var t in bg.GetComponentsInChildren<SpriteRenderer>(true)) {
                if (!t.name.StartsWith("MiddleBuilding")) continue;
                BuildingLifeData data = null;
                foreach (var d in _buildingData) if (d != null && d.sprite == t.sprite) data = d;
                if (data == null) continue;
                // 建物と明かり・煙をひとまとまりで描く(まとまりの描画順は建物と同じ。中では明かり・煙を建物より手前にする)
                var group = t.GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (group == null) group = t.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                group.sortingLayerID = t.sortingLayerID; group.sortingOrder = t.sortingOrder;
                // 窓: 一部だけに明かりを用意し、ときどき点けたり消したりする
                if (_windowLightSprite != null) {
                    foreach (var w in data.windows) {
                        if (Random.value > _windowFlickerRatio) continue;
                        var go = new GameObject("WindowLight");
                        go.transform.SetParent(t.transform, false);
                        // 描画順が建物と同じなので、少しだけカメラ側に置いて建物より手前に描かれるようにする
                        go.transform.localPosition = new Vector3(w.center.x, w.center.y, -0.05f);
                        var unit = _windowLightSprite.bounds.size;
                        go.transform.localScale = new Vector3(w.width / unit.x, w.height / unit.y, 1f);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = _windowLightSprite; sr.sortingLayerID = t.sortingLayerID; sr.sortingOrder = t.sortingOrder + 1;
                        var c = _windowLightColor; c.a = 0f; sr.color = c;
                        _windowLights.Add(new WindowLight { sr = sr, timer = Random.Range(0f, _windowOffTime.y), on = false });
                    }
                }
                // 煙: 屋上から細く上げる
                if (_smokePrefab != null) {
                    foreach (var p in data.smokePoints) {
                        var s = Instantiate(_smokePrefab, t.transform);
                        s.transform.localPosition = new Vector3(p.x, p.y, -0.05f);
                        foreach (var r in s.GetComponentsInChildren<ParticleSystemRenderer>()) { r.sortingLayerID = t.sortingLayerID; r.sortingOrder = t.sortingOrder + 2; }
                    }
                }
            }
        }
    }

    private void _UpdateWindows() {
        foreach (var w in _windowLights) {
            if (w.sr == null || !w.sr.gameObject.activeInHierarchy) continue;
            w.timer -= Time.deltaTime;
            if (w.timer > 0f) continue;
            w.on = !w.on;
            // 普段は消えていて、ときどき点いてすぐ消える(昼の場面なので点きっぱなしにしない)
            w.timer = w.on ? Random.Range(_windowOnTime.x, _windowOnTime.y) : Random.Range(_windowOffTime.x, _windowOffTime.y);
            var c = _windowLightColor; c.a = w.on ? _windowLightColor.a : 0f; w.sr.color = c;
        }
    }

    // ------------------------------------------------------------------
    // 鳥: ときどき遠景を群れで横切る(カメラについて動くので、遠くにいるように見える)
    // ------------------------------------------------------------------

    private class Bird { public Transform t; public SpriteRenderer sr; public Vector3 velocity; public float flap; public float bob; }
    private readonly List<Bird> _birds = new List<Bird>();
    private float _birdTimer;

    private void _UpdateBirds() {
        if (!_birdsEnabled || _birdFrames == null || _birdFrames.Length == 0 || _cam == null) return;
        _birdTimer -= Time.deltaTime;
        if (_birdTimer <= 0f) {
            _birdTimer = Random.Range(_birdInterval.x, _birdInterval.y);
            float w = _cam.orthographicSize * _cam.aspect, h = _cam.orthographicSize;
            float dir = Random.value < 0.5f ? 1f : -1f;
            var start = new Vector3(-dir * (w + 2f), Random.Range(h * 0.2f, h * 0.75f), 0f);
            int n = Random.Range(_birdCount.x, _birdCount.y + 1);
            for (int i = 0; i < n; i++) {
                var go = new GameObject("Bird");
                go.transform.SetParent(_cam.transform, false);
                // 群れはゆるいV字に並べる
                var offset = new Vector3(-dir * (i * 0.55f + Random.Range(0f, 0.3f)), (i % 2 == 0 ? 1 : -1) * (i * 0.18f) + Random.Range(-0.1f, 0.1f), 0f);
                go.transform.localPosition = start + offset + Vector3.forward * 90f;
                float s = Random.Range(0.75f, 1f);
                go.transform.localScale = new Vector3(dir * s, s, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _birdFrames[0]; sr.color = _birdColor; sr.sortingOrder = _birdSortingOrder;
                _birds.Add(new Bird { t = go.transform, sr = sr, velocity = new Vector3(dir * _birdSpeed * Random.Range(0.9f, 1.1f), Random.Range(-0.1f, 0.25f), 0f), flap = Random.value, bob = Random.value * 6f });
            }
        }
        float limit = _cam.orthographicSize * _cam.aspect + 4f;
        for (int i = _birds.Count - 1; i >= 0; i--) {
            var b = _birds[i];
            if (b.t == null) { _birds.RemoveAt(i); continue; }
            b.bob += Time.deltaTime * 3f;
            b.t.localPosition += (b.velocity + Vector3.up * Mathf.Sin(b.bob) * 0.15f) * Time.deltaTime;
            b.flap += Time.deltaTime * 6f;
            b.sr.sprite = _birdFrames[(int)b.flap % _birdFrames.Length];
            if (Mathf.Abs(b.t.localPosition.x) > limit + 3f) { Destroy(b.t.gameObject); _birds.RemoveAt(i); }
        }
    }

    // ------------------------------------------------------------------

    private void _Tick(Spawner s, List<Vector3> places, Vector3 offset) {
        if (!s.enabled || s.prefab == null || places.Count == 0) return;
        s.timer -= Time.deltaTime;
        if (s.timer > 0f) return;
        s.timer = Random.Range(s.interval.x, s.interval.y);
        if (_TryPickVisible(places, out var pos)) Instantiate(s.prefab, pos + offset, Quaternion.identity, transform);
    }

    private void _UpdateButterflies() {
        _butterflies.RemoveAll(b => b == null);
        // 画面から遠く離れた群れは消す
        for (int i = _butterflies.Count - 1; i >= 0; i--) {
            if (!_IsNearView(_butterflies[i].transform.position, _viewMargin + 12f)) { Destroy(_butterflies[i]); _butterflies.RemoveAt(i); }
        }
        if (_butterflyPrefab == null || _butterflies.Count >= _butterflyGroups) return;
        if (_TryPickVisible(_greenFloors, out var pos)) _butterflies.Add(Instantiate(_butterflyPrefab, pos + Vector3.up * 0.8f, Quaternion.identity, transform));
    }

    private readonly List<Vector3> _visibleBuffer = new List<Vector3>();

    private bool _TryPickVisible(List<Vector3> places, out Vector3 pos) {
        // 画面の近くの場所だけに絞ってから選ぶ(全体からランダムに選ぶと、場所が多いステージでは画面の近くがほとんど当たらない)
        _visibleBuffer.Clear();
        foreach (var p in places) if (_IsNearView(p, _viewMargin)) _visibleBuffer.Add(p);
        if (_visibleBuffer.Count == 0) { pos = default; return false; }
        pos = _visibleBuffer[Random.Range(0, _visibleBuffer.Count)];
        return true;
    }

    private bool _IsNearView(Vector3 p, float margin) {
        if (_cam == null) return false;
        float h = _cam.orthographicSize + margin, w = _cam.orthographicSize * _cam.aspect + margin;
        var c = _cam.transform.position;
        return Mathf.Abs(p.x - c.x) <= w && Mathf.Abs(p.y - c.y) <= h;
    }

    // ------------------------------------------------------------------
    // 地形の読み取り
    // ------------------------------------------------------------------

    private void _ScanStage() {
        var solid = new Dictionary<Vector3Int, bool>(); // マス → 緑の地形か
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None)) {
            if (tm.GetComponent<TilemapCollider2D>() == null) continue;
            if (System.Array.IndexOf(_excludedTilemapNames, tm.name) >= 0) continue;
            if (_grid == null) _grid = tm.layoutGrid;
            var b = tm.cellBounds; var tiles = tm.GetTilesBlock(b); int i = 0;
            for (int z = b.zMin; z < b.zMax; z++)
                for (int y = b.yMin; y < b.yMax; y++)
                    for (int x = b.xMin; x < b.xMax; x++, i++) {
                        if (tiles[i] == null) continue;
                        var cell = _grid.WorldToCell(tm.GetCellCenterWorld(new Vector3Int(x, y, z)));
                        bool green = tiles[i].name.Contains(_greenTileKeyword);
                        if (!solid.TryGetValue(cell, out var g) || green) solid[cell] = green; // 重なりは緑を優先(手前に描かれることが多い)
                    }
        }
        if (_grid == null) return;
        float half = _grid.cellSize.y * 0.5f;
        foreach (var kv in solid) {
            var below = kv.Key + Vector3Int.down;
            if (!solid.ContainsKey(below)) {
                var p = _grid.GetCellCenterWorld(below) + Vector3.up * (half - 0.05f); // 天井の下面
                (kv.Value ? _greenCeilings : _concreteCeilings).Add(p);
            }
            var above = kv.Key + Vector3Int.up;
            if (kv.Value && !solid.ContainsKey(above)) _greenFloors.Add(_grid.GetCellCenterWorld(above) + Vector3.down * half);
        }
        // 垂れた配線(地形装飾)の先端
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None)) {
            if (!tm.name.StartsWith("Deco_Ceiling")) continue;
            var b = tm.cellBounds; var tiles = tm.GetTilesBlock(b); int i = 0;
            for (int z = b.zMin; z < b.zMax; z++)
                for (int y = b.yMin; y < b.yMax; y++)
                    for (int x = b.xMin; x < b.xMax; x++, i++)
                        if (tiles[i] != null && tiles[i].name == _cableTileName)
                            _cables.Add(tm.GetCellCenterWorld(new Vector3Int(x, y, z)) + Vector3.down * 0.35f);
        }
        _solidForShafts = solid;
    }

    private Dictionary<Vector3Int, bool> _solidForShafts;

    // 広い天井(コンクリート)の下に光の筋を置く。シーンごとに同じ配置になるよう、位置から決める
    private void _PlaceLightShafts() {
        if (_solidForShafts == null) return;
        var solid = _solidForShafts;
        var used = new List<Vector3Int>();
        var cells = new List<Vector3Int>(solid.Keys);
        cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        bool IsCeiling(Vector3Int c) => solid.TryGetValue(c, out var green) && !green && !solid.ContainsKey(c + Vector3Int.down);
        foreach (var c in cells) {
            if (!IsCeiling(c)) continue;                     // コンクリートの天井の下面だけ
            if (IsCeiling(c + Vector3Int.left)) continue;    // 天井の左端からだけ数える
            int width = 0;
            while (IsCeiling(c + Vector3Int.right * width)) width++;
            if (width < _lightShaftMinWidth) continue;
            var mid = c + Vector3Int.right * (width / 2);
            int height = 0;
            while (height < 20 && !solid.ContainsKey(mid + Vector3Int.down * (height + 1))) height++;
            if (height < _lightShaftMinHeight) continue;
            // 近くの光の筋と間隔を空ける(高さが大きく違う別の階は気にしない)
            bool near = false;
            foreach (var u in used) if (Mathf.Abs(u.x - mid.x) < _lightShaftSpacing && Mathf.Abs(u.y - mid.y) < 8) { near = true; break; }
            if (near) continue;
            used.Add(mid);
            var top = _grid.GetCellCenterWorld(mid + Vector3Int.down) + Vector3.up * (_grid.cellSize.y * 0.5f);
            var shaft = Instantiate(_lightShaftPrefab, top, Quaternion.identity, transform);
            // 筋の長さを床までの高さに合わせる
            shaft.transform.localScale = new Vector3(1f, Mathf.Min(height, 12) / 6f, 1f);
        }
    }
}
