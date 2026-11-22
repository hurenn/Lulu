using System.Collections;
using UnityEngine;

[System.Serializable]
public class ParallaxLayer {
    public string name;
    [Tooltip("このレイヤーで動かすオブジェクト。ループする場合は、シーン上で並べた間隔のまま繰り返す")]
    public Transform[] objects;
    [Range(0f, 1f)]
    [Tooltip("0=遠景(画面に固定)、1=手前(地形と同じ速さ)。大きいほど速く流れる")]
    public float parallaxFactor = 0.5f;
    [Tooltip("ループするか")]
    public bool loop = false;
    [Tooltip("ループの周期(ワールド単位)。0なら自動 = 並べた範囲(端のオブジェクト同士の距離) + 平均の間隔")]
    [Min(0f)] public float loopWidth = 0f;
}

/// <summary>
/// パララックス背景。カメラの子に置き、カメラの移動量に係数を掛けて逆方向へ動かす。
/// ループするレイヤーは、シーン上で並べた位置関係を保ったまま、カメラから周期の半分以上離れたオブジェクトを反対側へ回す。
/// BackGroundを選択すると、シーンビューにループ後の位置(左右の繰り返し)を枠で表示する。
/// </summary>
public class BackGround : MonoBehaviour {
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private ParallaxLayer[] layers;
    [Tooltip("自動スクロール速度（単位/秒）。0で無効")]
    [SerializeField] private float autoScrollSpeed = 0f;
    [Tooltip("シェイク強度の倍率")]
    [SerializeField] private float shakeMultiplier = 0.3f;

    private Vector3 lastCamPos;
    private float[] _loopWidths;
    private Coroutine _shakeCo;
    private Vector3[] _shakeOffsets;

    void OnEnable()  => CinemachineManager.OnShake += _StartShake;
    void OnDisable() => CinemachineManager.OnShake -= _StartShake;

    void Start() {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        lastCamPos = cameraTransform.position;
        _shakeOffsets = new Vector3[layers.Length];

        // ループの周期を決める。オブジェクトは動かさない(シーン上の配置をそのまま使う)
        _loopWidths = new float[layers.Length];
        for (int i = 0; i < layers.Length; i++) {
            _loopWidths[i] = GetLoopWidth(layers[i]);
        }
    }

    void LateUpdate() {
        float deltaX = cameraTransform.position.x - lastCamPos.x;
        float camX = cameraTransform.position.x;

        for (int i = 0; i < layers.Length; i++) {
            var layer = layers[i];

            foreach (var obj in layer.objects) {
                if (obj == null) continue;
                // 反対方向パララックス
                obj.position += Vector3.right * (-deltaX * layer.parallaxFactor - autoScrollSpeed * layer.parallaxFactor * Time.deltaTime);
            }

            // ループ処理: カメラから周期の半分以上離れたら、周期1つ分だけ反対側へ回す
            if (layer.loop && _loopWidths[i] > 0f) {
                float period = _loopWidths[i];
                float half = period * 0.5f;
                foreach (var obj in layer.objects) {
                    if (obj == null) continue;
                    float diff = obj.position.x - camX;
                    if (diff > half) {
                        obj.position -= Vector3.right * period * Mathf.Ceil((diff - half) / period);
                    } else if (diff < -half) {
                        obj.position += Vector3.right * period * Mathf.Ceil((-half - diff) / period);
                    }
                }
            }
        }

        lastCamPos = cameraTransform.position;
    }

    /// <summary>
    /// ループの周期(ワールド単位)。loopWidthが0なら、並べた範囲 + 平均の間隔で自動計算する。
    /// 例: 幅Wの絵を隙間なく2枚並べた場合は 2W、間隔Dで3つ並べた場合は 3D になる
    /// </summary>
    public static float GetLoopWidth(ParallaxLayer layer) {
        if (layer == null || !layer.loop) return 0f;
        if (layer.loopWidth > 0f) return layer.loopWidth;

        var xs = new System.Collections.Generic.List<float>();
        Transform single = null;
        foreach (var obj in layer.objects) {
            if (obj == null) continue;
            xs.Add(obj.position.x);
            single = obj;
        }
        if (xs.Count == 0) return 0f;
        if (xs.Count == 1) return GetWorldBounds(single).size.x; // 1つだけなら自身の幅で繰り返す

        xs.Sort();
        float span = xs[xs.Count - 1] - xs[0];
        return span + span / (xs.Count - 1);
    }

