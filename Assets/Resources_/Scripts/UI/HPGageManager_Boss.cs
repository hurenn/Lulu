using UnityEngine;
using UnityEngine.UI;

public class HPGageManager_Boss : MonoBehaviour {
    [SerializeField] private CharacterParameter _characterParameter;
    [SerializeField] private Slider _hpBar;
    private int max_hp => _characterParameter != null ? _characterParameter.MaxHP : 1;

    private void Start() {
        _Setup();
    }

    private void _Setup() {
        if (_characterParameter == null) {
            _characterParameter = GetComponentInParent<CharacterParameter>();
        }
        if (_characterParameter != null) {
            // HP更新イベントに登録
            _characterParameter.OnHPChanged += UpdateHPGage;
            // 初期表示
            UpdateHPGage(_characterParameter.CurrentHP);
        }
    }

    private void UpdateHPGage(int current_hp) {
        if (_hpBar == null) {
            return;
        }

        _hpBar.value = (float)current_hp / max_hp;
    }
}
