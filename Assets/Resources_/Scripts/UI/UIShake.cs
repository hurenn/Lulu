using System.Collections;
using UnityEngine;

public class UIShake : MonoBehaviour {
    public RectTransform target;
    public float duration = 0.5f;
    public float strength = 10f;

    public void Shake() {
        StartCoroutine(ShakeCoroutine());
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
    }
}
