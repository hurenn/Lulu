using UnityEngine;

/// <summary>
/// ルルが触れると弾き飛ばされる小物(小石・空き缶・紙くずなど)。
/// ルルとは物理的にぶつからない(移動を邪魔しない)ようにし、重なった時の速さに応じてスクリプトで弾く。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class KickableProp : MonoBehaviour {
    [Tooltip("この速さ以上で触れると弾かれる")]
    [SerializeField] private float _minSpeed = 1.5f;
    [Tooltip("ルルの横の速さに掛ける倍率")]
    [SerializeField] private float _kickScale = 0.35f;
    [Tooltip("上向きに弾く強さ(最小・最大)")]
    [SerializeField] private Vector2 _upImpulse = new Vector2(1.5f, 3f);
    [Tooltip("回転の強さ")]
    [SerializeField] private float _spin = 6f;
    [Tooltip("触れたと判定する範囲を広げる量(紙くずなど風で飛ぶ物は大きく)")]
    [SerializeField] private float _reach = 0.1f;
    [SerializeField] private AudioClip _kickSound;
    [Range(0f, 1f)] [SerializeField] private float _volume = 0.5f;

    private Rigidbody2D _rb;
    private Collider2D _col;
    private AudioSource _audio;
    private float _cooldown;
    private Character_Base _ignoredWith;
    private Renderer _renderer;
    private float _startY;

    // 置かれた位置からこれ以上落ちて画面外にあれば、穴に落ちたとみなして消す(落ち続けて物理演算に残らないように)
    private const float FALL_DESTROY_DISTANCE = 30f;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _audio = GetComponent<AudioSource>();
        _renderer = GetComponentInChildren<Renderer>();
        _startY = transform.position.y;
    }

    private void Update() {
        if (transform.position.y < _startY - FALL_DESTROY_DISTANCE && (_renderer == null || !_renderer.isVisible)) {
            Destroy(gameObject);
            return;
        }
        _cooldown -= Time.deltaTime;
        var lulu = PlayerCharacterManager.Current;
        if (lulu == null) return;
        if (_ignoredWith != lulu) {
            // ルルとはぶつからないようにする(操作キャラが替わったら付け直す)
            foreach (var c in lulu.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(_col, c, true);
            _ignoredWith = lulu;
        }
        if (_cooldown > 0f) return;
        Vector2 v = lulu.Velocity;
        if (v.magnitude < _minSpeed) return;
        var lb = new Bounds(lulu.transform.position, lulu.GetCharacterSize());
        var mb = _col.bounds; mb.Expand(_reach * 2f);
        if (!lb.Intersects(mb)) return;

        float dir = Mathf.Abs(v.x) > 0.1f ? Mathf.Sign(v.x) : Mathf.Sign(transform.position.x - lulu.transform.position.x);
        var impulse = new Vector2(dir * (Mathf.Abs(v.x) * _kickScale + 0.8f), Random.Range(_upImpulse.x, _upImpulse.y));
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(impulse * _rb.mass, ForceMode2D.Impulse);
        _rb.angularVelocity = -dir * _spin * Random.Range(40f, 90f);
        if (_kickSound != null && _audio != null) _audio.PlayOneShot(_kickSound, _volume);
        _cooldown = 0.3f;
    }
}
