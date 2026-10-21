using System.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class AutoDestroy : MonoBehaviour {
    [SerializeField] protected float _lifetime = 0f;
    [SerializeField] private Animator _animator;

    // 消滅時のコールバック
    private System.Action _destroyedCallback = null;
    private bool _isDestroying = false; // Destroyはフレーム末まで遅れるため、同じフレームに二重に呼ばれても効果音を重ねて引き継がないようにする
    public void SetCallback(System.Action callback) {
        _destroyedCallback = callback;
    }

    protected virtual void Start() {
        StartCoroutine(_DestroyCoroutine());
    }

    /// <summary>
    /// 自動削除までのコルーチン
    /// </summary>
    private IEnumerator _DestroyCoroutine() {
        float current_time = 0;
        float add_time = 0;
        if (_animator != null) {
            add_time = _animator.GetCurrentAnimatorStateInfo(0).length;
        }

        while(current_time < _lifetime + add_time) {
            current_time += Time.deltaTime;
            yield return null;
        }

        DestroyNow();
    }

    /// <summary>
    /// 寿命を待たずに今すぐ消す(鳴っている効果音は最後まで鳴らす)
    /// </summary>
    public void DestroyNow() {
        if (_isDestroying) return;
        _isDestroying = true;
        _HandOverPlayingSounds();
        Destroy(gameObject);
    }

    /// <summary>
    /// 鳴っている途中の効果音を別のオブジェクトへ引き継ぎ、本体が消えても最後まで鳴らす
    /// (エフェクトより効果音の方が長い時に、途中で途切れないようにする)
    /// </summary>
    private void _HandOverPlayingSounds() {
        foreach (var source in GetComponentsInChildren<AudioSource>()) {
            if (!source.isPlaying || source.loop || source.clip == null) continue;

            var go = new GameObject(source.clip.name + "(HandOver)");
            go.transform.position = source.transform.position;
            var copy = go.AddComponent<AudioSource>();
            copy.clip = source.clip;
            copy.outputAudioMixerGroup = source.outputAudioMixerGroup;
            copy.volume = source.volume;
            copy.pitch = source.pitch;
            copy.panStereo = source.panStereo;
            copy.spatialBlend = source.spatialBlend;
            copy.priority = source.priority;
            copy.dopplerLevel = source.dopplerLevel;
            copy.spread = source.spread;
            copy.rolloffMode = source.rolloffMode;
            copy.minDistance = source.minDistance;
            copy.maxDistance = source.maxDistance;
            if (source.rolloffMode == AudioRolloffMode.Custom) {
                copy.SetCustomCurve(AudioSourceCurveType.CustomRolloff, source.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            }
            copy.reverbZoneMix = source.reverbZoneMix;
            copy.timeSamples = source.timeSamples; // 鳴っている途中の位置から続ける
            copy.Play();

            float remain = (source.clip.length - source.time) / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
            Destroy(go, remain + 0.1f);
        }
    }

    private void OnDestroy() {
        if (_destroyedCallback != null) {
            _destroyedCallback.Invoke();
        }
    }
}