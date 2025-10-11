using System.Linq;
using UnityEngine;

public class BackGround : MonoBehaviour {
    [SerializeField] private GameObject[] _vistaBuildings;
    [SerializeField] private GameObject[] _closeBuildings;
    [SerializeField] private Camera _mainCamera;
    [SerializeField, Range(0f, 1f)] private float _vistaBuildingParallax = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _closeBuildingParallax = 0.8f;

    // 背景の幅（ループ用）
    [SerializeField] private float _vistaBuildingWidth = 1.0f;
    [SerializeField] private float _closeBuildingWidth = 1.0f;

    // 初期位置を記録
    private Vector3[] _vistaInitialPositions;
    private Vector3[] _closeInitialPositions;
    private Vector3 _initialCameraPosition;

    private void Reset() {
        _mainCamera = Camera.main;

        // "VistaBuilding"を名前に含む子オブジェクトをすべて取得
        _vistaBuildings = transform.GetComponentsInChildren<Transform>()
            .Where(t => t.gameObject.name.Contains("VistaBuilding"))
            .Select(t => t.gameObject)
            .ToArray() ?? new GameObject[0];
        // "CloseBuilding"を名前に含む子オブジェクトをすべて取得
        _closeBuildings = transform.GetComponentsInChildren<Transform>()
            .Where(t => t.gameObject.name.Contains("CloseBuilding"))
            .Select(t => t.gameObject)
            .ToArray() ?? new GameObject[0];

        _vistaBuildingWidth = _vistaBuildings.First()?.GetComponent<SpriteRenderer>()?.bounds.size.x ?? 1f;
        _closeBuildingWidth = _closeBuildings.First()?.GetComponent<SpriteRenderer>()?.bounds.size.x ?? 1f;
    }

    private void Start() {        
        if (_mainCamera != null) {
            _RecordInitialPositions();
        }
    }

    private void Update() {
        if (_mainCamera == null) {
            return;
        }

        Vector3 cameraPosition = _mainCamera.transform.position;
        //_LoopBuildings(_vistaBuildings, _vistaInitialPositions, _vistaBuildingWidth, _vistaBuildingParallax);
        //_LoopBuildings(_closeBuildings, _closeInitialPositions, _closeBuildingWidth, _closeBuildingParallax);
    }

    /// <summary>
    /// 初期位置を記録
    /// </summary>
    private void _RecordInitialPositions() {
        _initialCameraPosition = _mainCamera.transform.position;

        // Vista建物の初期位置を記録
        _vistaInitialPositions = new Vector3[_vistaBuildings.Length];
        for (int i = 0; i < _vistaBuildings.Length; i++) {
            if (_vistaBuildings[i] != null) {
                _vistaInitialPositions[i] = _vistaBuildings[i].transform.position;
            }
        }

        // Close建物の初期位置を記録
        _closeInitialPositions = new Vector3[_closeBuildings.Length];
        for (int i = 0; i < _closeBuildings.Length; i++) {
            if (_closeBuildings[i] != null) {
                _closeInitialPositions[i] = _closeBuildings[i].transform.position;
            }
        }
    }

    /// <summary>
    /// ローカル位置でパララックス効果を実装
    /// </summary>
    private void _LoopBuildings(GameObject[] buildings, Vector3[] initialPositions, float buildingWidth, float parallax) {
        if (buildings == null || buildings.Length == 0 || initialPositions == null) {
            return;
        }

        // カメラの移動量を計算
        Vector3 cameraDelta = _mainCamera.transform.position - _initialCameraPosition;

        // パララックス効果を適用したオフセット
        Vector3 parallaxOffset = new Vector3(
            cameraDelta.x * parallax,
            cameraDelta.y * parallax * 0.1f, // Y軸は弱めのパララックス
            0f
        );

        // 各建物の位置を更新
        for (int i = 0; i < buildings.Length; i++) {
            if (buildings[i] == null || i >= initialPositions.Length) continue;

            // 初期位置にパララックスオフセットを適用
            Vector3 targetPosition = initialPositions[i] + parallaxOffset;

            // ループ処理のための範囲計算
            float totalWidth = buildingWidth * buildings.Length;
            float cameraHalfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;
            float visibleRange = cameraHalfWidth + buildingWidth;

            // ループ判定と位置調整
            float relativeX = targetPosition.x - (_mainCamera.transform.position.x * parallax);

            // 左側に出すぎた場合
            while (relativeX < -visibleRange) {
                targetPosition.x += totalWidth;
                relativeX += totalWidth;
            }

            // 右側に出すぎた場合
            while (relativeX > visibleRange + totalWidth) {
                targetPosition.x -= totalWidth;
                relativeX -= totalWidth;
            }

            buildings[i].transform.position = targetPosition;
        }
    }

    /// <summary>
    /// 初期位置をリセット（デバッグ用）
    /// </summary>
    [ContextMenu("Reset Initial Positions")]
    public void ResetInitialPositions() {
        if (_mainCamera != null) {
            _RecordInitialPositions();
        }
    }

    /// <summary>
    /// パララックス効果をリセット（デバッグ用）
    /// </summary>
    [ContextMenu("Reset Parallax")]
    public void ResetParallax() {
        if (_vistaInitialPositions != null) {
            for (int i = 0; i < _vistaBuildings.Length && i < _vistaInitialPositions.Length; i++) {
                if (_vistaBuildings[i] != null) {
                    _vistaBuildings[i].transform.position = _vistaInitialPositions[i];
                }
            }
        }

        if (_closeInitialPositions != null) {
            for (int i = 0; i < _closeBuildings.Length && i < _closeInitialPositions.Length; i++) {
                if (_closeBuildings[i] != null) {
                    _closeBuildings[i].transform.position = _closeInitialPositions[i];
                }
            }
        }

        if (_mainCamera != null) {
            _initialCameraPosition = _mainCamera.transform.position;
        }
    }
}