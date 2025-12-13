using System.Collections;
using UnityEngine;

public class Enemy_Base : Character_Base
{
    // 経験値
    [SerializeField] protected int _exp = 1;
    [SerializeField] protected CoinSpawner _coinSpawner;

    // ワープチェック用のコンポーネント
    [SerializeField] private WarpChecker _leftWarpChecker;
    [SerializeField] private WarpChecker _rightWarpChecker;

    // ロックオンマーカー
    [SerializeField] private GameObject _lockonMarker;
    [SerializeField] private DamageZone _damageZone;

    [SerializeField] protected GameObject _dieExplosion = null;
    public System.Action OnDowned = null;
    public System.Action OnDied = null;
    public System.Action OnDieEnded = null;

    // 次の行動までの時間
    protected float _nextActionTime = 0f;
    protected float _currentActionTime = 0f;

    protected override IEnumerator Die() {
        yield return base.Die();
        // ダメージゾーン無効化
        if (_damageZone != null) {
            _damageZone.gameObject.SetActive(false);
        }
        _col.enabled = false;

        // コイン生成
        if (_coinSpawner != null) {
            _coinSpawner.SpawnCoin(_exp);
        }

        // アニメーションの長さを取得してから削除
        float destroy_time = 0;
        var clip_info = _anim.GetCurrentAnimatorClipInfo(0);
        if (clip_info.Length > 0) {
            destroy_time = clip_info[0].clip.length;
        }

        OnDied?.Invoke();

        while(destroy_time > 0 ) {
            destroy_time -= Time.deltaTime;
            yield return null;
        }
        if (_dieExplosion != null) {
            // 爆発エフェクト生成
            Instantiate(_dieExplosion, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// ワープ地点取得
    /// </summary>
    /// <param name="warp_direction">ワープの向き</param>
    /// <param name="is_other_check">反対側もチェックするか</param>
    public WarpChecker? GetWarpChecker(WarpControl.eWarpDirection warp_direction, bool is_other_check = false) {
        if (warp_direction == WarpControl.eWarpDirection.Left) {
            if(is_other_check) {
                // 反対側もチェック
                var left_checker = _leftWarpChecker.GetWarpPoint();
                if (left_checker.HasValue) {
                    return _leftWarpChecker;
                }
                var right_checker = _rightWarpChecker.GetWarpPoint();
                if (right_checker.HasValue) {
                    return _rightWarpChecker;
                }
                return null;
            }
            return _leftWarpChecker;
        } else if (warp_direction == WarpControl.eWarpDirection.Right) {
            if (is_other_check) {
                // 反対側もチェック
                var right_checker = _rightWarpChecker.GetWarpPoint();
                if (right_checker.HasValue) {
                    return _rightWarpChecker;
                }
                var left_checker = _leftWarpChecker.GetWarpPoint();
                if (left_checker.HasValue) {
                    return _leftWarpChecker;
                }
                return null;
            }
            return _rightWarpChecker;
        }
        return null;
    }

    /// <summary>
    /// ロックオン表示切替
    /// </summary>
    public void EnableLockOnMarker(bool enable) {
        _lockonMarker.SetActive(enable);
    }
}
