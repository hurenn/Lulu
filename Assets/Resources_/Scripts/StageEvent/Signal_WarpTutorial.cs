using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Signal_WarpTutorial : MonoBehaviour
{
    public static Signal_WarpTutorial Instance { get; private set; }
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

    public void WarpTutorial_First() {
        _WarpTutorial_Common(Vector2.right, true);
    }
    public void WarpTutorial_Second() {
        _WarpTutorial_Common(new Vector2(0.75f, -0.75f), true);
    }
    public void WarpTutorial_Third() {
        _WarpTutorial_Common(Vector2.down, true);
    }
    public void WarpTutorial_Fourth() {
        _WarpTutorial_Common(Vector2.right, false);
    }
    public void WarpTutorial_Fifth() {
        _WarpTutorial_Common(Vector2.right, true, any_dir: true);
    }
    public void WarpTutorial_Sixth() {
        _WarpTutorial_Common(Vector2.right, true, any_dir: true);
    }
    public void WarpTutorial_End() {
        playerController.insertMoveRight = false;
        playerController.insertMoveDown = false;
        playerController.insertJumpHeld = false;
    }

    private void _WarpTutorial_Common(Vector2 dir_input, bool jump_input, bool any_dir = false) {
        if (playerController == null) {
            return;
        }

        // 入力リセット
        playerController.insertMoveRight = false;
        playerController.insertMoveDown = false;
        // 画面スロー
        _isTutorialActive = true;

        CharacterInputData specific_input = new CharacterInputData();
        specific_input.move = any_dir ? Vector2.zero : dir_input;
        specific_input.isJumpPressed = jump_input;
        playerController.SetSpecificInput(specific_input, () => {
            // 入力完了後の処理
            _isTutorialActive = false;
            Time.timeScale = 1f; // 時間を元に戻す

            // コントローラー自動入力
            StartCoroutine(_InsertController_Common(dir_input, jump_input));
        });
    }

    /// <summary>
    /// コントローラー自動入力
    /// </summary>
    private IEnumerator _InsertController_Common(Vector2 dir_input, bool jump_input) {
        if (playerController == null) {
            yield break;
        }

        // 方向入力
        playerController.insertMoveRight = dir_input.x > 0.5f;
        playerController.insertMoveDown = dir_input.y < -0.5f;

        yield return null;

        // ジャンプ入力
        if (jump_input) {
            playerController.insertJumpHeld = true;
            yield return null;
            playerController.insertJumpHeld = false;
        }
    }
}
