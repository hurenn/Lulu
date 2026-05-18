using UnityEngine;

[System.Serializable]
public class ParallaxLayer {
    public string name;
    public Transform[] objects;    // ���[�v������I�u�W�F�N�g�i��O�����Ȃǁj
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;  // 0=���i�A1=��O
    public bool loop = false;      // ���[�v���邩
}

public class BackGround : MonoBehaviour {
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private ParallaxLayer[] layers;
    [Tooltip("自動スクロール速度（単位/秒）。0で無効")]
    [SerializeField] private float autoScrollSpeed = 0f;

    private Vector3 lastCamPos;
    private float[] spriteWidths;

    void Start() {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        lastCamPos = cameraTransform.position;

        // �e���C���[�̃I�u�W�F�N�g�����擾
        spriteWidths = new float[layers.Length];
        for (int i = 0; i < layers.Length; i++) {
            var layer = layers[i];
            if (layer.loop && layer.objects.Length >= 2) {
                spriteWidths[i] = layer.objects[0].GetComponent<SpriteRenderer>().bounds.size.x;
                // ���ɕ��ׂ�
                for (int j = 0; j < layer.objects.Length; j++) {
                    var pos = layer.objects[j].localPosition;
                    pos.x = j * spriteWidths[i] * layer.objects[j].localScale.x;
                    layer.objects[j].localPosition = pos;
                }
            }
        }
    }

    void LateUpdate() {
        float deltaX = cameraTransform.position.x - lastCamPos.x;

        for (int i = 0; i < layers.Length; i++) {
            var layer = layers[i];

            foreach (var obj in layer.objects) {
                // ���Ε����p�����b�N�X
                obj.position += Vector3.right * (-deltaX * layer.parallaxFactor - autoScrollSpeed * layer.parallaxFactor * Time.deltaTime);
            }

            // ���[�v����
            if (layer.loop) {
                float width = spriteWidths[i];
                foreach (var obj in layer.objects) {
                    float diff = cameraTransform.position.x - obj.position.x;
                    if (diff > width) {
                        obj.position += Vector3.right * width * layer.objects.Length;
                    } else if (diff < -width) {
                        obj.position -= Vector3.right * width * layer.objects.Length;
                    }
                }
            }
        }

        lastCamPos = cameraTransform.position;
    }
}
    //[SerializeField] private GameObject[] _vistaBuildings;
    //[SerializeField] private GameObject[] _closeBuildings;
    //[SerializeField] private Camera _mainCamera;
    //[SerializeField, Range(0f, 1f)] private float _vistaBuildingParallax = 0.5f;
    //[SerializeField, Range(0f, 1f)] private float _closeBuildingParallax = 0.8f;

    //// �w�i�̕��i���[�v�p�j
    //[SerializeField] private float _vistaBuildingWidth = 1.0f;
    //[SerializeField] private float _closeBuildingWidth = 1.0f;

    //// �����ʒu���L�^
    //private Vector3[] _vistaInitialPositions;
    //private Vector3[] _closeInitialPositions;
    //private Vector3 _initialCameraPosition;

    //private void Reset() {
    //    _mainCamera = Camera.main;

    //    // "VistaBuilding"�𖼑O�Ɋ܂ގq�I�u�W�F�N�g�����ׂĎ擾
    //    _vistaBuildings = transform.GetComponentsInChildren<Transform>()
    //        .Where(t => t.gameObject.name.Contains("VistaBuilding"))
    //        .Select(t => t.gameObject)
    //        .ToArray() ?? new GameObject[0];
    //    // "CloseBuilding"�𖼑O�Ɋ܂ގq�I�u�W�F�N�g�����ׂĎ擾
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
    ///// �����ʒu���L�^
    ///// </summary>
    //private void _RecordInitialPositions() {
    //    _initialCameraPosition = _mainCamera.transform.position;

    //    // Vista�����̏����ʒu���L�^
    //    _vistaInitialPositions = new Vector3[_vistaBuildings.Length];
    //    for (int i = 0; i < _vistaBuildings.Length; i++) {
    //        if (_vistaBuildings[i] != null) {
    //            _vistaInitialPositions[i] = _vistaBuildings[i].transform.position;
    //        }
    //    }

    //    // Close�����̏����ʒu���L�^
    //    _closeInitialPositions = new Vector3[_closeBuildings.Length];
    //    for (int i = 0; i < _closeBuildings.Length; i++) {
    //        if (_closeBuildings[i] != null) {
    //            _closeInitialPositions[i] = _closeBuildings[i].transform.position;
    //        }
    //    }
    //}

    ///// <summary>
    ///// ���[�J���ʒu�Ńp�����b�N�X���ʂ�����
    ///// </summary>
    //private void _LoopBuildings(GameObject[] buildings, Vector3[] initialPositions, float buildingWidth, float parallax) {
    //    if (buildings == null || buildings.Length == 0 || initialPositions == null) {
    //        return;
    //    }

    //    // �J�����̈ړ��ʂ��v�Z
    //    Vector3 cameraDelta = _mainCamera.transform.position - _initialCameraPosition;

    //    // �p�����b�N�X���ʂ�K�p�����I�t�Z�b�g
    //    Vector3 parallaxOffset = new Vector3(
    //        cameraDelta.x * parallax,
    //        cameraDelta.y * parallax * 0.1f, // Y���͎�߂̃p�����b�N�X
    //        0f
    //    );

    //    // �e�����̈ʒu���X�V
    //    for (int i = 0; i < buildings.Length; i++) {
    //        if (buildings[i] == null || i >= initialPositions.Length) continue;

    //        // �����ʒu�Ƀp�����b�N�X�I�t�Z�b�g��K�p
    //        Vector3 targetPosition = initialPositions[i] + parallaxOffset;

    //        // ���[�v�����̂��߂͈̔͌v�Z
    //        float totalWidth = buildingWidth * buildings.Length;
    //        float cameraHalfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;
    //        float visibleRange = cameraHalfWidth + buildingWidth;

    //        // ���[�v����ƈʒu����
    //        float relativeX = targetPosition.x - (_mainCamera.transform.position.x * parallax);

    //        // �����ɏo�������ꍇ
    //        while (relativeX < -visibleRange) {
    //            targetPosition.x += totalWidth;
    //            relativeX += totalWidth;
    //        }

    //        // �E���ɏo�������ꍇ
    //        while (relativeX > visibleRange + totalWidth) {
    //            targetPosition.x -= totalWidth;
    //            relativeX -= totalWidth;
    //        }

    //        buildings[i].transform.position = targetPosition;
    //    }
    //}

    ///// <summary>
    ///// �����ʒu�����Z�b�g�i�f�o�b�O�p�j
    ///// </summary>
    //[ContextMenu("Reset Initial Positions")]
    //public void ResetInitialPositions() {
    //    if (_mainCamera != null) {
    //        _RecordInitialPositions();
    //    }
    //}

    ///// <summary>
    ///// �p�����b�N�X���ʂ����Z�b�g�i�f�o�b�O�p�j
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