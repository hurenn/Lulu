using UnityEngine;

public class Signal_LightTutorial : MonoBehaviour {
    public static Signal_LightTutorial Instance { get; private set; }
    public PlayerController playerController;
    private bool _isTutorialActive = false;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        if (playerController == null) {
            playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        }
    }

    private void Update() {
        if (_isTutorialActive) {
            // 画面スロー
            Time.timeScale = 0f;
        }
    }

    public void Tutorial_First() {
        // Xボタン入力待機
        _LightTutorial_Common();
    }

    private void _LightTutorial_Common() {
        if (playerController == null) {
            return;
        }
        // 画面スロー
        _isTutorialActive = true;

        CharacterInputData specific_input = new CharacterInputData();
        specific_input.abilityXPressed = true; // Xボタン入力を要求
        playerController.SetSpecificInput(specific_input, () => {
            // 入力完了後の処理
            _isTutorialActive = false;
            Time.timeScale = 1f; // 時間を元に戻す

            // プレイヤーコントローラーを有効化
            playerController.isEnabledCharacterInput = true;

            // Xボタン入力を実行
            playerController.OnAbilityX();
        });
    }
}
