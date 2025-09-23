using System.Collections;
using UnityEngine;

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

    // ワープ共通処理
    System.Action _onPreWarpCommon = null;
    System.Action _onWarpEndCommon = null;

    // 最後にワープした方向
    public eWarpDirection lastWarpDir { get; private set; } = eWarpDirection.Right;

    /// <summary>
    /// セットアップ
    /// </summary>
    public void Setup(System.Action on_pre_warp, System.Action on_warp_end) {
        _onPreWarpCommon = on_pre_warp;
        _onWarpEndCommon = on_warp_end;
    }

    /// <summary>
    /// 共通ワープ処理
    /// </summary>
    /// <param name="safe_point">ワープ先</param>
    private IEnumerator _ExecuteWarpCommon(
        Vector2 safe_point, 
        bool is_warp_camera = true,
        IEnumerator on_pre_end = null
        ) {
        if (_onPreWarpCommon != null) {
            _onPreWarpCommon();
            yield return 0.1f; // 一瞬待機
        }

        transform.position = safe_point;
        _cameraFollow.SetWarpMode(is_warp_camera);

        // 最後にワープした方向を保存
        WarpChecker nearest_checker = null;
        float nearest_dist = Mathf.Infinity;
        foreach (var checker in warpCheckers) {
            float dist = Vector2.Distance(checker.transform.position, safe_point);
            if (dist < nearest_dist) {
                nearest_dist = dist;
                nearest_checker = checker;
            }
        }
        if(nearest_checker != null) {
            lastWarpDir = (eWarpDirection)System.Array.IndexOf(warpCheckers, nearest_checker);
        }

        if(on_pre_end != null) {
            yield return on_pre_end;
        }

        if (_onWarpEndCommon != null)
            _onWarpEndCommon();
    }

    /// <summary>
    /// ワープ処理を実行
    /// </summary>
    /// <param name="direction">方向</param>
    public IEnumerator DirectionWarp(eWarpDirection direction)
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
        yield return _ExecuteWarpCommon(safe_point);

        yield return CoinWarpRoutine();
    }

    /// <summary>
    /// ワープチェッカーを指定してワープ
    /// </summary>
    public IEnumerator TargetWarp(WarpChecker warp_checker) {
        // ワープ先の決定
        var safe_point = warp_checker.GetWarpPoint();

        // ワープ先に移動
        if (safe_point.HasValue) {
            yield return _ExecuteWarpCommon(safe_point.Value);
        }
    }

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

            // ワープ先に移動
            yield return _ExecuteWarpCommon(
                coin_pos.Value, 
                is_warp_camera:false, 
                on_pre_end: wait_routine()
                );

            IEnumerator wait_routine() {
                yield return new WaitForSeconds(_coinWarpInterval);
            }
        }
    }

    /// <summary>
    /// コインワープが出来るか確認
    /// </summary>
    public Vector3? GetCoinWarpCheck() {
        // 前方チェック
        Vector3? coin_pos = _GetNearestCoin(_coinCheckSize, _forward, _coinCheckSize.x * 0.5f);
        // 後方チェック
        if (!coin_pos.HasValue)
            coin_pos = _GetNearestCoin(_coinCheckSize * _otherCheckRate, -_forward, _coinCheckSize.x * _otherCheckRate * 0.5f);
        // 上方チェック
        if (!coin_pos.HasValue)
            coin_pos = _GetNearestCoin(_coinCheckSize * _otherCheckRate, Vector2.up, _coinCheckSize.y * _otherCheckRate * 0.5f);
        // 下方チェック
        if (!coin_pos.HasValue)
            coin_pos = _GetNearestCoin(_coinCheckSize * _otherCheckRate, Vector2.down, _coinCheckSize.y * _otherCheckRate * 0.5f);
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
        Gizmos.DrawWireCube(transform.position + _forward * _coinCheckSize.x * 0.5f, _coinCheckSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position - _forward * _coinCheckSize.x * _otherCheckRate * 0.5f, _coinCheckSize * _otherCheckRate);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + Vector3.up * _coinCheckSize.y * _otherCheckRate * 0.5f, _coinCheckSize * _otherCheckRate);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.down * _coinCheckSize.y * _otherCheckRate * 0.5f, _coinCheckSize * _otherCheckRate);
    }
}
