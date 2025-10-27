using UnityEngine;
using UnityEngine.InputSystem;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }
    private string _titleSceneName = "Title";

    private void Update() {
        var keyboard = Keyboard.current;
        if(keyboard == null) {
            return;
        }

        // ESCキーでタイトルへ戻る
        if (keyboard.escapeKey.wasPressedThisFrame) {
            GameRestart();
        }

        // Rキーでステージ再スタート
        if (keyboard.rKey.wasPressedThisFrame) {
            StageRestart();
        }
    }

    public void StageRestart() {
        // シーン再読み込み
        ChangeScene.LoadScene();
    }

    public void GameRestart() {
        ChangeScene.LoadScene(_titleSceneName);
    }
}
