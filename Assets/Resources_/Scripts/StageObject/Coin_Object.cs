using System.Collections;
using UnityEngine;

/// <summary>
/// コインオブジェクト
/// </summary>
public class Coin_Object : StageObject_Base {
    [SerializeField] private int _MpRecoverAmount = 10; // コイン取得で回復するMP量
    [SerializeField] private int _coinValue = 1; // コインの価値

    [Header("自動回収設定")]
    [SerializeField] private bool _isAutoCollect = false;   // 自動回収するかどうか
    [SerializeField] private float _attractForce = 6.0f;    // プレイヤーに引き寄せる力
    [SerializeField] private float _friction = 0.95f; // 慣性移動の減衰率
    [SerializeField] private Vector2 _scatterPowerRandomRange = new Vector2(3.0f, 5.0f); // 散らばる力のランダム範囲
    [SerializeField] private Vector2 _scatterAngleRange = new Vector2(150.0f, 210.0f); // 散らばる角度の範囲
    private Transform _playerTransform = null;
    private Vector3 _velocity = Vector3.zero;
    [SerializeField] private SpriteRenderer _coinRend;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private GameObject _pickEffect;

    [SerializeField] private AudioClip _seCollectCoin; // コイン取得音

    private System.Action<Coin_Object> _releaseCallback = null; // コインプールに戻すためのコールバック

    /// <summary>
    /// 自動回収の初期化
    /// </summary>
    public void InitializeAutoCollect() {
        _isAutoCollect = true;
        if (_trail != null) {
            _trail.enabled = true; // トレイルを有効化
        }
        // レイヤーを変更
        gameObject.layer = LayerMask.NameToLayer("Default");

        // プレイヤーのTransformを取得
        if (_playerTransform == null) {
            _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            if (_playerTransform == null) {
                Debug.LogError("Playerオブジェクトが見つかりません。");
                return;
            }
        }

        // ランダムな角度で散らばる力を設定
        float angle = Random.Range(_scatterAngleRange.x, _scatterAngleRange.y);
        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
        if (transform.position.x > _playerTransform.position.x) {
            direction.x = -direction.x; // プレイヤーの反対方向に散らばる
        }
        _velocity = direction.normalized * Random.Range(_scatterPowerRandomRange.x, _scatterPowerRandomRange.y);
    }

    private void Update() {
        _UpdateAutoCollect();
    }

    /// <summary>
    /// コインの自動回収更新
    /// </summary>
    private void _UpdateAutoCollect() {
        if (!_isAutoCollect || _playerTransform == null || !_coinRend.enabled) {
            return;
        }

        // プレイヤー方向への吸引ベクトルを計算
        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        _velocity += directionToPlayer * _attractForce * Time.deltaTime;

        // 慣性移動
        transform.position += _velocity * Time.deltaTime;
        // 徐々に速度を減衰させる
        _velocity *= _friction;
    }

    protected override void _HitPlayer(Player_Character player) {
        if (!gameObject.activeSelf || (_coinRend != null && !_coinRend.enabled)) {
            return; // 非アクティブ時は処理しない
        }

        base._HitPlayer(player);
        if (player != null) {
            player.RecoverMP(_MpRecoverAmount, true); // コイン取得でMPを回復
            player.AddExp(_coinValue);
            if (player.audioSource != null && _seCollectCoin != null) {
                player.audioSource.PlayOneShot(_seCollectCoin);
            }

            if (_pickEffect != null) {
                Instantiate(_pickEffect, transform.position, Quaternion.identity, player.transform);
            }
            // レイヤーを変更
            gameObject.layer = LayerMask.NameToLayer("Default");

            StartCoroutine(_HideRoutine());
        }
    }

    /// <summary>
    /// コイン生成
    /// </summary>
    public void Spawn(Vector2 position, System.Action<Coin_Object> releaseCallback, bool is_auto_collect = true) {
        transform.position = position;
        gameObject.SetActive(true);
        _coinRend.enabled = true;   // コインの表示を有効化

        // レイヤーを元に戻す
        gameObject.layer = LayerMask.NameToLayer("Coin");

        _trail?.Clear(); // トレイルをクリア

        _releaseCallback = releaseCallback; // コールバックを設定
        if (is_auto_collect) {
            InitializeAutoCollect();
        }
    }

    /// <summary>
    /// コイン非表示ルーチン
    /// </summary>
    private IEnumerator _HideRoutine() {
        _coinRend.enabled = false;
        if (_trail != null) {
            yield return new WaitForSeconds(2.0f);
        }

        // プールに戻す
        _releaseCallback?.Invoke(this);
    }
}
