using System.Collections.Generic;
using UnityEngine;

// シーン内の草むら(BrushwoodHidingSpot)を自動収集し、いずれか1つをランダムに潜み状態にする。
// 逃げたら未使用の別の草むらへ移し、全て使い切ったらどこにも出現しなくなる
public class BrushwoodHidingManager : MonoBehaviour {
    [SerializeField] private BrushwoodHidingSpot _initialSpot; // 指定した場合、最初はこのスポットを潜み状態にする(未指定ならランダム)

    private List<BrushwoodHidingSpot> _unusedSpots;

    private void Awake() {
        var spots = FindObjectsByType<BrushwoodHidingSpot>(FindObjectsSortMode.None);
        _unusedSpots = new List<BrushwoodHidingSpot>(spots);
        foreach (var spot in spots) {
            spot.OnFled += _ShowNextSpot;
        }
    }

    private void Start() {
        if (_initialSpot != null && _unusedSpots.Remove(_initialSpot)) {
            _initialSpot.ShowHiding();
        } else {
            _ShowNextSpot();
        }
    }

    // 未使用の草むらからランダムに1つ選んで潜み状態にする。無くなったら何もしない
    private void _ShowNextSpot() {
        if (_unusedSpots.Count == 0) return;

        int index = Random.Range(0, _unusedSpots.Count);
        var spot = _unusedSpots[index];
        _unusedSpots.RemoveAt(index);

        spot.ShowHiding();
    }
}
