using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 床にとまっている鳥の群れ。ときどき地面をついばみ、ルルが近づくと一斉に飛び立つ。
/// ルルが離れてしばらくすると戻ってくる。
/// </summary>
public class PerchedBirds : MonoBehaviour {
    [SerializeField] private Sprite _idleSprite;
    [SerializeField] private Sprite _peckSprite;
    [Tooltip("飛ぶ時の羽ばたきのコマ")]
    [SerializeField] private Sprite[] _flyFrames = new Sprite[0];
    [SerializeField] private Color _color = new Color(0.22f, 0.24f, 0.28f, 1f);
    [SerializeField] private Vector2Int _count = new Vector2Int(2, 4);
    [Tooltip("ルルがこの距離まで近づくと飛び立つ")]
    [SerializeField] private float _fleeDistance = 4f;
    [Tooltip("飛び立ってから戻るまでの時間(ルルが離れている時)")]
    [SerializeField] private float _returnTime = 20f;
    [SerializeField] private string _sortingLayer = "Ground";
    [SerializeField] private int _sortingOrder = 6;

    private class Bird { public SpriteRenderer sr; public Vector3 home; public Vector3 vel; public float timer; public float flap; }
    private readonly List<Bird> _birds = new List<Bird>();
    private bool _flying;
    private float _awayTimer;

    private void Start() {
        int n = Random.Range(_count.x, _count.y + 1);
        for (int i = 0; i < n; i++) {
            var go = new GameObject("Bird");
            go.transform.SetParent(transform, false);
            var home = new Vector3(i * 0.55f - (n - 1) * 0.275f + Random.Range(-0.1f, 0.1f), 0f, 0f);
            go.transform.localPosition = home;
            go.transform.localScale = new Vector3(Random.value < 0.5f ? 1f : -1f, 1f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _idleSprite; sr.color = _color; sr.sortingLayerName = _sortingLayer; sr.sortingOrder = _sortingOrder;
            _birds.Add(new Bird { sr = sr, home = home, timer = Random.Range(0.5f, 3f) });
        }
    }

    private void Update() {
        var lulu = PlayerCharacterManager.Current;
        float dist = lulu != null ? Vector2.Distance(lulu.transform.position, transform.position) : 999f;
        if (!_flying) {
            if (dist < _fleeDistance) _TakeOff(lulu.transform.position);
            else foreach (var b in _birds) _Idle(b);
        } else {
            foreach (var b in _birds) _Fly(b);
            // ルルが離れていれば、しばらくして戻る
            _awayTimer = dist > _fleeDistance * 2.5f ? _awayTimer + Time.deltaTime : 0f;
            // 巣が画面に映っている間は戻さない(画面内に急に現れないように)
            if (_awayTimer > _returnTime && !_IsHomeInView()) _Land();
        }
    }

    private const float VIEW_MARGIN = 2f; // 画面に映っているとみなす範囲の余白

    private bool _IsHomeInView() {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return false;
        Vector2 offset = transform.position - cam.transform.position;
        float half_height = cam.orthographicSize + VIEW_MARGIN;
        float half_width = cam.orthographicSize * cam.aspect + VIEW_MARGIN;
        return Mathf.Abs(offset.x) < half_width && Mathf.Abs(offset.y) < half_height;
    }

    private void _Idle(Bird b) {
        b.timer -= Time.deltaTime;
        if (b.timer > 0f) return;
        // ついばむ(一瞬だけ頭を下げる)/向きを変える
        bool peck = b.sr.sprite == _idleSprite && Random.value < 0.7f;
        b.sr.sprite = peck ? _peckSprite : _idleSprite;
        if (!peck && Random.value < 0.3f) { var s = b.sr.transform.localScale; s.x *= -1f; b.sr.transform.localScale = s; }
        b.timer = peck ? Random.Range(0.15f, 0.3f) : Random.Range(0.6f, 2.5f);
    }

    private void _TakeOff(Vector3 from) {
        _flying = true; _awayTimer = 0f;
        foreach (var b in _birds) {
            float dir = Mathf.Sign(transform.position.x - from.x); if (dir == 0f) dir = 1f;
            b.vel = new Vector3(dir * Random.Range(3f, 5f), Random.Range(4f, 6f), 0f);
            var s = b.sr.transform.localScale; s.x = dir; b.sr.transform.localScale = s;
            b.flap = Random.value * 2f;
        }
    }

    private void _Fly(Bird b) {
        if (!b.sr.enabled) return;
        b.sr.transform.localPosition += b.vel * Time.deltaTime;
        b.vel.y += 1.5f * Time.deltaTime; // 上へ加速して遠ざかる
        b.flap += Time.deltaTime * 12f;
        if (_flyFrames.Length > 0) b.sr.sprite = _flyFrames[(int)b.flap % _flyFrames.Length];
        if (b.sr.transform.localPosition.y > 25f) b.sr.enabled = false;
    }

    private void _Land() {
        _flying = false;
        foreach (var b in _birds) { b.sr.enabled = true; b.sr.transform.localPosition = b.home; b.sr.sprite = _idleSprite; b.timer = Random.Range(0.5f, 2f); }
    }
}
