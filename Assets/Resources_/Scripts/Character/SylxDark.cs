using System.Collections;
using UnityEngine;

/// <summary>
/// 闇のゼルクスが生み出す敵。生み出された時に決められた、2つの動きのどちらかで動く。地形はすり抜ける
/// ①指定された2点の間を、片方からもう片方へ移動し、着いたら消える
/// ②弧を描きながらプレイヤーを弱く追尾し、生み出されてから一定時間で消える
/// ③(追いかけっこ中)プレイヤーの横の動きに付いていきながら、少し下がってから画面の左端へ飛び、着いたら消える
/// </summary>
public class SylxDark : Enemy_Base {
    public override bool IsBossSide => true; // ボスが生み出した敵(氷の必殺技ゲージが多く溜まる)

    public enum eMovePattern {
        Path,   // ①2点の間を移動する
        Chase,  // ②プレイヤーを弱く追尾する
        Escort, // ③追いかけっこ中のプレイヤーに付いて動き、少し下がってから画面の左端へ飛ぶ
    }

    [Header("① 2点の間を移動する")]
    [Tooltip("移動する速さ")]
    [SerializeField] private float _pathSpeed = 4.0f;

    [Header("② プレイヤーを弱く追尾する")]
    [Tooltip("飛ぶ速さ")]
    [SerializeField] private float _chaseSpeed = 6.0f;
    [Tooltip("飛び始めの向きを、プレイヤーへの向きからずらす角度の最小値(度)。ずらした分だけ弧を描く。1匹ごとに最小値〜最大値からランダムに決まる")]
    [SerializeField] private float _chaseArcAngleMin = 45.0f;
    [Tooltip("飛び始めの向きを、プレイヤーへの向きからずらす角度の最大値(度)")]
    [SerializeField] private float _chaseArcAngleMax = 90.0f;
    [Tooltip("プレイヤーの方へ向きを変える速さ(度/秒)。小さいほど追尾が弱く、大きな弧を描く")]
    [SerializeField] private float _chaseTurnSpeed = 60.0f;
    [Tooltip("生み出されてから消えるまでの時間(秒)")]
    [SerializeField] private float _chaseLifeTime = 5.0f;

    [Header("③ 追いかけっこ中(プレイヤーの横の動きに付いていき、画面の上での横の動きは一定にする)")]
    [Tooltip("生み出されてから後ろ(右)に下がる距離")]
    [SerializeField] private float _escortBackDistance = 2.0f;
    [Tooltip("後ろに下がる時間(秒)。この間に、上下はプレイヤーの高さ±指定の位置へ動く")]
    [SerializeField] private float _escortBackTime = 0.5f;
    [Tooltip("下がった後、画面の左端まで飛ぶ時間(秒)。この間、上下には動かない")]
    [SerializeField] private float _escortFlyTime = 2.0f;
    [Tooltip("プレイヤーがワープした時、一緒に瞬間移動せずにその場に留まってから、元の動きに戻る速さ")]
    [SerializeField] private float _escortWarpFollowSharpness = 6.0f;

    [Header("攻撃を受けた時")]
    [Tooltip("攻撃を受けた時に、動きを止める時間(秒)")]
    [SerializeField] private float _hitStopTime = 0.5f;

    [Header("消える時")]
    [Tooltip("移動し終わって消える時に出すエフェクト(未設定なら出さない)")]
    [SerializeField] private GameObject _vanishEffect;

    private eMovePattern _pattern = eMovePattern.Chase;
    private bool _isSetup = false;
    private Vector2 _position;
    private Vector2 _velocity;
    private Vector2 _end; // ①: 終点
    private float _elapsed = 0f;
    private float _stopTimer = 0f; // 攻撃を受けて動きを止めている残り時間
    private bool _isVanished = false;
    private Transform _playerTransform;
    // ③: プレイヤーからの横の距離(画面の上での横の位置になる)と、上下の動き
    private float _escortRelX;
    private float _escortStartRelX;
    private float _escortBackRelX;  // 下がり終えた時の距離
    private float _escortLeftRelX;  // 画面の左端の、プレイヤーからの横の距離
    private float _escortStartY;
    private float _escortTargetY;   // 下がる間に向かう高さ(生み出した時に決め、その後プレイヤーが上下に動いても変えない)
    private bool _isEscortFlying = false;
    private float _escortTime = 0f;
    // プレイヤーがワープした時に、一緒に瞬間移動してブレないようにずらす横の距離(少しずつ0に戻す)
    private float _escortWarpOffsetX = 0f;
    private WarpControl _escortWarpControl;

