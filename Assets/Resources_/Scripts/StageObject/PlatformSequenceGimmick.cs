using UnityEngine;

// 3つの足場に順番通り飛び乗ったらコインを出現させるギミック管理
public class PlatformSequenceGimmick : MonoBehaviour {
    private const int TOTAL_STEPS = 3;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seLand; // 次の足場に乗った時の音

    [SerializeField] private CoinSpawner _coinSpawner; // 成功時にコインを出す位置
    [SerializeField] private int _coinValue = 1;

    private int _nextIndex = 0;

    // 各足場(PlatformSequenceStep)から呼ばれる。stepIndexはその足場の順番(0始まり)
    public void OnPlatformLanded(int stepIndex) {
        if (stepIndex != _nextIndex) return; // 同じ足場・順番違いの足場では何も起こらない

        if (_seLand != null && _audioSource != null) {
            _audioSource.PlayOneShot(_seLand);
        }

        _nextIndex++;
        if (_nextIndex >= TOTAL_STEPS) {
            _coinSpawner?.SpawnCoin(_coinValue);
            _nextIndex = 0; // 次回また挑戦できるようにリセットする
        }
    }

    // 落下用の当たり判定(PlatformSequenceFailZone)から呼ばれる
    public void ResetSequence() {
        _nextIndex = 0;
    }
}
