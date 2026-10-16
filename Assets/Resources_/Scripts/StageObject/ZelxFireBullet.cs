using System;
using UnityEngine;

/// <summary>
/// 炎のゼルクスが発射する弾。プールで再利用する
/// 指定方向へ直進し、画面外に出る・プレイヤーに当たる・WaterBulletを受けるとプールへ返却される
/// 設定により、氷の能力のコンボ攻撃(Slash1～3)で跳ね返す・止めることができる
/// </summary>
public class ZelxFireBullet : MonoBehaviour {
    // Slashを受けた時の反応
    public enum eSlashReaction {
        None,            // 反応しない
        Reflect,         // どの段のSlashでも跳ね返る
        StopThenReflect, // _reflectComboStep未満の段では止まり、_reflectComboStep以上の段で跳ね返る
    }

    [SerializeField] private float _speed = 6.0f;
    [SerializeField] private DamageZone _damageZone;
    [SerializeField] private bool _isFlipByDirection = false; // trueなら進行方向が左の時に見た目を左右反転する(左右に飛ばす弾用)

    [Header("Slash被弾時")]
    [SerializeField] private eSlashReaction _slashReaction = eSlashReaction.None;
    [SerializeField] private int _reflectComboStep = 3;        // StopThenReflectの時、この段数以上のSlashで跳ね返る
    [SerializeField] private AudioClip _seReflect;             // 跳ね返った時の効果音
    [SerializeField] private AudioClip _seStop;                // 止められた時の効果音
    [SerializeField] private float _seSlashVolume = 1.0f;
    [SerializeField] private float _stopShakeDuration = 0.3f;  // 止められた時に震える時間
    [SerializeField] private float _stopShakeAmplitude = 0.1f; // 止められた時に震える幅
    [SerializeField] private float _stopResumeTime = 1.0f;     // 止められてから、跳ね返されずにこの時間が経つと前進を再開する
    [SerializeField] private bool _isKnockdownOnReflectHit = false; // trueなら跳ね返された後に当たった相手を吹き飛ばしてダウンさせ、弾は弾けて消える

    [Header("WaterBullet被弾時・弾けて消える時")]
    [SerializeField] private HitEffect _waterHitEffectPrefab;  // ダメージエフェクト
    [SerializeField] private HitEffect.eType _waterHitEffectType = HitEffect.eType.Normal;
    [SerializeField] private float _waterHitEffectSize = 1.0f;
    [SerializeField] private AudioClip _seWaterHit;            // 効果音
    [SerializeField] private float _seWaterHitVolume = 1.0f;

    private const float _FAR_VIEWPORT_MARGIN = 0.5f; // 画面外でもこの距離(画面サイズ比)までは返却を待つ

    private Vector2 _direction = Vector2.down;
    private Action<ZelxFireBullet> _onRelease;
    private bool _isReleased = false;
    private bool _isHolding = false; // true: 発射前の待機中(チャージ中)。移動も画面外判定もしない
    private bool _isStopped = false; // true: Slashで止められている。移動しない(画面外判定は行う)
    private bool _isReflected = false; // true: Slashで跳ね返された(プレイヤーには当たらず、撃ったキャラに当たる)
    private SlashAttackInfo _lastSlash; // 最後に受けたSlash(1回の攻撃の複数コライダーで何度も反応しないようにする)
    private Vector3 _stopPosition;      // 止められた位置(震える中心)
    private float _stopShakeTimer = 0f;
    private float _stopTimer = 0f;      // 前進を再開するまでの残り時間

    /// <summary>
    /// Slashで跳ね返されたかどうか
    /// </summary>
    public bool IsReflected => _isReflected;

    private void Awake() {
        // プレイヤーに当たったら消える(DamageZoneの_isHitDestroyで判定するため、光の能力で回避された時は消えない)
        _damageZone?.SetHitDestroyCallback(_Release);
        _damageZone?.Setup(_OnHitCharacter);
    }