    /// <summary>
    /// ③追いかけっこ中のプレイヤーに付いて動く動きにする(生み出した直後に呼ぶ)
    /// 今いる位置から少し後ろ(右)に下がりながら、生み出した時のプレイヤーの高さ+y_offsetの位置へ動き、その後に画面の左端まで飛んで消える
    /// </summary>
    public void SetupEscort(float y_offset) {
        _isSetup = true;
        _pattern = eMovePattern.Escort;
        _position = transform.position;
        _playerTransform = _FindPlayer();
        _escortStartRelX = _playerTransform != null ? _position.x - _playerTransform.position.x : 0f;
        _escortRelX = _escortStartRelX;
        _escortStartY = _position.y;
        _escortTargetY = (_playerTransform != null ? _playerTransform.position.y : _position.y) + y_offset;
        _isEscortFlying = false;
        _escortTime = 0f;
        _escortWarpOffsetX = 0f;
        _velocity = Vector2.left; // プレイヤーの方(左)を向いたまま動く
        if (_playerTransform != null) {
            _escortWarpControl = _playerTransform.GetComponentInChildren<WarpControl>(true);
            if (_escortWarpControl != null) _escortWarpControl.onWarped += _OnPlayerWarped;
        }
        _ApplyPosition();
    }

    // プレイヤーがワープした瞬間: 一緒に瞬間移動しないよう、今いる位置に留まるだけ横の距離をずらす
    private void _OnPlayerWarped(Vector2 from, Vector2 to) {
        _escortWarpOffsetX -= to.x - from.x;
    }

    private void OnDestroy() {
        if (_escortWarpControl != null) _escortWarpControl.onWarped -= _OnPlayerWarped;
    }

    /// <summary>
    /// ①2点の間を移動する動きにする(生み出した直後に呼ぶ)。始点へ移動してから動き始める
    /// </summary>
    public void SetupPath(Vector2 start, Vector2 end) {
        _isSetup = true;
        _pattern = eMovePattern.Path;
        _position = start;
        _end = end;
        Vector2 dir = end - start;
        _velocity = dir.sqrMagnitude > 0.0001f ? dir.normalized * _pathSpeed : Vector2.zero;
        _ApplyPosition();
    }

    /// <summary>
    /// ②今いる位置から、弧を描きながらプレイヤーを弱く追尾する動きにする(生み出した直後に呼ぶ)
    /// </summary>
    public void SetupChase() {
        _isSetup = true;
        _pattern = eMovePattern.Chase;
        _position = transform.position;
        _playerTransform = _FindPlayer();
        // プレイヤーへの向きから左右どちらかにずらして飛び始め、少しずつプレイヤーの方へ向きを変えて弧を描く
        Vector2 to_player = _playerTransform != null ? (Vector2)_playerTransform.position - _position : Vector2.down;
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = Mathf.Atan2(to_player.y, to_player.x) * Mathf.Rad2Deg + Random.Range(_chaseArcAngleMin, _chaseArcAngleMax) * side;
        _velocity = _AngleToVector(angle) * _chaseSpeed;
        _ApplyPosition();
    }

    protected override void _Setup() {
        base._Setup();
        // シーンに直接置かれた時など、動きが決められていなければプレイヤーを追尾する
        if (!_isSetup) SetupChase();
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        // 被弾で吹き飛ばされず、決められた動きを続ける
        _rb.linearVelocity = Vector2.zero;
        if (_isDead) return;

        float delta = Time.deltaTime;
        _elapsed += delta;
        // 攻撃を受けた直後は動きを止める(消えるまでの時間は進める)
        if (_stopTimer > 0f) {
            _stopTimer -= delta;
            if (_pattern == eMovePattern.Chase && _elapsed >= _chaseLifeTime) _Vanish();
            // ③は止まっている間も、プレイヤーの横の動きには付いていく(画面の上で止まって見える)
            if (_pattern == eMovePattern.Escort && _playerTransform != null) {
                _position.x = _playerTransform.position.x + _escortRelX + _UpdateEscortWarpOffset(delta);
                _rb.MovePosition(_position);
            }
            return;
        }
        bool is_end;
        switch (_pattern) {
            case eMovePattern.Path:
                is_end = _UpdatePath(delta);
                break;
            case eMovePattern.Escort:
                is_end = _UpdateEscort(delta);
                break;
            default:
                is_end = _UpdateChase(delta);
                break;
        }
        if (is_end) {
            _Vanish();
            return;
        }
        _rb.MovePosition(_position);
        _UpdateFacing();
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        bool result = base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
        if (result) _stopTimer = _hitStopTime;
        return result;
    }

    // ①: 終点へまっすぐ進む。着いたら終わり
    private bool _UpdatePath(float delta) {
        Vector2 next = _position + _velocity * delta;
        // 終点を通り過ぎたら(進む向きと終点への向きが逆になったら)着いたとする
        if (Vector2.Dot(_end - next, _velocity) <= 0f) {
            _position = _end;
            return true;
        }
        _position = next;
        return false;
    }

