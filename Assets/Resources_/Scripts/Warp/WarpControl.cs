using System.Collections;
using UnityEngine;

public class WarpControl : MonoBehaviour {
    private const float JUST_AVOID_ZONE_DURATION = 0.1f; // ジャスト回避ゾーンの持続時間
    
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

    [SerializeField] private Vector2 _coinCheckSize = new Vector2(5,3);   // コインチェックの半径
    [SerializeField] private LayerMask _coinLayer;          // コインのレイヤー
    [SerializeField] private float _coinWarpInterval = 0.1f;
    [SerializeField] private float _avoidWarpInterval = 0.5f;
    private bool _isRight = true;   // 右向きか確認
    public bool isRight { get { return _isRight; } set { _isRight = value; } }
    private Vector3 _forward => _isRight ? Vector3.right : Vector3.left;
    // コインワープ用の前方以外のチェック率
    private float _otherCheckRate = 0.7f;

    // 回避ワープ用のエフェクト間隔タイマー
    private float _avoidEffectInterval = 0.5f;
    private float _currentAvoidEffectInterval = 0.1f;

    // ワープ共通処理
    System.Action _onPreWarpCommon = null;
    System.Action _onWarpEndCommon = null;

    // 最後にワープした方向
    public eWarpDirection lastWarpDir { get; private set; } = eWarpDirection.Right;

    [SerializeField] private AudioSource _audioSource;
    public AudioSource audioSource { get { return _audioSource; } }
    [SerializeField] private AudioClip _seWarp;

    /// <summary>
    /// セットアップ
    /// </summary>
    public void Setup(System.Action on_pre_warp, System.Action on_warp_end) {
        _onPreWarpCommon = on_pre_warp;
        _onWarpEndCommon = on_warp_end;
    }

    // --- デバッグ用: ジャスト回避判定の可視化 ---
    private Vector2? _debugJustAvoidCenter = null;
    private float _debugJustAvoidRadius = 1.0f;
    private float _debugJustAvoidTimer = 0f;

    // --- ジャスト回避クールタイム管理 ---
    private float _justAvoidCooldownTimer = 0f;

    // --- ジャスト回避判定管理 ---
    private bool _isJustAvoidActive = false;
    private Vector2 _justAvoidCenter;
    private float _justAvoidRadius = 0.5f;
    private float _justAvoidTimer;
    private System.Action _justAvoidCallback;
    public void SetJustAvoidCallback(System.Action callback) {
        if (_justAvoidCallback != null) return;
        _justAvoidCallback = callback;
    }
    private bool _justAvoided;

