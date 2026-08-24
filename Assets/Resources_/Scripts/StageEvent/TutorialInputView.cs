using UnityEngine;

public class TutorialInputView : MonoBehaviour {
    // 十字キー入力表示
    [SerializeField] private SpriteRenderer _rightInputView;
    [SerializeField] private SpriteRenderer _downInputView;
    // ボタン入力表示
    [SerializeField] private SpriteRenderer _ButtonInputRend;
    // プレイヤーコントローラー
    [SerializeField] private PlayerController _PlayerController;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seInput;

    private bool _isAllComplete = false;
    private float _blinkSpeed = 2.0f;

    private void Update() {
        if(_PlayerController == null) {
            _PlayerController = FindAnyObjectByType<PlayerController>();
        }

        var input = _PlayerController.virtualInput;
        bool is_right = input.move.x > 0.5f || _rightInputView == null;
        bool is_down = input.move.y < -0.5f || _downInputView == null;

        bool is_button = input.isJumpPressed;

        // 十字キー入力表示
        if (_rightInputView != null) {
            if (is_right || _isAllComplete) {
                if(is_right && !_rightInputView.color.Equals(Color.green)) {
                    // 入力音再生
                    if(_audioSource != null && _seInput != null) {
                        _audioSource.PlayOneShot(_seInput);
                    }
                }
                _rightInputView.color = Color.green;
            } else {
                _rightInputView.color = _GetWaitInputColor();
            }
        }
        if (_downInputView != null) {
            if (is_down || _isAllComplete) {
                if(is_down && !_downInputView.color.Equals(Color.green)) {
                    // 入力音再生
                    if(_audioSource != null && _seInput != null) {
                        _audioSource.PlayOneShot(_seInput);
                    }
                }
                _downInputView.color = Color.green;
            } else {
                _downInputView.color = _GetWaitInputColor();
            }
        }

        // 十字キー完了判定
        var isDpadComplete = is_right && is_down;

        // ボタン入力表示
        if (_ButtonInputRend != null) {
            if (_isAllComplete) {
                _ButtonInputRend.color = Color.green;
            } else if (isDpadComplete == false) {
                _ButtonInputRend.color = Color.gray;
            } else if (is_button) {
                _isAllComplete = true;
            } else {
                _ButtonInputRend.color = _GetWaitInputColor();
            }
        } else if (isDpadComplete) {
            _isAllComplete = true;
        }
    }

    // 入力待ちの点滅色取得
    private Color _GetWaitInputColor() {
        float t = Mathf.PingPong(Time.unscaledTime * _blinkSpeed, 1.0f);
        return Color.Lerp(Color.white, Color.yellow, t);
    }
}
