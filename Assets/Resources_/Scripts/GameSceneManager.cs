using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSceneManager : MonoBehaviour
{
    private static GameSceneManager _instance;
    public static GameSceneManager Instance {
        get {
            // シーン上のGameSceneManagerを探す
            if (_instance == null) {
                _instance = FindAnyObjectByType<GameSceneManager>();
                if (_instance == null) {
                    // 見つからなければ新規作成
                    _instance = new GameObject("GameSceneManager").AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
            }
            return _instance;
        }
        set {
            _instance = value;
        }
    }
    private string _titleSceneName = "Title";
    
    // リトライ種別を記憶
    private static bool _isDeathRetry = false;

    private void Start() {
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

        // デバッグ用：数字キーで能力の付与/解除
        if (keyboard.numpad4Key.wasPressedThisFrame) {
            var player = PlayerCharacterManager.Current as Player_Character;
            if (player != null) {
                var had_ability = PlayerParameter.Instance.IsOwned(eAbilityType.Ice);
                if (had_ability) {
                    player.RemoveAbility(eAbilitySlot.Y);
                } else {
                    player.SetAbilitySlot(eAbilityType.Ice, eAbilitySlot.Y);
                }
            }
        }
        if (keyboard.numpad8Key.wasPressedThisFrame) {
            var player = PlayerCharacterManager.Current as Player_Character;
            if (player != null) {
                var had_ability = PlayerParameter.Instance.IsOwned(eAbilityType.Light);
                if (had_ability) {
                    player.RemoveAbility(eAbilitySlot.X);
                } else {
                    player.SetAbilitySlot(eAbilityType.Light, eAbilitySlot.X);
                }
            }
        }
        if (keyboard.numpad6Key.wasPressedThisFrame) {
            var player = PlayerCharacterManager.Current as Player_Character;
            if (player != null) {
                var had_ability = PlayerParameter.Instance.IsOwned(eAbilityType.Fire);
                if (had_ability) {
                    player.RemoveAbility(eAbilitySlot.A);
                } else {
                    player.SetAbilitySlot(eAbilityType.Fire, eAbilitySlot.A);
                }
            }
        }
        if (keyboard.numpad2Key.wasPressedThisFrame) {
            var player = PlayerCharacterManager.Current as Player_Character;
            if (player != null) {
                var had_ability = PlayerParameter.Instance.IsOwned(eAbilityType.Warp);
                if (had_ability) {
                    player.RemoveAbility(eAbilitySlot.B);
                } else {
                    player.SetAbilitySlot(eAbilityType.Warp, eAbilitySlot.B);
                }
            }
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
