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
    [SerializeField] private AudioClip _seWarp;

    // 集中線エフェクト
    [SerializeField] protected ParticleSystem _warpFocusEffect;
    public void PlayWarpFocusEffect() {
        if (_warpFocusEffect != null) {
            if (_warpFocusEffect.gameObject.activeSelf == false) {
                _warpFocusEffect.gameObject.SetActive(true);
            }
            _warpFocusEffect.Play();
        }
    }

    // ワープの瞬間、ワープ前の位置に残す残像
    [Header("残像")]
    [SerializeField] private SpriteRenderer _afterImageSource;   // 残像にする見た目(未設定なら残像を出さない)
    [SerializeField] private float _afterImageDuration = 0.2f;   // 残像が消えるまでの時間
    [SerializeField] private Color _afterImageColor = new Color(1f, 1f, 1f, 0.6f); // 残像の色(アルファは出始めの濃さ。消えるまでに薄くなる)
    [SerializeField] private Material _afterImageMaterial;       // 残像の材質(WarpAfterImageRainbowで虹色に光らせる。未設定なら本体と同じ材質)

    // ワープの直後、ワープ先で見た目を一瞬光らせる(_afterImageSourceの見た目に、同じ形の光を重ねる)
    [Header("ワープ直後の発光")]
    [SerializeField] private Material _warpFlashMaterial;        // 光の材質(SpriteColorFlashで加算合成する。未設定なら光らせない)
    [SerializeField] private Color _warpFlashColor = new Color(0.2f, 0.75f, 1f, 0.35f); // 光の色(アルファは光り始めの強さ)
    [SerializeField] private float _warpFlashDuration = 0.18f;   // 光が消えるまでの時間
    private SpriteRenderer _warpFlash;
    private Coroutine _warpFlashRoutine;

    // ワープ先調整前の最大距離地点エフェクト（成功・失敗に関わらず表示）
    [SerializeField] protected GameObject _warpMaxDistanceEffectPrefab;
    public void PlayWarpMaxDistanceEffect(Vector3 pos) {
        if (_warpMaxDistanceEffectPrefab != null) {
            Instantiate(_warpMaxDistanceEffectPrefab, pos, Quaternion.identity);
        }
    }

    /// <summary>
    /// セットアップ
    /// </summary>
    public void Setup(System.Action on_pre_warp, System.Action on_warp_end) {
        _onPreWarpCommon = on_pre_warp;
        _onWarpEndCommon = on_warp_end;
        if(_warpFocusEffect == null) {
            _warpFocusEffect = GameObject.Find("WarpFocusEffect")?.GetComponent<ParticleSystem>();
        }
        if(_warpFocusEffect != null) {
            _warpFocusEffect.gameObject.SetActive(false);
        }
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
        // 能力が使えないエリアに入る場合は、エリアとの境界線で止める
        AbilitySealArea.ClipPath(transform.position, ref safe_point);

        // ワープ前の共通処理
        if (_onPreWarpCommon != null) {
            _onPreWarpCommon();
            yield return 0.1f; // 一瞬待機
        }

        // ワープSE再生
        if (_audioSource != null && _seWarp != null) {
            _audioSource.PlayOneShot(_seWarp);
        }
        _SpawnAfterImage();
        transform.position = safe_point;
        _PlayWarpFlash();

        // ワープ集中線エフェクト再生
        PlayWarpFocusEffect();

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

    // 今の見た目をその場に複製し、_afterImageDurationかけて薄くしながら消す
    private void _SpawnAfterImage() {
        if (_afterImageSource == null || _afterImageSource.sprite == null || _afterImageDuration <= 0f) return;

        var src = _afterImageSource;
        var image = new GameObject("WarpAfterImage").AddComponent<SpriteRenderer>();
        image.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
        image.transform.localScale = src.transform.lossyScale;
        image.sprite = src.sprite;
        image.flipX = src.flipX;
        image.flipY = src.flipY;
        image.sharedMaterial = _afterImageMaterial != null ? _afterImageMaterial : src.sharedMaterial;
        image.sortingLayerID = src.sortingLayerID;
        image.sortingOrder = src.sortingOrder - 1; // 本体より後ろに表示する
        var color = src.color * _afterImageColor;
        image.color = color;
        StartCoroutine(_FadeAfterImage(image, color));
        Destroy(image.gameObject, _afterImageDuration); // コルーチンが止まっても残り続けないようにする
    }

    // ワープ先で見た目を一瞬光らせる(連続ワープでは最初から光り直す)
    private void _PlayWarpFlash() {
        if (_warpFlashMaterial == null || _afterImageSource == null || _warpFlashDuration <= 0f) return;
        if (_warpFlash == null) {
            _warpFlash = new GameObject("WarpFlash").AddComponent<SpriteRenderer>();
            _warpFlash.transform.SetParent(_afterImageSource.transform, false);
            _warpFlash.sharedMaterial = _warpFlashMaterial;
        }
        if (_warpFlashRoutine != null) StopCoroutine(_warpFlashRoutine);
        _warpFlashRoutine = StartCoroutine(_WarpFlashRoutine());
    }

    private IEnumerator _WarpFlashRoutine() {
        var src = _afterImageSource;
        _warpFlash.enabled = true;
        _warpFlash.sortingLayerID = src.sortingLayerID;
        _warpFlash.sortingOrder = src.sortingOrder + 1; // 本体の手前に重ねる
        var color = _warpFlashColor;
        // ワープ先に出た直後はワープのアニメーション(姿がほぼ見えない)なので、姿が戻るまでは薄めずに待つ
        var anim = src.GetComponent<Animator>();
        for (float wait = 0f; wait < _WARP_FLASH_MAX_WAIT && anim != null && _IsWarpAnimation(anim); wait += Time.deltaTime) {
            _UpdateWarpFlash(src, _warpFlashColor);
            yield return null;
        }
        for (float t = 0f; t < _warpFlashDuration; t += Time.deltaTime) {
            color.a = _warpFlashColor.a * (1f - t / _warpFlashDuration);
            _UpdateWarpFlash(src, color);
            yield return null;
        }
        _warpFlash.enabled = false;
        _warpFlashRoutine = null;
    }

    private const float _WARP_FLASH_MAX_WAIT = 0.3f; // 姿が戻るのを待つ時間の上限

    private static bool _IsWarpAnimation(Animator anim) {
        var state = anim.GetCurrentAnimatorStateInfo(0);
        return state.IsName("Warp_Enter") || state.IsName("Warp_Exit");
    }

    // 本体のアニメーションに合わせて、同じ絵を重ねる
    private void _UpdateWarpFlash(SpriteRenderer src, Color color) {
        _warpFlash.sprite = src.sprite;
        _warpFlash.flipX = src.flipX;
        _warpFlash.flipY = src.flipY;
        color.a *= src.color.a;
        _warpFlash.color = color;
        _warpFlash.enabled = src.enabled; // 姿を消している時(コインレールの演出など)は光らせない
    }

    private void OnDisable() {
        // 途中で止まっても光ったまま残らないようにする
        if (_warpFlash != null) _warpFlash.enabled = false;
        _warpFlashRoutine = null;
    }

    private IEnumerator _FadeAfterImage(SpriteRenderer image, Color color) {
        float alpha = color.a;
        for (float t = 0f; t < _afterImageDuration && image != null; t += Time.deltaTime) {
            color.a = alpha * (1f - t / _afterImageDuration);
            image.color = color;
            yield return null;
        }
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

            // 調整前の最大距離地点にエフェクトを表示（ワープの成功・失敗に関わらず表示）
            // 横・斜め方向の場合はキャラクターの横幅分だけx軸方向を内側にずらす
            Vector3 effect_pos = warp_checker.transform.position;
            int horizontal_sign = _GetHorizontalSign(direction);
            if (horizontal_sign != 0) {
                effect_pos.x -= horizontal_sign * warp_checker.characterWidth;
            }
            PlayWarpMaxDistanceEffect(effect_pos);

            safe_point = warp_checker.GetWarpDestination(origin, warp_checker.transform.position);
        }
        // 能力が使えないエリアに入る場合は境界線で止まるので、その先の敵は攻撃しない
        AbilitySealArea.ClipPath(origin, ref safe_point);

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

        // コインワープチェック（指定方向）
        var coin_in_dir = GetCoinInWarpDirection(direction);
        if (coin_in_dir.HasValue) {
            yield return _ExecuteWarpCommon(coin_in_dir.Value, is_warp_camera: false, end_delay: _coinWarpInterval);
            yield return CoinWarp();
            yield break;
        }

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
        if(warp_checker == null) {
            yield break;
        }

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
        // 詰み防止: 届いたコインは取得時にコイン検知の対象外になるため、通常は同じコインが続けて選ばれることはない。
        // 届かないコインへの「ワープ失敗→同じコインへ再ワープ」のループを、失敗するワープを出さずに止める
        // (別のコインへ移っていく分には制限しない)
        Transform last_coin = null;

        while (true) {
            // ワープ先取得
            var coin = _GetCoinWarpTarget();
            if (coin == null) break;

            // 間にワープ禁止エリアがあって届かないコインには飛ばずに終える(手前で止まる中途半端なワープを出さない)
            if (Physics2D.Linecast(transform.position, coin.position, LayerMask.GetMask("WarpProhibitedArea")).collider != null) break;

            // 能力が使えないエリアの中からはワープできない
            if (AbilitySealArea.Contains(transform.position)) break;

            // 保険: 直前にワープしたコインがまた選ばれた(=届かず取得できなかった)場合は、再ワープせずに終える
            if (coin == last_coin) break;
            last_coin = coin;

            // サーカス演出のレールに乗ったら、演出側でレールの最後まで渡り切る
            var circus = CoinRailCircus.FindByCoin(coin);
            if (circus != null) {
                yield return circus.Play(this, coin);
                continue;
            }

            // 能力が使えないエリアの中のコインへは、境界線までワープして終える
            Vector2 coin_point = coin.position;
            bool is_sealed_path = AbilitySealArea.ClipPath(transform.position, ref coin_point);
            if (is_sealed_path && Vector2.Distance(transform.position, coin_point) < 0.2f) break; // 既に境界線にいる

            // ワープ先に移動
            yield return _ExecuteWarpCommon(
                coin.position,
                is_warp_camera:false,
                end_delay: _coinWarpInterval
                );
            if (is_sealed_path) break;
        }
    }

    /// <summary>
    /// 指定地点へワープ(コインワープ演出用、カメラ追従のワープ扱いはしない)
    /// </summary>
    public IEnumerator WarpToPoint(Vector2 point, float end_delay) {
        yield return _ExecuteWarpCommon(point, is_warp_camera: false, end_delay: end_delay);
    }

    /// <summary>
    /// コインワープが出来るか確認
    /// </summary>
    public Vector3? GetCoinWarpCheck() {
        var coin = _GetCoinWarpTarget();
        return coin != null ? coin.position : (Vector3?)null;
    }

    /// <summary>
    /// コインワープのワープ先となるコインを取得
    /// </summary>
    private Transform _GetCoinWarpTarget() {
        // 前方チェック
        Transform coin = _GetNearestCoinTransform(_coinCheckSize, _forward, _coinCheckSize.x * 0.5f);
        // 後方チェック
        if (coin == null)
            coin = _GetNearestCoinTransform(_coinCheckSize * _otherCheckRate, -_forward, _coinCheckSize.x * _otherCheckRate * 0.5f);
        // 上方チェック
        if (coin == null)
            coin = _GetNearestCoinTransform(_coinCheckSize * _otherCheckRate, Vector2.up, _coinCheckSize.y * _otherCheckRate * 0.5f);
        // 下方チェック
        if (coin == null)
            coin = _GetNearestCoinTransform(_coinCheckSize * _otherCheckRate, Vector2.down, _coinCheckSize.y * _otherCheckRate * 0.5f);
        return coin;
    }

    /// <summary>
    ///　コインワープが出来るか確認（指定方向）
    /// </summary>
    public Vector3? GetCoinInWarpDirection(eWarpDirection direction) {
        Vector2 dir = direction switch {
            eWarpDirection.Up => Vector2.up,
            eWarpDirection.UpRight => new Vector2(1f, 1f).normalized,
            eWarpDirection.Right => Vector2.right,
            eWarpDirection.DownRight => new Vector2(1f, -1f).normalized,
            eWarpDirection.Down => Vector2.down,
            eWarpDirection.DownLeft => new Vector2(-1f, -1f).normalized,
            eWarpDirection.Left => Vector2.left,
            eWarpDirection.UpLeft => new Vector2(-1f, 1f).normalized,
            _ => Vector2.zero
        };
        if (dir == Vector2.zero) return null;
        return _GetNearestCoin(_coinCheckSize, dir, _coinCheckSize.magnitude * 0.5f);
    }

    /// <summary>
    /// 一番近くのコインを取得
    /// </summary>
    private Vector3? _GetNearestCoin(Vector2 check_size, Vector2 direction, float distance) {
        var coin = _GetNearestCoinTransform(check_size, direction, distance);
        return coin != null ? coin.position : (Vector3?)null;
    }

    /// <summary>
    /// 一番近くのコインを取得（同じコインかどうかを判別できるようTransformで返す）
    /// </summary>
    private Transform _GetNearestCoinTransform(Vector2 check_size, Vector2 direction, float distance) {
        Vector3 origin = transform.position;
        
        // コイン検知
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, check_size, 0, direction, distance, _coinLayer);

        float min_dist = Mathf.Infinity;
        Transform best_coin = null;

        // 検知したコインの中で一番近いものを取得
        foreach (var hit in hits) {
            float dist = Vector3.Distance(origin, hit.point);
            if (dist < min_dist) {
                min_dist = dist;
                best_coin = hit.collider.transform;
            }
        }
        return best_coin;
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
        
        // 角度を0〜360度に正規化
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

    /// <summary>
    /// ワープ方向の水平成分の符号を取得（右方向系: 1, 左方向系: -1, 上下のみ: 0）
    /// </summary>
    private int _GetHorizontalSign(eWarpDirection direction) {
        return direction switch {
            eWarpDirection.Right or eWarpDirection.UpRight or eWarpDirection.DownRight => 1,
            eWarpDirection.Left or eWarpDirection.UpLeft or eWarpDirection.DownLeft => -1,
            _ => 0
        };
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