    /// <summary>
    /// ③: プレイヤーの横の動きに付いていきながら、少し後ろ(右)に下がり、その後に画面の左端まで飛ぶ。着いたら終わり
    /// (プレイヤーからの横の距離だけを動かすので、プレイヤーの速さに関わらず、画面の上での横の動きは一定になる)
    /// </summary>
    private bool _UpdateEscort(float delta) {
        if (_playerTransform == null) return true;
        Vector2 player_position = _playerTransform.position;
        _escortTime += delta; // 攻撃を受けて止まっている間は進めない
        if (_escortTime < _escortBackTime) {
            // 後ろに下がりながら(だんだん遅くなる)、プレイヤーの高さ±指定の位置へ動く
            float rate = Mathf.Clamp01(_escortTime / Mathf.Max(_escortBackTime, 0.001f));
            float ease = 1f - (1f - rate) * (1f - rate);
            _escortRelX = _escortStartRelX + _escortBackDistance * ease;
            _position.y = Mathf.Lerp(_escortStartY, _escortTargetY, ease);
        } else {
            if (!_isEscortFlying) {
                // 飛び始める時に、画面の左端(の外)までの距離を決める
                _isEscortFlying = true;
                _escortBackRelX = _escortRelX;
                _escortLeftRelX = _GetScreenLeftX(player_position.x) - player_position.x - _GetCharacterHalfWidth();
            }
            // 画面の左端まで一定の速さで飛ぶ(上下には動かない)
            float rate = (_escortTime - _escortBackTime) / Mathf.Max(_escortFlyTime, 0.001f);
            _escortRelX = Mathf.Lerp(_escortBackRelX, _escortLeftRelX, rate);
            if (rate >= 1f) return true;
        }
        _position.x = player_position.x + _escortRelX + _UpdateEscortWarpOffset(delta);
        return false;
    }

    // ワープでずらした横の距離を、少しずつ0に戻す(戻した後の値を返す)
    private float _UpdateEscortWarpOffset(float delta) {
        _escortWarpOffsetX = Mathf.Lerp(_escortWarpOffsetX, 0f, 1f - Mathf.Exp(-_escortWarpFollowSharpness * delta));
        return _escortWarpOffsetX;
    }

    // 今カメラに映っている範囲の左端(Z=0の平面上)。カメラが無ければ、指定した位置
    private static float _GetScreenLeftX(float fallback) {
        var cam = Camera.main;
        if (cam == null) return fallback;
        return cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, -cam.transform.position.z)).x;
    }

    private float _GetCharacterHalfWidth() {
        var size = GetCharacterSize();
        return size.x > 0f ? size.x * 0.5f : 0.5f;
    }

    // ②: プレイヤーの方へ少しずつ向きを変えながら飛ぶ。一定時間で終わり
    private bool _UpdateChase(float delta) {
        if (_playerTransform == null) _playerTransform = _FindPlayer();
        if (_playerTransform != null) {
            Vector2 to_player = (Vector2)_playerTransform.position - _position;
            if (to_player.sqrMagnitude > 0.0001f) {
                float current = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                float target = Mathf.Atan2(to_player.y, to_player.x) * Mathf.Rad2Deg;
                _velocity = _AngleToVector(Mathf.MoveTowardsAngle(current, target, _chaseTurnSpeed * delta)) * _chaseSpeed;
            }
        }
        _position += _velocity * delta;
        return _elapsed >= _chaseLifeTime;
    }

    private void _ApplyPosition() {
        transform.position = _position;
        _UpdateFacing();
    }

    /// <summary>
    /// 倒されずに消える(闇のゼルクスの演出で、出ているSylxDarkを片付ける時など)
    /// </summary>
    public void Vanish() {
        if (_isDead) return;
        _Vanish();
    }

    // 倒されずに消える(コインは出さない)
    private void _Vanish() {
        if (_isVanished) return;
        _isVanished = true;
        if (_vanishEffect != null) Instantiate(_vanishEffect, transform.position, Quaternion.identity);
        ParticleTrail.DetachAll(gameObject); // 靄の軌跡は残して、自然に消えるようにする
        Destroy(gameObject);
    }

    protected override IEnumerator Die() {
        // 倒された時は、靄の軌跡を切り離して残す
        ParticleTrail.DetachAll(gameObject);
        yield return base.Die();
    }

    private void _UpdateFacing() {
        if (Mathf.Abs(_velocity.x) > 0.01f) _isRight = _velocity.x > 0f;
    }

    private static Transform _FindPlayer() {
        var controller = PlayerCharacterManager.Controller;
        return controller != null ? controller.Character.transform : null;
    }

    private static Vector2 _AngleToVector(float angle) {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
