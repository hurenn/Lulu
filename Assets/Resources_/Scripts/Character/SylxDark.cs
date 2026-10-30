using UnityEngine;

/// <summary>
/// 闇のゼルクスが生み出す敵。生み出された時に決められた、2つの動きのどちらかで動く。地形はすり抜ける
/// ①指定された2点の間を、片方からもう片方へ移動し、着いたら消える
/// ②発射した時点のプレイヤーの位置へ向かって大きく弧を描いて突撃し、少し通り過ぎたところで消える(追尾はしない)
/// </summary>
public class SylxDark : Enemy_Base {
    public override bool IsBossSide => true; // ボスが生み出した敵(氷の必殺技ゲージが多く溜まる)

    public enum eMovePattern {
        Path,   // ①2点の間を移動する
        Charge, // ②プレイヤーのいた位置へ弧を描いて突撃する
    }

    [Header("① 2点の間を移動する")]
    [Tooltip("移動する速さ")]
    [SerializeField] private float _pathSpeed = 4.0f;

    [Header("② プレイヤーのいた位置へ突撃する")]
    [Tooltip("突撃する速さ")]
    [SerializeField] private float _chargeSpeed = 8.0f;
    [Tooltip("弧のふくらみの最小値(目標までの距離に対する割合)。1匹ごとに最小値〜最大値からランダムに決まる")]
    [SerializeField] private float _chargeArcRateMin = 0.3f;
    [Tooltip("弧のふくらみの最大値(目標までの距離に対する割合)")]
    [SerializeField] private float _chargeArcRateMax = 0.6f;
    [Tooltip("目標の位置を通り過ぎてから消えるまでの距離")]
    [SerializeField] private float _chargeOvershoot = 2.0f;

    [Header("消える時")]
    [Tooltip("移動し終わって消える時に出すエフェクト(未設定なら出さない)")]
    [SerializeField] private GameObject _vanishEffect;

    private eMovePattern _pattern = eMovePattern.Charge;
    private bool _isSetup = false;
    private Vector2 _position;
    private Vector2 _velocity;
    // ①: 終点 / ②: 弧(2次ベジェ曲線)の始点・制御点・終点(目標の位置)
    private Vector2 _start;
    private Vector2 _control;
    private Vector2 _end;
    private float _arcDuration = 0f; // ②: 弧の上を進むのにかかる時間
    private float _overshootRemain = 0f; // ②: 目標を通り過ぎてから、あと進む距離
    private bool _isPassedTarget = false; // ②: 目標に着いた(弧を進み終えた)
    private float _elapsed = 0f;

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
    /// ②今いる位置から、目標の位置へ弧を描いて突撃する動きにする(生み出した直後に呼ぶ)
    /// </summary>
    public void SetupCharge(Vector2 target) {
        _isSetup = true;
        _pattern = eMovePattern.Charge;
        _position = transform.position;
        _start = _position;
        _end = target;

        // 始点と目標を結ぶ線から、左右どちらかへふくらませた点を制御点にする
        Vector2 chord = _end - _start;
        Vector2 normal = new Vector2(-chord.y, chord.x);
        float side = Random.value < 0.5f ? -1f : 1f;
        _control = (_start + _end) * 0.5f + normal * Random.Range(_chargeArcRateMin, _chargeArcRateMax) * side;

        // 弧の長さの近似(弦と、制御点を通る折れ線の平均)から、一定の速さで進む時間を決める
        float length = (chord.magnitude + Vector2.Distance(_start, _control) + Vector2.Distance(_control, _end)) * 0.5f;
        _arcDuration = Mathf.Max(0.01f, length / Mathf.Max(0.01f, _chargeSpeed));
        _overshootRemain = _chargeOvershoot;
        _isPassedTarget = false;
        _velocity = _GetArcTangent(0f) * _chargeSpeed;
        _ApplyPosition();
    }

    protected override void _Setup() {
        base._Setup();
        // シーンに直接置かれた時など、動きが決められていなければプレイヤーへ突撃する
        if (!_isSetup) {
            var player = _FindPlayer();
            SetupCharge(player != null ? (Vector2)player.position : (Vector2)transform.position + Vector2.down);
        }
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();
        // 被弾で吹き飛ばされず、決められた動きを続ける
        _rb.linearVelocity = Vector2.zero;
        if (_isDead) return;

        float delta = Time.deltaTime;
        _elapsed += delta;
        bool is_end = _pattern == eMovePattern.Path ? _UpdatePath(delta) : _UpdateCharge(delta);
        if (is_end) {
            _Vanish();
            return;
        }
        _rb.MovePosition(_position);
        _UpdateFacing();
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

    // ②: 弧の上を目標まで進み、その後は弧の終わりの向きのまま少し進む。進み切ったら終わり
    private bool _UpdateCharge(float delta) {
        if (_elapsed < _arcDuration) {
            float t = _elapsed / _arcDuration;
            Vector2 next = _GetArcPoint(t);
            if (delta > 0f) _velocity = (next - _position) / delta;
            _position = next;
            return false;
        }
        if (!_isPassedTarget) {
            // 目標に着いた瞬間: 弧の終わりの向きで進み続ける
            _isPassedTarget = true;
            _position = _end;
            _velocity = _GetArcTangent(1f) * _chargeSpeed;
        }
        float move = _chargeSpeed * delta;
        _position += _velocity.normalized * Mathf.Min(move, _overshootRemain);
        _overshootRemain -= move;
        return _overshootRemain <= 0f;
    }

    private Vector2 _GetArcPoint(float t) {
        float u = 1f - t;
        return u * u * _start + 2f * u * t * _control + t * t * _end;
    }

    private Vector2 _GetArcTangent(float t) {
        Vector2 tangent = 2f * (1f - t) * (_control - _start) + 2f * t * (_end - _control);
        if (tangent.sqrMagnitude < 0.0001f) tangent = _end - _start;
        return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.down;
    }

    private void _ApplyPosition() {
        transform.position = _position;
        _UpdateFacing();
    }

    // 倒されずに消える(コインは出さない)
    private void _Vanish() {
        if (_vanishEffect != null) Instantiate(_vanishEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void _UpdateFacing() {
        if (Mathf.Abs(_velocity.x) > 0.01f) _isRight = _velocity.x > 0f;
    }

    private static Transform _FindPlayer() {
        var controller = PlayerCharacterManager.Controller;
        return controller != null ? controller.Character.transform : null;
    }
}
