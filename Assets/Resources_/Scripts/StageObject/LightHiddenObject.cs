using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// LightAbility使用中(isLightInvincible/isAutoLightInvincible)だけ、イージング付きフェードで
/// 非表示になり当たり判定も外れるギミック。
/// </summary>
public class LightHiddenObject : MonoBehaviour {
    [SerializeField] private Renderer _renderer; // SpriteRenderer/TilemapRendererどちらも可
    [SerializeField] private Collider2D _collider;
    [SerializeField] private float _fadeDuration = 0.3f; // 消える/現れるアニメーションの時間(秒)

    private Tilemap _tilemap;
    private float _currentAlpha = 1f;
    private float _fadeStartAlpha = 1f;
    private float _fadeElapsed;
    private bool _targetVisible = true;

    private void Reset() {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider2D>();
    }

    private void Awake() {
        if (_renderer != null) _tilemap = _renderer.GetComponent<Tilemap>();
    }

    private void Update() {
        var player = PlayerCharacterManager.Current as Player_Character;
        bool isUsingLight = player != null &&
            (player.PlayerCharaParam.isLightInvincible || player.PlayerCharaParam.isAutoLightInvincible);
        bool visible = !isUsingLight;

        if (visible != _targetVisible) {
            _targetVisible = visible;
            _fadeStartAlpha = _currentAlpha;
            _fadeElapsed = 0f;
        }

        float targetAlpha = _targetVisible ? 1f : 0f;
        if (_fadeDuration <= 0f) {
            _currentAlpha = targetAlpha;
        } else {
            _fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            _currentAlpha = Mathf.Lerp(_fadeStartAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
        }
        _ApplyAlpha(_currentAlpha);

        if (_collider != null) _collider.enabled = visible;
    }

    private void _ApplyAlpha(float alpha) {
        if (_renderer == null) return;
        if (_tilemap != null) {
            var c = _tilemap.color;
            c.a = alpha;
            _tilemap.color = c;
        } else if (_renderer is SpriteRenderer sr) {
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
        } else {
            _renderer.enabled = alpha > 0.5f;
        }
    }
}
