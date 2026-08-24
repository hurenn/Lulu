using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// ヒットエフェクト
/// </summary>
public class HitEffect : MonoBehaviour {
    public enum eType {
        Normal,
        Heavy,
        Fire,
        Ice
    }
    // エフェクトの色定義
    [SerializeField]
    private Dictionary<eType, Color> _typeColor =
        new Dictionary<eType, Color>() {
        { eType.Normal, Color.white },
        { eType.Heavy, Color.yellow },
        { eType.Fire, Color.red },
        { eType.Ice, Color.cyan }
    };

    // パーティクル
    [SerializeField] private ParticleSystem _particle = null;
    private float _defaultSize = 0f;

    public void Setup(eType type, float size_rate = 1.0f) {
        if (_particle == null) {
            _particle = GetComponent<ParticleSystem>();
        }
        if(_defaultSize == 0f) {
            _defaultSize = _particle.main.startSize.constant;
        }

        var main = _particle.main;
        if (_typeColor.ContainsKey(type)) {
            main.startColor = new ParticleSystem.MinMaxGradient(_typeColor[type], Color.white);
        } else {
            main.startColor = Color.white;
        }

        main.startSize = new ParticleSystem.MinMaxCurve(_defaultSize * size_rate);
    }
}