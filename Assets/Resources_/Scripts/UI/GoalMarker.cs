using UnityEngine;
using UnityEngine.UI;

public class GoalMarker : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform goal;
    [SerializeField] private RectTransform markerUI;
    [SerializeField] private Image markerImage;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float edgeOffset = 50f; // 画面端から少し内側に
    private bool _isActive = false;

    private void Reset() {
        player = GameObject.FindWithTag("Player")?.transform;
        goal = GameObject.FindAnyObjectByType<ChangeScene>()?.transform;
        markerUI = GetComponent<RectTransform>();
        markerImage = GetComponent<Image>();
        mainCamera = Camera.main;
    }

    private void Update() {
        if(player == null || goal == null || markerUI == null || mainCamera == null) {
            return;
        }

        // フェードイン・アウト処理
        var color = markerImage.color;
        if (_isActive && color.a < 1f) {
            color.a += Time.deltaTime * 3;
            markerImage.color = color;
        } else if (!_isActive && color.a > 0f) {
            color.a -= Time.deltaTime * 3;
            markerImage.color = color;
        }
        if (!_isActive) {
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(goal.position);

        // ゴールがカメラの前方にあるか？
        bool isBehind = screenPos.z < 0;

        // もしカメラの後ろなら、方向を反転（後方でもマーカー出す場合）
        if (isBehind) {
            screenPos *= -1;
        }

        // 画面中心からの方向ベクトルを求める
        Vector3 dir = (screenPos - new Vector3(Screen.width / 2, Screen.height / 2, 0)).normalized;

        // 画面端の位置を計算
        Vector3 edgePos = new Vector3(
            Mathf.Clamp(Screen.width / 2 + dir.x * (Screen.width / 2 - edgeOffset), edgeOffset, Screen.width - edgeOffset),
            Mathf.Clamp(Screen.height / 2 + dir.y * (Screen.height / 2 - edgeOffset), edgeOffset, Screen.height - edgeOffset),
            0
        );

        // マーカーの位置を更新
        markerUI.position = edgePos;

        // 向きを調整（矢印の先をゴール方向に）
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        markerUI.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 画面内にゴールがあるなら非表示
        bool isOnScreen =
            screenPos.x > 0 && screenPos.x < Screen.width &&
            screenPos.y > 0 && screenPos.y < Screen.height &&
            !isBehind;

        markerUI.gameObject.SetActive(!isOnScreen);
    }

    public void SetMarkerActive(bool isActive) {
        _isActive = isActive;
    }
}