    private void Update() {
        if (_currentAvoidEffectInterval > 0) {
            _currentAvoidEffectInterval -= Time.deltaTime;
        }
        // ジャスト回避判定（毎フレーム）
        if (_isJustAvoidActive) {
            if (!_justAvoided) {
                int damageZoneLayer = LayerMask.NameToLayer("DamageZone");
                int layerMask = 1 << damageZoneLayer;
                Collider2D[] hits = Physics2D.OverlapCircleAll(_justAvoidCenter, _justAvoidRadius, layerMask);
                foreach (var hit in hits) {
                    var damageZone = hit.GetComponent<DamageZone>();
                    if (damageZone != null) {
                        _justAvoided = true;
                        _justAvoidCallback?.Invoke();
                        break;
                    }
                }
            }
            _justAvoidTimer -= Time.deltaTime;
            if (_justAvoidTimer <= 0f) {
                _isJustAvoidActive = false;
                _justAvoidCallback = null;
                _justAvoidCooldownTimer = 0.5f;
            }
        }
        // デバッグ用: ジャスト回避判定の可視化タイマー
        if (_debugJustAvoidTimer > 0f) {
            _debugJustAvoidTimer -= Time.deltaTime;
            if (_debugJustAvoidTimer <= 0f) {
                _debugJustAvoidCenter = null;
            }
        }
        // ジャスト回避クールタイム
        if (_justAvoidCooldownTimer > 0f) {
            _justAvoidCooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 共通ワープ処理
    /// </summary>
    /// <param name="safe_point">ワープ先</param>
    private IEnumerator _ExecuteWarpCommon(
        Vector2 safe_point, 
        bool is_warp_camera = true,
        float end_delay = 0.0f
        ) {

        // safe_pointとの間に"WarpProhibitedArea"がある場合は手前で止める
        RaycastHit2D hit = Physics2D.Linecast(transform.position, safe_point, LayerMask.GetMask("WarpProhibitedArea"));
        if (hit.collider != null) {
            // 少し手前で止める
            safe_point = hit.point - (safe_point - (Vector2)transform.position).normalized * 0.1f;
        }

        // ワープ前の共通処理
        if (_onPreWarpCommon != null) {
            _onPreWarpCommon();
            yield return 0.1f; // 一瞬待機
        }

        // ワープSE再生
        if (_audioSource != null && _seWarp != null) {
            _audioSource.PlayOneShot(_seWarp);
        }
        transform.position = safe_point;

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

        if (end_delay > 0.0f)
            yield return new WaitForSeconds(end_delay);

        if (_onWarpEndCommon != null)
            _onWarpEndCommon();
    }

    /// <summary>
    /// ワープ処理を実行
    /// </summary>
    /// <param name="direction">方向</param>
    public IEnumerator DirectionWarp(eWarpDirection direction, 
        System.Action<Enemy_Base> warp_attack_callback)
    {
        // ワープ前の位置保存
        Vector2 origin = transform.position;

        // ワープ先の決定
        Vector2 safe_point = origin;
        if (0 <= direction && (int)direction < warpCheckers.Length) {
            WarpChecker warp_checker = warpCheckers[(int)direction];
            safe_point = warp_checker.GetWarpDestination(origin, warp_checker.transform.position);
        }

        // 現在地からワープ先までの間に敵がいるか確認
        RaycastHit2D[] hits = Physics2D.LinecastAll(origin, safe_point, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits) {
            Enemy_Base enemy = hit.collider.GetComponent<Enemy_Base>();
            if (enemy != null) {
                // 敵に攻撃コールバック
                if (warp_attack_callback != null) {
                    warp_attack_callback(enemy);
                }
            }
        }

        // ジャスト回避の確認
        SpawnJustAvoidZone();

        // ワープ先に移動
        yield return _ExecuteWarpCommon(safe_point);

        yield return CoinWarp();
    }

    /// <summary>
    /// ジャスト回避判定（毎フレーム検索）
    /// </summary>
    public void SpawnJustAvoidZone() {
        // クールタイム中は判定しない
        if (_justAvoidCooldownTimer > 0f || _isJustAvoidActive) return;

        _isJustAvoidActive = true;
        _justAvoidCenter = transform.position;
        _justAvoidTimer = JUST_AVOID_ZONE_DURATION;
        _justAvoided = false;

        // デバッグ用: 判定範囲を記録
        _debugJustAvoidCenter = _justAvoidCenter;
        _debugJustAvoidRadius = _justAvoidRadius;
        _debugJustAvoidTimer = JUST_AVOID_ZONE_DURATION;
    }

    /// <summary>
    /// ワープチェッカーを指定してワープ
    /// </summary>
    public IEnumerator TargetWarp(WarpChecker warp_checker, float end_delay = 0.0f) {
        // ワープ前の位置保存
        Vector2 origin = transform.position;
        var safe_point = warp_checker.GetWarpDestination(origin, warp_checker.transform.position);

        // ワープ先に移動
        yield return _ExecuteWarpCommon(safe_point, end_delay: end_delay);
    }

    /// <summary>
    /// コインワープ
    /// </summary>
    public IEnumerator CoinWarp() {
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
                end_delay: _coinWarpInterval
                );
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

    /// <summary>
    /// 回避ワープ
    /// </summary>
    public IEnumerator AvoidWarp(System.Action avoid_effect, Vector2 input_dir) {
        // 全てのチェッカーでワープ可能な方向を調べる
        WarpChecker[] valid_checkers =
        System.Array.FindAll(warpCheckers, (checker) => {
            var safe_point = checker.GetWarpPoint(true);
            if (!safe_point.HasValue) {
                safe_point = checker.GetWarpPoint(false);
            }
            return safe_point.HasValue;
        });

        if (_currentAvoidEffectInterval <= 0) {
            if (avoid_effect != null) {
                avoid_effect();
            }
        }

        // ワープ可能なチェッカーが無ければキャンセル
        if (valid_checkers.Length == 0) {
            if (_currentAvoidEffectInterval <= 0) {
                _currentAvoidEffectInterval = _avoidEffectInterval;
            }
            yield break;
        }

        WarpChecker selected_checker = null;

        // input_dirの方向に対応するチェッカーを優先的に選択
        if (input_dir.magnitude > 0.1f) {
            eWarpDirection preferred_direction = _GetDirectionFromInput(input_dir);
            
            // 優先方向のチェッカーが有効かチェック
            if (preferred_direction != eWarpDirection.Neutral && 
                (int)preferred_direction < warpCheckers.Length) {
                WarpChecker preferred_checker = warpCheckers[(int)preferred_direction];
                
                // 優先チェッカーが有効なチェッカーのリストに含まれているか確認
                if (System.Array.Exists(valid_checkers, (checker) => checker == preferred_checker)) {
                    selected_checker = preferred_checker;
                }
            }
        }

        // 優先方向が選択できなければランダムに選択
        if (selected_checker == null) {
            selected_checker = valid_checkers[Random.Range(0, valid_checkers.Length)];
        }

        yield return TargetWarp(selected_checker, _avoidWarpInterval);
    }

    /// <summary>
    /// 入力方向からワープ方向を取得
    /// </summary>
    private eWarpDirection _GetDirectionFromInput(Vector2 input_dir) {
        // 入力を正規化
        input_dir.Normalize();

        // 8方向の角度（0度 = 右、反時計回り）
        float angle = Mathf.Atan2(input_dir.y, input_dir.x) * Mathf.Rad2Deg;
        
        // 角度を0～360度に正規化
        if (angle < 0) angle += 360f;

        // 8方向に分類（各方向45度の範囲）
        // Right: 337.5 ~ 22.5, UpRight: 22.5 ~ 67.5, Up: 67.5 ~ 112.5, ...
        if (angle >= 337.5f || angle < 22.5f) {
            return eWarpDirection.Right;
        } else if (angle >= 22.5f && angle < 67.5f) {
            return eWarpDirection.UpRight;
        } else if (angle >= 67.5f && angle < 112.5f) {
            return eWarpDirection.Up;
        } else if (angle >= 112.5f && angle < 157.5f) {
            return eWarpDirection.UpLeft;
        } else if (angle >= 157.5f && angle < 202.5f) {
            return eWarpDirection.Left;
        } else if (angle >= 202.5f && angle < 247.5f) {
            return eWarpDirection.DownLeft;
        } else if (angle >= 247.5f && angle < 292.5f) {
            return eWarpDirection.Down;
        } else if (angle >= 292.5f && angle < 337.5f) {
            return eWarpDirection.DownRight;
        }

        return eWarpDirection.Neutral;
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

        // ジャスト回避判定のデバッグ表示
        if (_debugJustAvoidCenter.HasValue && _debugJustAvoidTimer > 0f) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_debugJustAvoidCenter.Value, _debugJustAvoidRadius);
        }
    }
}
