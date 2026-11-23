using UnityEngine;

/// <summary>
/// 床の水たまり。ルルが踏んだり着地したりすると、水しぶきと波紋を出す。
/// </summary>
public class Puddle : MonoBehaviour {
    [Tooltip("水しぶき(使い終わったら自分で消えるプレハブ)")]
    [SerializeField] private GameObject _splashPrefab;
    [Tooltip("水たまりの幅(ワールド単位)")]
    [SerializeField] private float _width = 1.6f;
    [Tooltip("歩いて踏んだ時に、しぶきを出す間隔(秒)")]
    [SerializeField] private float _stepInterval = 0.28f;
    [SerializeField] private AudioClip _splashSound;
    [Range(0f, 1f)] [SerializeField] private float _volume = 0.35f;

    private AudioSource _audio;
    private float _timer;
    private float _lastVy;
    private bool _wasIn;

    private void Awake() { _audio = GetComponent<AudioSource>(); }

    private void Update() {
        _timer -= Time.deltaTime;
        var lulu = PlayerCharacterManager.Current;
        if (lulu == null) return;
        var feet = lulu.transform.position + Vector3.down * (lulu.GetCharacterSize().y * 0.5f);
        bool inside = Mathf.Abs(feet.x - transform.position.x) < _width * 0.5f && Mathf.Abs(feet.y - transform.position.y) < 0.35f;
        Vector2 v = lulu.Velocity;
        // 着地(落ちてきて水たまりに入った)は大きく、歩き・走りは一定間隔で
        bool landed = inside && !_wasIn && _lastVy < -3f;
        bool stepping = inside && Mathf.Abs(v.x) > 1f && _timer <= 0f;
        if (landed || stepping) {
            var s = Instantiate(_splashPrefab, new Vector3(feet.x, transform.position.y, transform.position.z), Quaternion.identity);
            if (landed) s.transform.localScale = Vector3.one * 1.5f;
            if (_splashSound != null && _audio != null) _audio.PlayOneShot(_splashSound, landed ? _volume * 1.5f : _volume);
            _timer = _stepInterval;
        }
        _wasIn = inside;
        _lastVy = v.y;
    }
}
