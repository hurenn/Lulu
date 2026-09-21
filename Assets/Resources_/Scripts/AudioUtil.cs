using System.Collections.Generic;
using UnityEngine;

// AudioSource.volumeはUnity側で常に0~1にクランプされてしまう(PlayClipAtPoint経由でも直接設定でも同じ)ため、
// 収録音量が小さいクリップを増幅したい場合はクリップの波形データ自体を増幅する
public static class AudioUtil {
    private static readonly Dictionary<string, AudioClip> _amplifiedCache = new Dictionary<string, AudioClip>();

    public static void PlayClipAtPointUnclamped(AudioClip clip, Vector3 position, float gain) {
        if (clip == null) return;

        var playClip = gain > 1f ? _GetAmplifiedClip(clip, gain) : clip;

        var go = new GameObject("OneShotAudio(Unclamped)");
        go.transform.position = position;

        var source = go.AddComponent<AudioSource>();
        source.clip = playClip;
        source.volume = 1f;
        source.spatialBlend = 0f;
        source.Play();

        Object.Destroy(go, playClip.length);
    }

    // 波形サンプルをgain倍して-1~1にクランプしたクリップを作る(音割れするが大きく聞こえる)
    private static AudioClip _GetAmplifiedClip(AudioClip source, float gain) {
        string key = source.GetInstanceID() + "_" + gain;
        if (_amplifiedCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var samples = new float[source.samples * source.channels];
        source.GetData(samples, 0);
        for (int i = 0; i < samples.Length; i++) {
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
        }

        var amplified = AudioClip.Create(source.name + "_amp", source.samples, source.channels, source.frequency, false);
        amplified.SetData(samples, 0);

        _amplifiedCache[key] = amplified;
        return amplified;
    }
}