    // 当たった相手への追加処理(ダメージ処理の直前に呼ばれる)
    private void _OnHitCharacter(IDamageable character) {
        if (_isReflected && _isKnockdownOnReflectHit && character is IKnockdownTarget target) {
            target.Knockdown(_direction);
            _Burst(); // 弾けて消える
        }
    }

    /// <summary>
    /// 発射（プールから取り出すたびに呼ぶ）
    /// </summary>
    /// <param name="direction">進行方向</param>
    /// <param name="on_release">返却時のコールバック</param>
    /// <param name="owner">発射したキャラ(この弾が当たらないようにする)</param>
    public void Fire(Vector2 direction, Action<ZelxFireBullet> on_release, IDamageable owner = null) {
        _direction = direction.normalized;
        _onRelease = on_release;
        _damageZone?.SetIgnoreTarget(owner);
        _ApplyFlip();
        // チャージ中から続けて発射する場合は、チャージ中に当たった相手にも再度当たるよう記録を消去する
        _isHolding = false;
        _ResetState();
    }

    /// <summary>
    /// 発射せずにその場で待機させる（チャージ中など。位置は呼び出し側で動かす）。後でFireを呼ぶと発射される
    /// 待機中も当たり判定はあり、WaterBulletを受けると消える
    /// </summary>
    /// <param name="on_release">返却時のコールバック</param>
    /// <param name="owner">発射したキャラ(この弾が当たらないようにする)</param>
    public void Hold(Action<ZelxFireBullet> on_release, IDamageable owner = null) {
        _onRelease = on_release;
        _damageZone?.SetIgnoreTarget(owner);
        _isHolding = true;
        _ResetState();
    }

    // プールから再利用した時に、前回の状態を引き継がないようにする
    private void _ResetState() {
        _isReleased = false;
        _isStopped = false;
        _isReflected = false;
        _lastSlash = null;
        _stopShakeTimer = 0f;
        _damageZone?.ResetHitCharacters();
    }

    // 進行方向に合わせて見た目を左右反転する
    private void _ApplyFlip() {
        if (!_isFlipByDirection) return;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (_direction.x < 0f ? -1f : 1f);
        transform.localScale = scale;
    }

    /// <summary>
    /// 返却済み(プールへ戻った・破棄された)かどうか
    /// </summary>
    public bool IsReleased => _isReleased;

    /// <summary>
    /// 発射せずに消す（チャージ中に撃ち手が倒された時など）
    /// </summary>
    public void Release() {
        _Release();
    }

    private void Update() {
        if (_isHolding) return;
        if (_isStopped) {
            _UpdateStopShake();
            // 跳ね返されないまま時間が経ったら、止められた位置から元の方向へ前進を再開する
            _stopTimer -= Time.deltaTime;
            if (_stopTimer <= 0f) {
                _isStopped = false;
                transform.position = _stopPosition;
            }
        } else {
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        }
        _CheckOffScreen();
    }

