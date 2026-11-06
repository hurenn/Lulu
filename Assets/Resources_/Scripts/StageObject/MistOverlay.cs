using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面全体を覆うUIの霧(Lulu/UIMist)に、カメラの位置と映している高さを渡す。
/// 霧の模様をワールド座標で作れるので、画面にくっつかずに景色と一緒に流れて見える。
/// </summary>
[RequireComponent(typeof(Graphic))]
public class MistOverlay : MonoBehaviour {
    [SerializeField] private Material _mistMaterial; // Lulu/UIMistの材質
    [Tooltip("霧がカメラの動きにどれだけ付いていくか。1=景色と同じ、1より大きいと手前にあるように見える")]
    [SerializeField] private float _parallax = 1.2f;

    private static readonly int _CAMERA_POS_ID = Shader.PropertyToID("_CameraPos");
    private static readonly int _VIEW_HEIGHT_ID = Shader.PropertyToID("_ViewHeight");

    private Material _material;

    private void Awake() {
        if (_mistMaterial == null) return;
        // 材質アセットを書き換えないよう複製して使う
        _material = new Material(_mistMaterial);
        GetComponent<Graphic>().material = _material;
    }

    private void OnDestroy() {
        if (_material != null) Destroy(_material);
    }

    private void LateUpdate() {
        if (_material == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        _material.SetVector(_CAMERA_POS_ID, cam.transform.position * _parallax);
        _material.SetFloat(_VIEW_HEIGHT_ID, cam.orthographicSize * 2f);
    }
}
