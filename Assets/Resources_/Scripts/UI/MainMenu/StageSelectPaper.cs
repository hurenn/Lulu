using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ステージセレクトで黒板に貼る紙(1枚=1ステージ)。
/// 選択中は少し大きくなって傾きがまっすぐになり、発光アウトラインで光る。
/// まだ遊べないステージ(シーンがビルド設定に無い)は灰色にする
/// </summary>
public class StageSelectPaper : MonoBehaviour {
    private const float SELECTED_SCALE = 1.15f;  // 選択中の拡大率
    private const float FOLLOW_SPEED = 18f;      // 拡大・傾きが目標に追いつく速さ
    private const float APPEAR_TIME = 0.25f;     // 出てくる時間(秒)
    private const float SHAKE_TIME = 0.3f;       // 選べない時に揺れる時間(秒)
    private const float SHAKE_ANGLE = 8f;        // 揺れの角度

    [SerializeField] private int _stageNumber = 1;
    [SerializeField] private string _sceneName;               // 開始するシーン(各ステージの最初のエリア)
    [SerializeField] private float _baseAngle;                // 貼ってある時の傾き(度)
    [SerializeField] private UIGlowOutline _glow;
    [SerializeField] private Graphic _image;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f); // 遊べないステージの色

    private bool _isSelected;
    private float _scale = 1f;
    private float _angle;
    private float _appearRate = 1f;   // 出てくる演出の拡大率
    private float _shakeAngle;
    private Coroutine _appearCoroutine;
    private Coroutine _shakeCoroutine;

    public int StageNumber => _stageNumber;
    public string SceneName => _sceneName;
    public bool IsAvailable => !string.IsNullOrEmpty(_sceneName) && Application.CanStreamedLevelBeLoaded(_sceneName);

    private void Awake() {
        _angle = _baseAngle;
        if (!IsAvailable) {
            if (_image != null) _image.color = _lockedColor;
            if (_label != null) _label.color *= _lockedColor;
        }
        _Apply();
    }

    private void OnDisable() {
        _appearCoroutine = null;
        _shakeCoroutine = null;
        _shakeAngle = 0f;
        _appearRate = 1f;
    }

    private void Update() {
        float rate = 1f - Mathf.Exp(-FOLLOW_SPEED * Time.unscaledDeltaTime);
        _scale = Mathf.Lerp(_scale, _isSelected ? SELECTED_SCALE : 1f, rate);
        _angle = Mathf.Lerp(_angle, _isSelected ? 0f : _baseAngle, rate);
        _Apply();
    }

    private void _Apply() {
        transform.localScale = Vector3.one * (_scale * _appearRate);
        transform.localRotation = Quaternion.Euler(0f, 0f, _angle + _shakeAngle);
    }

    /// <summary>
    /// 選択状態の切り替え
    /// </summary>
    public void SetSelected(bool selected, bool instant = false) {
        _isSelected = selected;
        if (_glow != null) _glow.SetHighlighted(selected, instant);
        if (instant) {
            _scale = selected ? SELECTED_SCALE : 1f;
            _angle = selected ? 0f : _baseAngle;
            _Apply();
        }
    }

    /// <summary>
    /// 紙がポンと貼られるように出てくる
    /// </summary>
    public void PlayAppear(float delay) {
        if (_appearCoroutine != null) StopCoroutine(_appearCoroutine);
        _appearRate = 0f;
        _Apply();
        _appearCoroutine = StartCoroutine(_AppearAnim(delay));
    }

    private IEnumerator _AppearAnim(float delay) {
        float t = -delay;
        while (t < APPEAR_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / APPEAR_TIME);
            // 少し大きくなりすぎてから戻る(イーズアウトバック)
            const float BACK = 1.7f;
            float x = rate - 1f;
            _appearRate = 1f + (BACK + 1f) * x * x * x + BACK * x * x;
            _Apply();
            yield return null;
        }
        _appearRate = 1f;
        _Apply();
        _appearCoroutine = null;
    }

    /// <summary>
    /// 選べない時に左右に揺れる
    /// </summary>
    public void Shake() {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(_ShakeAnim());
    }

    private IEnumerator _ShakeAnim() {
        float t = 0f;
        while (t < SHAKE_TIME) {
            t += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(t / SHAKE_TIME);
            _shakeAngle = Mathf.Sin(rate * Mathf.PI * 6f) * SHAKE_ANGLE * (1f - rate);
            yield return null;
        }
        _shakeAngle = 0f;
        _shakeCoroutine = null;
    }
}
