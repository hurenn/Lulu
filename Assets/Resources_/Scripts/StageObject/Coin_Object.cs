using System.Threading;
using UnityEngine;

/// <summary>
/// コインオブジェクト
/// </summary>
public class Coin_Object : StageObject_Base
{
    [SerializeField] private int _MpRecoverAmount = 10; // コイン取得で回復するMP量
    [SerializeField] private int _coinValue = 1; // コインの価値
    [SerializeField] private AudioClip _collectSound; // コイン取得音

    [Header("自動回収設定")]
    [SerializeField] private bool _isAutoCollect = false;   // 自動回収するかどうか
    [SerializeField] private float _attractForce = 6.0f;    // プレイヤーに引き寄せる力
    [SerializeField] private float _friction = 0.95f; // 慣性移動の減衰率
    [SerializeField] private Vector2 _scatterPowerRandomRange = new Vector2(3.0f, 5.0f); // 散らばる力のランダム範囲
    [SerializeField] private Vector2 _scatterAngleRange = new Vector2(150.0f, 210.0f); // 散らばる角度の範囲
    private Transform _playerTransform = null;
    private Vector3 _velocity = Vector3.zero;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private GameObject _pickEffect;

    /// <summary>
    /// 自動回収の初期化
    /// </summary>
    public void InitializeAutoCollect() {
        _isAutoCollect = true;
        if(_trail != null) {
            _trail.enabled = true; // トレイルを有効化
        }
        // レイヤーを変更
        gameObject.layer = LayerMask.NameToLayer("Default");

        // プレイヤーのTransformを取得
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        if (_playerTransform == null) {
            Debug.LogError("Playerオブジェクトが見つかりません。");
            return;
        }

        // ランダムな角度で散らばる力を設定
        float angle = Random.Range(_scatterAngleRange.x, _scatterAngleRange.y);
        Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
        if(transform.position.x > _playerTransform.position.x) {
            direction.x = -direction.x; // プレイヤーの反対方向に散らばる
        }
        _velocity = direction.normalized * Random.Range(_scatterPowerRandomRange.x, _scatterPowerRandomRange.y);
    }

    private void Update() {
        if(!_isAutoCollect || _playerTransform == null) {
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

    protected override void _HitPlayer(Player_Character player)
    {
        base._HitPlayer(player);
        if (player != null)
        {
            player.RecoverMP(_MpRecoverAmount, true); // コイン取得でMPを回復
            player.AddExp(_coinValue);
            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound, transform.position);
            }
            if(_trail != null) {
                _trail.transform.parent = null; // トレイルを親から外す
                _trail.autodestruct = true; // 自動削除を有効化
            }
            if(_pickEffect != null) {
                Instantiate(_pickEffect, transform.position, Quaternion.identity, player.transform);
            }
            Destroy(gameObject); // コインオブジェクトを削除
        }
    }
}