    // 画面外に出たらプールへ返却
    // 画面端から発射された弾が発射直後に消えないよう、画面から遠ざかる方向へ出た時だけ返却する
    private void _CheckOffScreen() {
        var cam = Camera.main;
        if (cam == null) return;

        var viewport_pos = cam.WorldToViewportPoint(transform.position);
        bool is_out_left = viewport_pos.x < 0f && _direction.x <= 0f;
        bool is_out_right = viewport_pos.x > 1f && _direction.x >= 0f;
        bool is_out_bottom = viewport_pos.y < 0f && _direction.y <= 0f;
        bool is_out_top = viewport_pos.y > 1f && _direction.y >= 0f;
        // 画面へ向かっていても、カメラに置いていかれて大きく離れた弾は返却する(残り続けないように)
        bool is_far = viewport_pos.x < -_FAR_VIEWPORT_MARGIN || viewport_pos.x > 1f + _FAR_VIEWPORT_MARGIN
            || viewport_pos.y < -_FAR_VIEWPORT_MARGIN || viewport_pos.y > 1f + _FAR_VIEWPORT_MARGIN;
        if (is_out_left || is_out_right || is_out_bottom || is_out_top || is_far) {
            _Release();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (_isReleased) return;

        var slash = other.GetComponentInParent<SlashAttackInfo>();
        if (slash != null) {
            _OnSlashHit(slash);
            return;
        }

        if (other.GetComponentInParent<WaterBullet>() == null) return;

        // WaterBulletを受けたら弾けて消える
        _Burst();
    }

    // ダメージエフェクトと効果音を出して、弾けて消える
    private void _Burst() {
        if (_isReleased) return;
        if (_waterHitEffectPrefab != null) {
            var effect = EffectPool.Instance.Spawn(_waterHitEffectPrefab.gameObject, transform.position);
            if (effect != null) {
                foreach (var hit_effect in effect.GetComponentsInChildren<HitEffect>()) {
                    hit_effect.Setup(_waterHitEffectType, _waterHitEffectSize);
                }
            }
        }
        if (_seWaterHit != null) {
            AudioUtil.PlayClipAtPointUnclamped(_seWaterHit, transform.position, _seWaterHitVolume);
        }
        _Release();
    }

    // Slashを受けた時、設定に応じて止まる・跳ね返る
    private void _OnSlashHit(SlashAttackInfo slash) {
        // チャージ中(発射前)は反応しない
        if (_slashReaction == eSlashReaction.None || _isHolding) return;
        if (slash == _lastSlash) return;
        _lastSlash = slash;

        if (_slashReaction == eSlashReaction.StopThenReflect && slash.ComboStep < _reflectComboStep) {
            // その場で止まり、小さく震える(止まっている時に再び受けた場合も震え直す)
            if (!_isStopped) {
                _isStopped = true;
                _stopPosition = transform.position;
            }
            _stopShakeTimer = _stopShakeDuration;
            _stopTimer = _stopResumeTime; // 次のSlashを受けたら数え直す
            if (_seStop != null) AudioUtil.PlayClipAtPointUnclamped(_seStop, transform.position, _seSlashVolume);
            return;
        }
        _Reflect();
    }

    // 止められた位置を中心に、徐々に小さくなりながら震える
    private void _UpdateStopShake() {
        if (_stopShakeTimer <= 0f) {
            transform.position = _stopPosition;
            return;
        }
        _stopShakeTimer -= Time.deltaTime;
        float rate = _stopShakeDuration > 0f ? Mathf.Clamp01(_stopShakeTimer / _stopShakeDuration) : 0f;
        transform.position = _stopPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * _stopShakeAmplitude * rate);
    }

    // プレイヤーと反対方向へ真っすぐ跳ね返る。跳ね返った後はプレイヤーに当たらず、撃ったキャラに当たる
    private void _Reflect() {
        var player = PlayerCharacterManager.Current;
        float direction_x;
        if (player != null && !Mathf.Approximately(transform.position.x, player.transform.position.x)) {
            direction_x = transform.position.x > player.transform.position.x ? 1f : -1f;
        } else {
            direction_x = _direction.x > 0f ? -1f : 1f; // プレイヤーと重なっている時は来た方向へ返す
        }
        _direction = new Vector2(direction_x, 0f);
        _ApplyFlip();

        // 震えている途中なら、止められた位置から跳ね返る
        if (_isStopped) {
            transform.position = _stopPosition;
        }
        _isStopped = false;
        _isReflected = true;
        _damageZone?.SetIgnoreTarget(player);
        _damageZone?.ResetHitCharacters();

        if (_seReflect != null) AudioUtil.PlayClipAtPointUnclamped(_seReflect, transform.position, _seSlashVolume);
    }

    private void _Release() {
        if (_isReleased) return;
        _isReleased = true;

        if (_onRelease != null) {
            _onRelease(this);
        } else {
            Destroy(gameObject);
        }
    }
}
