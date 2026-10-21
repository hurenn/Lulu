using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ルルの移動の軌跡(ワープの軌跡)
/// TrailRendererと違い、軌跡の各点が「残ってからの時間」で透明になりながら細くなって消える
/// (ワープで一瞬に引かれた長い線も、線全体が同時に薄れて消える)
/// 移動した経路に沿って白いキラキラを出す
/// </summary>
public class WarpTrail : MonoBehaviour {
    [Header("軌跡")]
    [SerializeField] private Material _material;               // 軌跡の材質(輪郭をぼかしたテクスチャ。幅方向がV)
    [SerializeField] private Color _color = new Color(0.38f, 0.46f, 0.62f, 1f); // 軌跡の色(青みがかった灰色)
    [SerializeField] private float _lifetime = 0.5f;           // 軌跡が消えるまでの時間
    [SerializeField] private float _width = 0.7f;              // 軌跡の太さ
    [SerializeField] private AnimationCurve _alphaOverLife = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.4f, 0.9f), new Keyframe(1f, 0f)); // 経過時間(0〜1)ごとの濃さ
    [SerializeField] private AnimationCurve _widthOverLife = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f));                          // 経過時間(0〜1)ごとの太さの倍率
    [SerializeField] private float _minVertexDistance = 0.1f;  // この距離以上動いたら点を足す
    [SerializeField] private float _maxSegmentLength = 0.3f;   // ワープで大きく動いた時は、この間隔で点を補う
    [SerializeField] private string _sortingLayerName = "Objects";
    [SerializeField] private int _sortingOrder = 0;

    [Header("キラキラ")]
    [SerializeField] private ParticleSystem _sparkle;          // 白いキラキラ(放出はこのスクリプトが行う)
    [SerializeField] private float _sparklePerUnit = 4f;       // 1ワールド単位動くごとに出す数
    [SerializeField] private float _sparkleSpread = 0.25f;     // 軌跡からばらける距離

    private struct Point {
        public Vector3 pos;
        public float birth;
    }

    private readonly List<Point> _points = new List<Point>();
    private Mesh _mesh;
    private GameObject _meshObject;
    private float _sparkleCarry; // 次のキラキラまでの端数
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Color> _colors = new List<Color>();
    private readonly List<Vector2> _uvs = new List<Vector2>();
    private readonly List<int> _triangles = new List<int>();

    private void Awake() {
        // 軌跡のメッシュはワールド座標で作るので、ルルに付いて動かない別のオブジェクトに描く
        _meshObject = new GameObject(name + "_Mesh");
        _mesh = new Mesh { name = "WarpTrail" };
        _mesh.MarkDynamic();
        _meshObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var mesh_renderer = _meshObject.AddComponent<MeshRenderer>();
        mesh_renderer.sharedMaterial = _material;
        mesh_renderer.sortingLayerName = _sortingLayerName;
        mesh_renderer.sortingOrder = _sortingOrder;
        mesh_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mesh_renderer.receiveShadows = false;
    }

    private void OnEnable() {
        Clear();
        if (_meshObject != null) _meshObject.SetActive(true);
    }

    private void OnDisable() {
        if (_meshObject != null) _meshObject.SetActive(false);
    }

    private void OnDestroy() {
        if (_meshObject != null) Destroy(_meshObject);
        if (_mesh != null) Destroy(_mesh);
    }

    /// <summary>
    /// 軌跡を消す
    /// </summary>
    public void Clear() {
        _points.Clear();
        _sparkleCarry = 0f;
        _mesh?.Clear();
    }

    private void LateUpdate() {
        float now = Time.time;
        var pos = transform.position;

        // 動いた分の点を足す(大きく動いた時は間を補い、キラキラも経路に沿って出す)
        if (_points.Count == 0) {
            _points.Add(new Point { pos = pos, birth = now });
        } else {
            var last = _points[_points.Count - 1].pos;
            float dist = Vector3.Distance(last, pos);
            if (dist >= _minVertexDistance) {
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist / _maxSegmentLength));
                for (int i = 1; i <= steps; i++) {
                    _points.Add(new Point { pos = Vector3.Lerp(last, pos, (float)i / steps), birth = now });
                }
                _EmitSparkles(last, pos, dist);
            }
        }

        // 寿命が尽きた点を消す
        int remove = 0;
        while (remove < _points.Count && now - _points[remove].birth >= _lifetime) remove++;
        if (remove > 0) _points.RemoveRange(0, remove);

        _BuildMesh(now, pos);
    }

    private void _EmitSparkles(Vector3 from, Vector3 to, float dist) {
        if (_sparkle == null || _sparklePerUnit <= 0f) return;
        _sparkleCarry += dist * _sparklePerUnit;
        int count = Mathf.FloorToInt(_sparkleCarry);
        _sparkleCarry -= count;
        var emit_params = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++) {
            emit_params.position = Vector3.Lerp(from, to, Random.value) + (Vector3)(Random.insideUnitCircle * _sparkleSpread);
            emit_params.applyShapeToPosition = false;
            _sparkle.Emit(emit_params, 1);
        }
    }

    private void _BuildMesh(float now, Vector3 head) {
        _mesh.Clear();
        // 今の位置を先頭として繋げる
        int count = _points.Count + 1;
        if (count < 2 || _material == null) return;

        _vertices.Clear(); _colors.Clear(); _uvs.Clear(); _triangles.Clear();
        float length = 0f;
        for (int i = 0; i < count; i++) {
            var p = _GetPos(i, head);
            var prev = _GetPos(Mathf.Max(i - 1, 0), head);
            var next = _GetPos(Mathf.Min(i + 1, count - 1), head);
            var dir = (Vector2)(next - prev);
            if (dir.sqrMagnitude < 0.000001f) dir = Vector2.right;
            var normal = new Vector3(-dir.y, dir.x, 0f).normalized;

            float life = i < _points.Count ? Mathf.Clamp01((now - _points[i].birth) / _lifetime) : 0f;
            float half_width = _width * 0.5f * _widthOverLife.Evaluate(life);
            var color = _color;
            color.a *= Mathf.Clamp01(_alphaOverLife.Evaluate(life));

            if (i > 0) length += Vector3.Distance(p, _GetPos(i - 1, head));
            _vertices.Add(p - normal * half_width);
            _vertices.Add(p + normal * half_width);
            _colors.Add(color); _colors.Add(color);
            _uvs.Add(new Vector2(length, 0f)); _uvs.Add(new Vector2(length, 1f));
            if (i > 0) {
                int v = i * 2;
                _triangles.Add(v - 2); _triangles.Add(v - 1); _triangles.Add(v);
                _triangles.Add(v - 1); _triangles.Add(v + 1); _triangles.Add(v);
            }
        }
        _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    private Vector3 _GetPos(int index, Vector3 head) => index < _points.Count ? _points[index].pos : head;
}
