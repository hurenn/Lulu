using System.Collections;
using UnityEngine;

public class LocalTimePause : MonoBehaviour
{
    public bool IsPaused { get; private set; } = false;
    private float _pauseTimer = 0f;
    [SerializeField] private float _hitStopWait = 0.05f;

    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private ParticleSystem[] _particleSystems;

    private void Reset() {
        _animator = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    private void Update() {
        if (IsPaused) {
            _pauseTimer -= Time.unscaledDeltaTime;
            if (_pauseTimer <= 0f) {
                Resume();
            }
        }
    }

    public IEnumerator Pause(float duration) {
        if (IsPaused) yield break;
        yield return new WaitForSeconds(_hitStopWait); // 一瞬待ってから停止

        IsPaused = true;
        _pauseTimer = duration;

        // アニメーションと物理挙動を停止
        if (_animator != null) _animator.speed = 0f;
        if (_rigidbody2D != null) _rigidbody2D.simulated = false;
        // パーティクルシステムの一時停止
        if (_particleSystems != null) {
            foreach (var ps in _particleSystems) {
                ps.Pause();
            }
        }
    }

    public void Resume() {
        if (!IsPaused) return;
        IsPaused = false;

        // アニメーションと物理挙動を再開
        if (_animator != null) _animator.speed = 1f;
        if (_rigidbody2D != null) _rigidbody2D.simulated = true;
        // パーティクルシステムの再開
        if (_particleSystems != null) {
            foreach (var ps in _particleSystems) {
                ps.Play();
            }
        }
    }
}
