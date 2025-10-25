#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;

public class TitleManager : MonoBehaviour {
#if UNITY_EDITOR
    [SerializeField] private SceneAsset _sceneAsset; // シーンアセット
#endif
    [SerializeField] private string _sceneName;

    [SerializeField] private PlayerController _playerController;
    [SerializeField] private GameObject _japaneseActiveButton;
    [SerializeField] private GameObject _englishActiveButton;
    [SerializeField] private RectTransform _japaneseUiRect;
    [SerializeField] private RectTransform _englishUiRect;
    [SerializeField] private RectTransform _frameUiRect;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _seSelect;
    [SerializeField] private AudioClip _seDecide;

    private bool _isSelected = false;
    private bool _isDecided = false;

    private void Reset() {
        _playerController = FindAnyObjectByType<PlayerController>();
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if (_sceneAsset != null) {
            _sceneName = _sceneAsset.name;
        }
    }
#endif
    private PlayerParameter _playerParameter;
    private void Start() {
        _playerParameter = PlayerParameter.Instance;
        _playerParameter.language = PlayerParameter.eLanguage.Japanese;
    }

    public void Update() {
        if (_playerController == null) {
            _playerController = FindAnyObjectByType<PlayerController>();
        }
        if (_playerParameter == null || _isDecided) {
            return;
        }

        if (_playerController.Input.move.y > 0.5f) {
            _SetLaunguage(PlayerParameter.eLanguage.Japanese);
        } else if (_playerController.Input.move.y < -0.5f) {
            _SetLaunguage(PlayerParameter.eLanguage.English);
        } else {
            _isSelected = false;
        }

        if (_playerController.Input.messageNextPressed) {
            if (_audioSource != null && _seDecide != null) {
                _audioSource.PlayOneShot(_seDecide);
            }
            _isDecided = true;
            ChangeScene.LoadScene(_sceneName);
        }
    }
    private void _SetLaunguage(PlayerParameter.eLanguage language) {
        if (_isSelected || _playerParameter.language == language) {
            return;
        }
        _isSelected = true;
        _playerParameter.language = language;
        _UpdateUiView(language);
        if (_audioSource != null && _seSelect != null) {
            _audioSource.PlayOneShot(_seSelect);
        }
    }

    private void _UpdateUiView(PlayerParameter.eLanguage language) {
        _japaneseActiveButton.SetActive(language == PlayerParameter.eLanguage.Japanese);
        _englishActiveButton.SetActive(language == PlayerParameter.eLanguage.English);
        if (language == PlayerParameter.eLanguage.Japanese) {
            StartCoroutine(_UiMoveAnim(_japaneseUiRect.anchoredPosition, 0.1f));
        } else {
            StartCoroutine(_UiMoveAnim(_englishUiRect.anchoredPosition, 0.1f));
        }
    }

    private IEnumerator _UiMoveAnim(Vector2 set_anchor_pos, float over_rate) {
        var start_pos = _frameUiRect.anchoredPosition;
        float anim_time = 0.05f;
        float elapsed_time = 0f;
        while (elapsed_time < anim_time) {
            elapsed_time += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed_time / anim_time);
            _frameUiRect.anchoredPosition = Vector2.Lerp(start_pos, set_anchor_pos, t);
            yield return null;
        }
    }
}
