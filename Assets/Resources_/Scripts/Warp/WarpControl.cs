using System.Collections;
using UnityEngine;
using static WarpControl;

public class WarpControl : MonoBehaviour
{
    public enum eWarpDirection
    {
        Neutral = -1,
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft
    }

    // ワープチェック用のコンポーネント
    [SerializeField] private WarpChecker[] warpCheckers;

    // カメラ
    [SerializeField] private CameraFollow _cameraFollow = default;

    [SerializeField] private Vector2 _coinCheckSize = new Vector2(5,3);   // コインチェックの半径
    [SerializeField] private LayerMask _coinLayer;          // コインのレイヤー
    [SerializeField] private float _coinWarpInterval = 0.1f;
    private bool _isRight = true;   // 右向きか確認
    public bool isRight { get { return _isRight; } set { _isRight = value; } }
    private Vector3 _forward => _isRight ? Vector3.right : Vector3.left;

    private float _otherCheckRate = 0.7f;

    /// <summary>
    /// ワープ処理を実行
    /// </summary>
    /// <param name="direction">方向</param>
    public IEnumerator Warp(eWarpDirection direction)
    {
        // ワープ前の位置保存
        Vector2 origin = transform.position;

        // ワープ先の決定
        Vector2 safe_point = origin;
        if (0 <= direction && (int)direction < warpCheckers.Length) {
            WarpChecker warp_checker = warpCheckers[(int)direction];
            safe_point = warp_checker.GetWarpDestination(origin, warp_checker.transform.position);
        }

        // ワープ先に移動
        transform.position = safe_point;
        _cameraFollow.SetWarpMode(true);

        yield return CoinWarpRoutine();
    }

    /// <summary>
    /// ワープチェッカーを指定してワープ
    /// </summary>
    public void TargetWarp(WarpChecker warp_checker) {
        // ワープ先の決定
        var safe_point = warp_checker.GetWarpPoint();

        // ワープ先に移動
        if (safe_point.HasValue) {
            transform.position = safe_point.Value;
            _cameraFollow.SetWarpMode(true);
        }
    }

    //IEnumerator WarpCommonAction(
    //    WarpControl.eWarpDirection tmp_direction, IEnumerator warp_coroutine) {

    //    // スライディングリセット
    //    _isSliding = false;
    //    // 重力を無効化
    //    _isWarpDelay = true;
    //    // 速度をリセット
    //    _rb.linearVelocity = Vector2.zero;

    //    //アニメーション管理
    //    _anim.SetBool("Warp", true);    // ワープアニメフラグ
    //    _anim.SetBool("Fall", true);    // 空中アニメフラグ
    //    _anim.Play("Warp_Enter");       // ワープアニメ再生

    //    // 一瞬待機
    //    yield return new WaitForSeconds(_param.warpWaitTime);

    //    // ワープ実行
    //    yield return warp_coroutine;
    //    _anim.SetBool("Warp", false);

    //    // Note: ワープダッシュはカット

    //    // ワープダッシュのクールタイムをリセット
    //    _currentWarpCoolTime = _param.warpCoolTime;
    //    // スライドジャンプリセット
    //    _isSlidingJump = false;
    //}

    /// <summary>
    /// コインワープ
    /// </summary>
    public IEnumerator CoinWarpRoutine() {
        int count = 0;
        int max_count = 100;

        while (count < max_count) {
            // ワープ先取得
            var coin_pos = GetCoinWarpCheck();
            if (!coin_pos.HasValue) break;

            transform.position = coin_pos.Value;

            yield return new WaitForSeconds(_coinWarpInterval);
        }
    }

    /// <summary>
    /// コインワープが出来るか確認
    /// </summary>
    public Vector3? GetCoinWarpCheck() {
        // 前方チェック
        Vector3? coin_pos = _GetNearestCoin(_coinCheckSize, _forward, _coinCheckSize.x / 2);
        // 後方チェック
        if (!coin_pos.HasValue)
            coin_pos = _GetNearestCoin(_coinCheckSize * _otherCheckRate, -_forward, _coinCheckSize.x * _otherCheckRate / 2);
        // 上方チェック
        if (!coin_pos.HasValue)
            coin_pos = _GetNearestCoin(_coinCheckSize * _otherCheckRate, Vector2.up, _coinCheckSize.y * _otherCheckRate / 2);
        // 下方チェック
        if (!coin_pos.HasValue)
            coin_pos = _GetNearestCoin(_coinCheckSize * _otherCheckRate, Vector2.down, _coinCheckSize.y * _otherCheckRate / 2);
        return coin_pos;
    }
    
    /// <summary>
    /// 一番近くのコインを取得
    /// </summary>
    private Vector3? _GetNearestCoin(Vector2 check_size, Vector2 direction, float distance) {
        Vector3 origin = transform.position;
        
        // コイン検知
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, check_size, 0, direction, distance, _coinLayer);

        if (hits.Length > 0) {
            float min_dist = Mathf.Infinity;
            Vector3 best_pos = origin;

            // 検知したコインの中で一番近い位置を取得
            foreach (var hit in hits) {
                float dist = Vector3.Distance(origin, hit.point);
                if (dist < min_dist) {
                    min_dist = dist;
                    best_pos = hit.collider.transform.position;
                }
            }
            return best_pos;
        }
        return null;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + _forward * _coinCheckSize.x / 2, _coinCheckSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position - _forward * _coinCheckSize.x * _otherCheckRate / 2, _coinCheckSize * _otherCheckRate);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + Vector3.up * _coinCheckSize.y * _otherCheckRate / 2, _coinCheckSize * _otherCheckRate);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.down * _coinCheckSize.y * _otherCheckRate / 2, _coinCheckSize * _otherCheckRate);
    }
}
