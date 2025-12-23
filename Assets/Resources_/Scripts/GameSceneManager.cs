using UnityEngine;
using UnityEngine.InputSystem;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    private string _titleSceneName = "Title";

    private void Update() {
        var keyboard = Keyboard.current;
        if (keyboard == null) {
            return;
        }

        // ESCキーでタイトルへ戻る
        if (keyboard.escapeKey.wasPressedThisFrame) {
            GameRestart();
        }

        // Rキーでステージ再スタート
        if (keyboard.rKey.wasPressedThisFrame) {
            StageRestart(false);
        }

        // デバッグ用：Pキーで経験値200獲得
        if (keyboard.pKey.wasPressedThisFrame) {
            var player = FindAnyObjectByType<Player_Character>();
            if (player != null) {
                player.AddExp(200);
            }
        }

        // デバッグ用：Numpad9キーで必殺チャージ
        if (keyboard.numpad9Key.wasPressedThisFrame) {
            Ability_Base[] abilities = FindObjectsByType<Ability_Base>(FindObjectsSortMode.None);
            foreach (var ability in abilities) {
                if (ability != null) {
                    ability.ForceCharge();
                }
            }
        }

        // デバッグ用：数字キーで能力の付与/解除
        if (keyboard.numpad4Key.wasPressedThisFrame) {
            var player = FindAnyObjectByType<Player_Character>();
            if (player != null) {
                var had_ability = PlayerParameter.Instance.Abilities.ContainsKey(eAbilityType.Ice);
                player.SetAbilitySlot(had_ability ? eAbilityType.None : eAbilityType.Ice, eAbilitySlot.Y);
            }
        }
        if (keyboard.numpad8Key.wasPressedThisFrame) {
            var player = FindAnyObjectByType<Player_Character>();
            if (player != null) {
                var had_ability = PlayerParameter.Instance.Abilities.ContainsKey(eAbilityType.Light);
                player.SetAbilitySlot(had_ability ? eAbilityType.None : eAbilityType.Light, eAbilitySlot.X);
            }
        }
        if (keyboard.numpad6Key.wasPressedThisFrame) {
            var player = FindAnyObjectByType<Player_Character>();
            if (player != null) {
                var had_ability = PlayerParameter.Instance.Abilities.ContainsKey(eAbilityType.Fire);
                player.SetAbilitySlot(had_ability ? eAbilityType.None : eAbilityType.Fire, eAbilitySlot.A);
            }
        }
        if (keyboard.numpad5Key.wasPressedThisFrame) {
            var player = FindAnyObjectByType<Player_Character>();
            if (player != null) {
                player.SaveAbilitySlot(); // 能力スロットセーブ
            }
        }

        // デバッグ用：Lキーで言語切り替え
        if (keyboard.lKey.wasPressedThisFrame) {
            var player_param = PlayerParameter.Instance;
            var new_language = player_param.language == PlayerParameter.eLanguage.Japanese ?
                PlayerParameter.eLanguage.English : PlayerParameter.eLanguage.Japanese;
            player_param.language = new_language;
        }
    }

    public void StageRestart(bool is_ability_save) {
        // シーン再読み込み
        ChangeScene.LoadScene(is_ability_save);
    }

    public void GameRestart() {
        ChangeScene.LoadScene(false, _titleSceneName);
    }
}
