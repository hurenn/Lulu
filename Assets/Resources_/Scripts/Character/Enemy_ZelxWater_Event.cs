using UnityEngine;

/// <summary>
/// イベント用の「水のゼルクス」
/// 逃げるプレイヤーを画面左端から狙い撃つ。攻撃は当たるが倒されることはない
/// 最初から登場し、カメラに映るギリギリの左端を維持して漂いながら、
/// 漂う→発射地点のうちプレイヤーのY座標に近い方のY座標へ移動→発射→待機 を繰り返す
/// プレイヤーがゴール範囲に触れたら非表示になる
/// 通常時はWaterShieldでFireBulletを無効化し、スタン中のみWaterShieldを外してFireBulletが当たるようになる
/// </summary>
public class Enemy_ZelxWater_Event : Enemy_Base, IDamageSourceBlocker {
    private enum eState {
        Drifting,   // 左端で漂う
        MovingToFirePoint, // 発射地点と同じY座標を目指す
        Firing,     // 発射後の待機
    }

    [Header("位置")]
    [SerializeField] private float _edgeMargin = 0.0f;   // 画面左端からの余白(0で体がちょうど画面に収まる位置)
    [SerializeField] private float _edgeSmoothTime = 0.3f; // 画角が急に変わった時に、新しい画面端の位置へ移動するまでの時間の目安
    [SerializeField] private Collider2D _goalArea;       // プレイヤーがこの範囲に触れたら非表示にする

    [Header("シールド")]
    [SerializeField] private GameObject _waterShield;    // 通常時に表示し、FireBulletを無効化する。スタン中は外す

    [Header("漂う")]
    [SerializeField] private float _driftTime = 3.0f;    // 左端で漂う時間
    [SerializeField] private float _driftSpeed = 1.5f;   // 漂う速さ
    [SerializeField] private float _driftRangeX = 0.5f;  // 左端から内側へ漂う幅(画面外へははみ出さない)
    [SerializeField] private float _driftRangeY = 1.0f;  // 上下に漂う幅

    [Header("射撃")]
    [SerializeField] private Transform[] _firePoints = new Transform[2]; // 弾の発射座標(プレイヤーのY座標に近いものを選ぶ)
    [SerializeField] private ZelxFireBullet _bulletPrefab; // 発射する弾(Zelxの位置から右へまっすぐ飛ばす)
    [SerializeField] private float _moveSpeedY = 4.0f;   // 発射地点のY座標へ向かう速さ
    [SerializeField] private float _afterFireWait = 2.0f; // 発射してから再び漂うまでの時間

    private eState _state = eState.Drifting;
    private float _stateTimer = 0f;
    private int _firePointIndex = 0;

    // 位置は「カメラ左端からのXオフセット」と「Y座標」で管理し、カメラが動いても左端を維持する
    private float _offsetX;     // 左端の定位置からのXオフセット(正で画面内側)
    private float _currentY;
    private float _driftAnchorY; // 漂い始めた時のY座標(ここを中心に上下に漂う)
    private bool _isEdgeInitialized = false;
    private float _edgeFromCameraX;  // カメラから見た画面左端の定位置のXオフセット(画角の変化に滑らかに追従する)
    private float _edgeVelocityX;    // SmoothDamp用
    private float _clampVelocityY;   // SmoothDamp用

    private bool _isStunned = false; // true: スタン中(WaterShieldを外し、FireBulletが当たる)
    public bool IsStunned => _isStunned;

    protected override void _Setup() {
        base._Setup();
        _isRight = true; // 画面左端から右向きに狙う
        _currentY = transform.position.y;
        _ChangeState(eState.Drifting); // 最初から左端に登場して漂う
        _SetStunned(false); // 通常時はWaterShieldを付けておく
    }

    /// <summary>
    /// スタンさせる。WaterShieldを外し、FireBulletが当たるようになる
    /// </summary>
    public void StartStun() {
        _SetStunned(true);
    }

    /// <summary>
    /// スタンを解除する。WaterShieldを付け直し、FireBulletが当たらなくなる
    /// </summary>
    public void EndStun() {
        _SetStunned(false);
    }

    private void _SetStunned(bool is_stunned) {
        _isStunned = is_stunned;
        if (_waterShield != null) {
            _waterShield.SetActive(!is_stunned);
        }
    }

