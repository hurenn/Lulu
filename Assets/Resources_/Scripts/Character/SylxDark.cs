using System.Collections;
using UnityEngine;

/// <summary>
/// 闇のゼルクスが生み出す敵。生み出された時に決められた、2つの動きのどちらかで動く。地形はすり抜ける
/// ①指定された2点の間を、片方からもう片方へ移動し、着いたら消える
/// ②弧を描きながらプレイヤーを弱く追尾し、生み出されてから一定時間で消える
/// </summary>
public class SylxDark : Enemy_Base {
    public override bool IsBossSide => true; // ボスが生み出した敵(氷の必殺技ゲージが多く溜まる)

    public enum eMovePattern {
        Path,   // ①2点の間を移動する
        Chase,  // ②プレイヤーを弱く追尾する
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
            return;
        }
        bool is_end = _pattern == eMovePattern.Path ? _UpdatePath(delta) : _UpdateChase(delta);
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
