using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSceneManager : PersistentSingleton<GameSceneManager>
{
    private string _titleSceneName = "Title";

    // リトライ種別を記憶
    private static bool _isDeathRetry = false;

    protected override void OnSingletonAwake() {
        // DontDestroyOnLoadで永続化されるため、Start()は生存期間中に一度しか呼ばれない。
        // シーン再読み込みのたびに復帰処理を行うため、sceneLoadedイベントを使用する
        SceneManager.sceneLoaded += _OnSceneLoaded;
    }

    protected override void OnSingletonDestroy() {
        SceneManager.sceneLoaded -= _OnSceneLoaded;
    }

    private void _OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // デバッグ用：AボタンのFire/Water入れ替えは、シーン再読み込みのたびに解除する
        // （PlayerParameterは永続化されるため、入れ替えたままだと次回シーンでもNumpad6がFireに戻らない）
        _ResetWaterDebugSlot();

        // シーン再読み込み時のみリスポーン地点に移動
        var player = PlayerCharacterManager.Current as Player_Character;
        var checkpointManager = CheckpointManager.Instance;
        if (player != null && checkpointManager != null && checkpointManager.ShouldRespawnInCurrentScene()) {
            checkpointManager.RespawnPlayer(player);
        }
        // 死亡によるリトライの場合のみ必殺チャージを50%に設定
        if (_isDeathRetry) {
            StartCoroutine(_ChargeAbilitiesOnRespawn(0.5f));
            _isDeathRetry = false; // フラグをリセット
        }
    }

    /// <summary>
    /// デバッグ用：Numpad3でAボタンをWaterに入れ替えたままの場合、Fireへ戻す
    /// （JoinAbilityで正式に取得したWaterは解除しない）
    /// </summary>
    private void _ResetWaterDebugSlot() {
        var player_param = PlayerParameter.Instance;
        if (!player_param.isWaterDebugEquipped) {
            return;
        }
        player_param.isWaterDebugEquipped = false;
        if (player_param.GetAssignedAbilityType(eAbilitySlot.A) == eAbilityType.Water) {
            player_param.SetAssignedSlot(eAbilityType.Water, eAbilitySlot.None);
            player_param.SetAssignedSlot(eAbilityType.Fire, eAbilitySlot.A);
        }
    }

    /// <summary>
    /// リスポーン時にすべてのアビリティに必殺チャージを付与
    /// </summary>
    /// <param name="chargeRate">チャージ率（0.0～1.0）</param>
    private IEnumerator _ChargeAbilitiesOnRespawn(float chargeRate) {
        Ability_Base[] abilities = new Ability_Base[0];
        while (abilities.Length == 0) {
            abilities = FindObjectsByType<Ability_Base>(FindObjectsSortMode.None);
            yield return null;
        }
        foreach (var ability in abilities) {
            if (ability != null) {
                ability.ForceCharge(chargeRate);
            }
        }
    }

    private void Update() {
        var buildConfig = BuildConfig.Instance;

        // ExhibitionまたはTestビルドの場合のみデバッグ用のキー入力を有効化
        var enableDebugInput = buildConfig != null &&
            (buildConfig.BuildType == BuildConfig.eBuildType.Exhibition || buildConfig.BuildType == BuildConfig.eBuildType.Test);
        if (enableDebugInput) {
            _DebugKeyInput();
        }
    }

    /// <summary>
    /// デバッグ用のキー入力処理
    /// </summary>
    private void _DebugKeyInput() {
        var keyboard = Keyboard.current;
        if (keyboard == null) {
            return;
        }

        // Tキーでタイトルへ戻る
        if (keyboard.tKey.wasPressedThisFrame) {
            GameRestart();
        }

        // Rキーでステージ再スタート
        if (keyboard.rKey.wasPressedThisFrame) {
            StageRestart(false, false); // ポーズからのリトライ扱い（チャージなし）
        }

        // デバッグ用：Eキーで経験値200獲得
        if (keyboard.eKey.wasPressedThisFrame) {
            var player = PlayerCharacterManager.Current as Player_Character;
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

        // デバッグ用：数字キーでボタンごとに割り当てられている能力の付与/解除
        if (keyboard.numpad4Key.wasPressedThisFrame) {
            _ToggleAbilityBySlot(eAbilitySlot.Y);
        }
        if (keyboard.numpad8Key.wasPressedThisFrame) {
            _ToggleAbilityBySlot(eAbilitySlot.X);
        }
        if (keyboard.numpad6Key.wasPressedThisFrame) {
            _ToggleAbilityBySlot(eAbilitySlot.A);
        }
        if (keyboard.numpad2Key.wasPressedThisFrame) {
            _ToggleAbilityBySlot(eAbilitySlot.B);
        }

        // デバッグ用：Numpad3キーでWaterをAボタンに付与/解除（Water検証用）
        if (keyboard.numpad3Key.wasPressedThisFrame) {
            _ToggleWaterOnSlotA();
        }

        if (keyboard.numpad5Key.wasPressedThisFrame) {
            var player = PlayerCharacterManager.Current as Player_Character;
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

    /// <summary>
    /// 指定スロットに現在割り当てられている能力を付与/解除する（デバッグ用）
    /// 「既に付与されている」は実際にそのスロットへ装備中かどうかで判定する
    /// （所有フラグはNumpad5で保存するまで立たないため、保存前でも正しくトグルできるようにする）
    /// </summary>
    private void _ToggleAbilityBySlot(eAbilitySlot slot) {
        var player = PlayerCharacterManager.Current as Player_Character;
        if (player == null) {
            return;
        }

        var player_param = PlayerParameter.Instance;
        var ability_type = player_param.GetAssignedAbilityType(slot);
        if (ability_type == eAbilityType.None) {
            return;
        }

        bool is_equipped = player.GetCurrentSlot(ability_type) == slot;
        if (is_equipped) {
            player.RemoveAbility(slot); // ボタンから外し、シーン上のAbilityも削除する
        } else {
            player.SetAbilitySlot(ability_type, slot);
        }
    }

    /// <summary>
    /// 指定スロットに指定した能力を強制的に配置する（デバッグ用：元々そのスロットにあった能力は外される）
    /// </summary>
    private void _PlaceAbilityOnSlot(eAbilityType ability_type, eAbilitySlot slot) {
        var player = PlayerCharacterManager.Current as Player_Character;
        if (player == null) {
            return;
        }

        var player_param = PlayerParameter.Instance;
        var current_type = player_param.GetAssignedAbilityType(slot);
        if (current_type == ability_type) {
            return; // 既に配置済み
        }

        if (current_type != eAbilityType.None) {
            if (player.GetCurrentSlot(current_type) == slot) {
                player.RemoveAbility(slot); // 元々装備されていた能力をボタンから外し、シーン上のAbilityも削除する
            }
            player_param.SetAssignedSlot(current_type, eAbilitySlot.None);
        }
        player_param.SetAssignedSlot(ability_type, slot);
        player.SetAbilitySlot(ability_type, slot);
    }

    /// <summary>
    /// デバッグ用：AボタンのWaterを付与/解除する（解除時はAボタンの割り当てをFireに戻す）
    /// </summary>
    private void _ToggleWaterOnSlotA() {
        var player = PlayerCharacterManager.Current as Player_Character;
        if (player == null) {
            return;
        }

        bool is_water_equipped = player.GetCurrentSlot(eAbilityType.Water) == eAbilitySlot.A;
        if (is_water_equipped) {
            player.RemoveAbility(eAbilitySlot.A); // ボタンから外し、シーン上のAbilityも削除する
            var player_param = PlayerParameter.Instance;
            player_param.SetAssignedSlot(eAbilityType.Water, eAbilitySlot.None);
            player_param.SetAssignedSlot(eAbilityType.Fire, eAbilitySlot.A);
            player_param.isWaterDebugEquipped = false;
        } else {
            _PlaceAbilityOnSlot(eAbilityType.Water, eAbilitySlot.A);
            PlayerParameter.Instance.isWaterDebugEquipped = true;
        }
    }

    /// <summary>
    /// ステージリトライ
    /// </summary>
    /// <param name="is_ability_save">能力を保存するか</param>
    /// <param name="is_death_retry">死亡によるリトライか（trueの場合必殺チャージ50%で復帰）</param>
    public void StageRestart(bool is_ability_save, bool is_death_retry = false) {
        // 死亡リトライフラグを設定
        _isDeathRetry = is_death_retry;
        
        // シーン再読み込み
        ChangeScene.LoadScene(is_ability_save);
    }

    public void GameRestart() {
        _isDeathRetry = false; // タイトルに戻る場合はフラグをクリア
        CheckpointManager.Instance.ClearCheckpoint(); // タイトルに戻る場合はチェックポイントをクリア
        ChangeScene.LoadScene(false, _titleSceneName);
    }
}