    // 通常時(WaterShieldあり)はFireBulletを防ぐ
    // DamageZoneはこの判定で防がれると、ロックオン(ヒットコールバック)・ヒットエフェクト・ダメージ処理を行わずに弾が消える
    public bool IsBlockedDamageSource(DamageZone source) {
        return !_isStunned && source.GetComponentInParent<FireBullet>() != null;
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        // 攻撃は当たる(被弾演出・効果音あり)が、HPは減らさず倒されないようにする。吹き飛びも無効
        return base.Damage(0, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        _rb.linearVelocity = Vector2.zero;

        var cam = Camera.main;
        if (cam == null || _isDead) {
            return;
        }

        // ゴール範囲に触れたら非表示にする
        if (_IsPlayerInGoalArea()) {
            gameObject.SetActive(false);
            return;
        }

        float dt = Time.deltaTime;
        _stateTimer += dt;

        switch (_state) {
            case eState.Drifting:
                _UpdateDrift();
                if (_stateTimer >= _driftTime) {
                    // 移動を始める時点で、プレイヤーのY座標に近い発射地点を選ぶ
                    int index = _GetNearestFirePointIndex();
                    if (index >= 0) {
                        _firePointIndex = index;
                        _ChangeState(eState.MovingToFirePoint);
                    }
                }
                break;

            case eState.MovingToFirePoint: {
                    // 漂いで内側に寄った分を戻しながら、発射地点のY座標を目指す
                    float target_y = _ClampYInView(cam, _firePoints[_firePointIndex].position.y);
                    _currentY = Mathf.MoveTowards(_currentY, target_y, _moveSpeedY * dt);
                    _offsetX = Mathf.MoveTowards(_offsetX, 0f, _moveSpeedY * dt);
                    if (Mathf.Approximately(_currentY, target_y) && _offsetX <= 0f) {
                        _Fire(_firePointIndex);
                        _ChangeState(eState.Firing);
                    }
                    break;
                }

            case eState.Firing:
                if (_stateTimer >= _afterFireWait) {
                    _ChangeState(eState.Drifting);
                }
                break;
        }

        // 画面端の位置はカメラからの相対位置で管理し、カメラの移動にはそのまま付いていく。
        // 画角が急に変わって画面端の位置が変わった時は、そこへ滑らかに移動する
        float target_edge_x = _GetLeftEdgeX(cam) - cam.transform.position.x;
        float clamped_y = _ClampYInView(cam, _currentY);
        if (!_isEdgeInitialized) {
            _isEdgeInitialized = true;
            _edgeFromCameraX = target_edge_x;
            _currentY = clamped_y;
        } else {
            _edgeFromCameraX = Mathf.SmoothDamp(_edgeFromCameraX, target_edge_x, ref _edgeVelocityX, _edgeSmoothTime, Mathf.Infinity, dt);
            _currentY = Mathf.SmoothDamp(_currentY, clamped_y, ref _clampVelocityY, _edgeSmoothTime, Mathf.Infinity, dt);
        }
        float x = cam.transform.position.x + _edgeFromCameraX + _offsetX;
        _rb.MovePosition(new Vector2(x, _currentY));
    }

    private void _ChangeState(eState state) {
        _state = state;
        _stateTimer = 0f;
        if (state == eState.Drifting) {
            _driftAnchorY = _currentY;
        }
    }

    // 漂い始めの位置から途切れずに動き出すよう、どちらの軸もオフセット0から始まるサイン波で漂う
    private void _UpdateDrift() {
        float t = _stateTimer * _driftSpeed;
        _offsetX = (1f - Mathf.Cos(t)) * 0.5f * _driftRangeX; // 0～_driftRangeXの範囲で内側にだけ漂う
        _currentY = _driftAnchorY + Mathf.Sin(t * 1.3f) * _driftRangeY;
    }

    /// <summary>
    /// 弾を発射する。Zelxの位置から右へまっすぐ飛ばす
    /// </summary>
    /// <param name="fire_point_index">発射地点の番号(0が発射地点①)</param>
    private void _Fire(int fire_point_index) {
        if (_bulletPrefab == null) return;
        var bullet = Instantiate(_bulletPrefab, transform.position, Quaternion.identity);
        bullet.Fire(Vector2.right, null, this); // 返却先を渡さないため、消える時はDestroyされる。自分自身には当たらない
    }

    private bool _IsPlayerInGoalArea() {
        if (_goalArea == null) return false;
        var player = PlayerCharacterManager.Current as Player_Character;
        return player != null && _goalArea.OverlapPoint(player.transform.position);
    }

    // 発射地点のうち、プレイヤーのY座標に最も近いものの番号を返す(未設定の発射地点は除く。1つも無ければ-1)
    // プレイヤーがいない時は、自身のY座標に近いものを選ぶ
    private int _GetNearestFirePointIndex() {
        if (_firePoints == null) return -1;
        var player = PlayerCharacterManager.Current;
        float target_y = player != null ? player.transform.position.y : _currentY;

        int nearest = -1;
        float nearest_distance = float.MaxValue;
        for (int i = 0; i < _firePoints.Length; i++) {
            if (_firePoints[i] == null) continue;
            float distance = Mathf.Abs(_firePoints[i].position.y - target_y);
            if (distance < nearest_distance) {
                nearest_distance = distance;
                nearest = i;
            }
        }
        return nearest;
    }

    // 体がちょうどカメラに収まる左端のX座標
    private float _GetLeftEdgeX(Camera cam) {
        return _ViewportToWorld(cam, 0f, 0.5f).x + GetCharacterSize().x * 0.5f + _edgeMargin;
    }

    // 自身の奥行き(Z)におけるビューポート座標のワールド座標
    // (カメラは透視投影のため、orthographicSizeでは実際に映る範囲を求められない)
    private Vector3 _ViewportToWorld(Camera cam, float x, float y) {
        float depth = transform.position.z - cam.transform.position.z;
        return cam.ViewportToWorldPoint(new Vector3(x, y, depth));
    }

    // 体が画面の上下からはみ出さないようにY座標を制限する
    private float _ClampYInView(Camera cam, float y) {
        float body_half_height = GetCharacterSize().y * 0.5f;
        float min_y = _ViewportToWorld(cam, 0.5f, 0f).y + body_half_height;
        float max_y = _ViewportToWorld(cam, 0.5f, 1f).y - body_half_height;
        return min_y <= max_y ? Mathf.Clamp(y, min_y, max_y) : y;
    }

    private void OnDrawGizmosSelected() {
        // 発射地点のY座標を可視化
        if (_firePoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var point in _firePoints) {
            if (point == null) continue;
            Gizmos.DrawLine(point.position + Vector3.left * 3f, point.position + Vector3.right * 3f);
        }
    }
}
