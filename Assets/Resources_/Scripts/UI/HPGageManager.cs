using System.Collections.Generic;
using UnityEngine;

public class HPGageManager : MonoBehaviour {
    /// <summary>
    /// 対象キャラクターのパラメーター
    /// </summary>
    [SerializeField] private CharacterParameter _characterParameter;

    [SerializeField] private HP_Heart _heartPrefab;
    private List<HP_Heart> _hearts = new List<HP_Heart>();
    [SerializeField] private Transform _heartParent;
    private bool isInitialized = false;

    private void Reset() {
        if (_heartParent == null) _heartParent = transform;
    }

    private void Start() {
        // 初期化
        if (_characterParameter != null) {
            // HP更新イベントに登録
            _characterParameter.OnHPChanged += UpdateHPGage;
            // 初期表示
            UpdateHPGage(_characterParameter.CurrentHP);

            _characterParameter.OnMaxHPChanged += UpdateMaxHP;
        }
    }

    /// <summary>
    /// HPゲージの初期化
    /// </summary>
    /// <param name="max_hp">最大HP</param>
    public void UpdateMaxHP(int max_hp) {
        var childCount = _heartParent.childCount;
        for (int i = 0; i < childCount; i++) {
            // 初期化のために既存の子オブジェクトを削除
            Destroy(_heartParent.GetChild(i).gameObject);
        }
        _hearts.Clear();

        // 最大HP分のハートを生成
        for (int i = 0; i < max_hp; i++) {
            var heart = Instantiate(_heartPrefab, _heartParent);
            if (isInitialized && i == max_hp - 1) {
                // 最後のハートのみスポーンアニメーションを再生
                heart.OnPlaySpawnAnim();
            }
            _hearts.Add(heart);
        }

        // 初期化フラグを設定
        if (!isInitialized) {
            isInitialized = true;
        }
    }

    /// <summary>
    /// HPゲージの更新
    /// </summary>
    /// <param name="current_hp">現在のHP</param>
    private void UpdateHPGage(int current_hp) {
        for (int i = 0; i < _hearts.Count; i++) {
            // 現在のHPに応じてハートを満タンにするかどうかを設定
            _hearts[i].SetFill(i < current_hp);
        }
    }
}
