using System.Collections;
using UnityEngine;

public class UIShake : MonoBehaviour {
    public RectTransform target;
    public float duration = 0.5f;
    public float strength = 10f;

    // 実行中のシェイクコルーチン（連続呼び出しでbasePosがズレるのを防ぐ）
    private Coroutine _shakeCoroutine;

    public void Shake() {
        if (_shakeCoroutine != null) {
            StopCoroutine(_shakeCoroutine);
        }
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine() {
        float time = 0;
        Vector2 basePos = target.anchoredPosition;

        while (time < duration) {
            time += Time.deltaTime;
            target.anchoredPosition = basePos + Random.insideUnitCircle * strength;
            yield return null;
        }

        target.anchoredPosition = basePos;
        _shakeCoroutine = null;
    }
}