    /// <summary>オブジェクト(子を含む)の描画範囲</summary>
    public static Bounds GetWorldBounds(Transform t) {
        var renderers = t.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(t.position, Vector3.zero);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

#if UNITY_EDITOR
    /// <summary>ループするレイヤーのオブジェクトを、先頭の位置から隙間なく横に並べ直す(調整の起点用)</summary>
    [ContextMenu("ループ素材を隙間なく並べ直す")]
    private void _ArrangeAdjacent() {
        if (layers == null) return;
        foreach (var layer in layers) {
            if (layer == null || !layer.loop || layer.objects == null || layer.objects.Length < 2) continue;
            var first = layer.objects[0];
            if (first == null) continue;
            float x = first.position.x;
            for (int j = 0; j < layer.objects.Length; j++) {
                var obj = layer.objects[j];
                if (obj == null) continue;
                float w = GetWorldBounds(obj).size.x;
                if (j > 0) {
                    UnityEditor.Undo.RecordObject(obj, "Arrange Parallax Layer");
                    var p = obj.position; p.x = x; obj.position = p;
                }
                x += w;
            }
        }
    }

    // 選択中: 各オブジェクトの範囲(実線)と、ループで左右に繰り返される位置(薄い枠)を表示
    private void OnDrawGizmosSelected() {
        if (layers == null) return;
        Color[] colors = { new Color(1f, 0.6f, 0.2f), new Color(0.3f, 0.8f, 1f), new Color(0.6f, 1f, 0.4f), new Color(1f, 0.4f, 0.8f) };
        for (int i = 0; i < layers.Length; i++) {
            var layer = layers[i];
            if (layer == null || layer.objects == null) continue;
            var col = colors[i % colors.Length];
            float period = Application.isPlaying && _loopWidths != null && i < _loopWidths.Length ? _loopWidths[i] : GetLoopWidth(layer);
            foreach (var obj in layer.objects) {
                if (obj == null) continue;
                var b = GetWorldBounds(obj);
                Gizmos.color = col;
                Gizmos.DrawWireCube(b.center, b.size);
                if (!layer.loop || period <= 0f) continue;
                Gizmos.color = new Color(col.r, col.g, col.b, 0.35f);
                Gizmos.DrawWireCube(b.center + Vector3.right * period, b.size);
                Gizmos.DrawWireCube(b.center - Vector3.right * period, b.size);
            }
            if (layer.loop && period > 0f && layer.objects.Length > 0 && layer.objects[0] != null) {
                var b0 = GetWorldBounds(layer.objects[0]);
                UnityEditor.Handles.color = col;
                UnityEditor.Handles.Label(new Vector3(b0.min.x, b0.max.y, 0f), $"{layer.name}  周期 {period:0.##}" + (layer.loopWidth > 0f ? "(手動)" : "(自動)"));
            }
        }
    }
#endif

    private void _StartShake(float intensity, float duration) {
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(_ShakeCo(intensity, duration));
    }

    private IEnumerator _ShakeCo(float intensity, float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float current = intensity * shakeMultiplier * (1f - elapsed / duration);

            for (int i = 0; i < layers.Length; i++) {
                float layerIntensity = current * layers[i].parallaxFactor;
                Vector3 newOffset = new Vector3(
                    Random.Range(-layerIntensity, layerIntensity),
                    Random.Range(-layerIntensity, layerIntensity),
                    0f);
                Vector3 delta = newOffset - _shakeOffsets[i];
                _shakeOffsets[i] = newOffset;
                foreach (var obj in layers[i].objects) {
                    if (obj != null) obj.position += delta;
                }
            }
            yield return null;
        }

        // オフセットをリセット
        for (int i = 0; i < layers.Length; i++) {
            foreach (var obj in layers[i].objects) {
                if (obj != null) obj.position -= _shakeOffsets[i];
            }
            _shakeOffsets[i] = Vector3.zero;
        }
        _shakeCo = null;
    }
}
    //[SerializeField] private GameObject[] _vistaBuildings;
    //[SerializeField] private GameObject[] _closeBuildings;
    //[SerializeField] private Camera _mainCamera;
    //[SerializeField, Range(0f, 1f)] private float _vistaBuildingParallax = 0.5f;
    //[SerializeField, Range(0f, 1f)] private float _closeBuildingParallax = 0.8f;

    //// 背景の幅（ループ用）
    //[SerializeField] private float _vistaBuildingWidth = 1.0f;
    //[SerializeField] private float _closeBuildingWidth = 1.0f;

    //// 初期位置を記録
    //private Vector3[] _vistaInitialPositions;
    //private Vector3[] _closeInitialPositions;
    //private Vector3 _initialCameraPosition;

    //private void Reset() {
    //    _mainCamera = Camera.main;

    //    // "VistaBuilding"を名前に含む子オブジェクトをすべて取得
    //    _vistaBuildings = transform.GetComponentsInChildren<Transform>()
    //        .Where(t => t.gameObject.name.Contains("VistaBuilding"))
    //        .Select(t => t.gameObject)
    //        .ToArray() ?? new GameObject[0];
    //    // "CloseBuilding"を名前に含む子オブジェクトをすべて取得
    //    _closeBuildings = transform.GetComponentsInChildren<Transform>()
    //        .Where(t => t.gameObject.name.Contains("CloseBuilding"))
    //        .Select(t => t.gameObject)
    //        .ToArray() ?? new GameObject[0];

    //    _vistaBuildingWidth = _vistaBuildings.First()?.GetComponent<SpriteRenderer>()?.bounds.size.x ?? 1f;
    //    _closeBuildingWidth = _closeBuildings.First()?.GetComponent<SpriteRenderer>()?.bounds.size.x ?? 1f;
    //}

    //private void Start() {        
    //    if (_mainCamera != null) {
    //        _RecordInitialPositions();
    //    }
    //}

    //private void LateUpdate() {
    //    if (_mainCamera == null) {
    //        return;
    //    }

    //    Vector3 cameraPosition = _mainCamera.transform.position;
    //    //_LoopBuildings(_vistaBuildings, _vistaInitialPositions, _vistaBuildingWidth, _vistaBuildingParallax);
    //    //_LoopBuildings(_closeBuildings, _closeInitialPositions, _closeBuildingWidth, _closeBuildingParallax);
    //}

    ///// <summary>
    ///// 初期位置を記録
    ///// </summary>
    //private void _RecordInitialPositions() {
    //    _initialCameraPosition = _mainCamera.transform.position;

    //    // Vista建物の初期位置を記録
    //    _vistaInitialPositions = new Vector3[_vistaBuildings.Length];
    //    for (int i = 0; i < _vistaBuildings.Length; i++) {
    //        if (_vistaBuildings[i] != null) {
    //            _vistaInitialPositions[i] = _vistaBuildings[i].transform.position;
    //        }
    //    }

    //    // Close建物の初期位置を記録
    //    _closeInitialPositions = new Vector3[_closeBuildings.Length];
    //    for (int i = 0; i < _closeBuildings.Length; i++) {
    //        if (_closeBuildings[i] != null) {
    //            _closeInitialPositions[i] = _closeBuildings[i].transform.position;
    //        }
    //    }
    //}

    ///// <summary>
    ///// ローカル位置でパララックス効果を実装
    ///// </summary>
    //private void _LoopBuildings(GameObject[] buildings, Vector3[] initialPositions, float buildingWidth, float parallax) {
    //    if (buildings == null || buildings.Length == 0 || initialPositions == null) {
    //        return;
    //    }

    //    // カメラの移動量を計算
    //    Vector3 cameraDelta = _mainCamera.transform.position - _initialCameraPosition;

    //    // パララックス効果を適用したオフセット
    //    Vector3 parallaxOffset = new Vector3(
    //        cameraDelta.x * parallax,
    //        cameraDelta.y * parallax * 0.1f, // Y軸は弱めのパララックス
    //        0f
    //    );

    //    // 各建物の位置を更新
    //    for (int i = 0; i < buildings.Length; i++) {
    //        if (buildings[i] == null || i >= initialPositions.Length) continue;

    //        // 初期位置にパララックスオフセットを適用
    //        Vector3 targetPosition = initialPositions[i] + parallaxOffset;

    //        // ループ処理のための範囲計算
    //        float totalWidth = buildingWidth * buildings.Length;
    //        float cameraHalfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;
    //        float visibleRange = cameraHalfWidth + buildingWidth;

    //        // ループ判定と位置調整
    //        float relativeX = targetPosition.x - (_mainCamera.transform.position.x * parallax);

    //        // 左側に出すぎた場合
    //        while (relativeX < -visibleRange) {
    //            targetPosition.x += totalWidth;
    //            relativeX += totalWidth;
    //        }

    //        // 右側に出すぎた場合
    //        while (relativeX > visibleRange + totalWidth) {
    //            targetPosition.x -= totalWidth;
    //            relativeX -= totalWidth;
    //        }

    //        buildings[i].transform.position = targetPosition;
    //    }
    //}

    ///// <summary>
    ///// 初期位置をリセット（デバッグ用）
    ///// </summary>
    //[ContextMenu("Reset Initial Positions")]
    //public void ResetInitialPositions() {
    //    if (_mainCamera != null) {
    //        _RecordInitialPositions();
    //    }
    //}

    ///// <summary>
    ///// パララックス効果をリセット（デバッグ用）
    ///// </summary>
    //[ContextMenu("Reset Parallax")]
    //public void ResetParallax() {
    //    if (_vistaInitialPositions != null) {
    //        for (int i = 0; i < _vistaBuildings.Length && i < _vistaInitialPositions.Length; i++) {
    //            if (_vistaBuildings[i] != null) {
    //                _vistaBuildings[i].transform.position = _vistaInitialPositions[i];
    //            }
    //        }
    //    }

    //    if (_closeInitialPositions != null) {
    //        for (int i = 0; i < _closeBuildings.Length && i < _closeInitialPositions.Length; i++) {
    //            if (_closeBuildings[i] != null) {
    //                _closeBuildings[i].transform.position = _closeInitialPositions[i];
    //            }
    //        }
    //    }

    //    if (_mainCamera != null) {
    //        _initialCameraPosition = _mainCamera.transform.position;
    //    }
    //}