using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

public class Enemy_Base : Character_Base
{
    // 経験値
    [SerializeField] protected int _exp = 1;
    [SerializeField] protected CoinSpawner _coinSpawner;

    // ワープチェック用のコンポーネント
    [SerializeField] private WarpChecker _leftWarpChecker;
    [SerializeField] private WarpChecker _rightWarpChecker;

    // ロックオンマーカー
    [SerializeField] private Animator _lockonMarkerAnim;
    public bool isAutoLockOn = true;
    // ロックオンできるかどうか(状態によってロックオンさせない敵がオーバーライドする)
    public virtual bool CanLockOn => true;

    [SerializeField] private DamageZone _damageZone;

    [SerializeField] protected GameObject _dieExplosion = null;
    // 被弾演出(Feel)
    [SerializeField] private MMF_Player _hitFeedback;
    // 被弾時に見た目へ重ねる光(見た目のオブジェクトはVariantごとに違うため、実行時に_spriteの子へ移す)
    [SerializeField] private SpriteFlashOverlay _hitFlash;
    // 撃破演出(Feel)。爆発と同時に再生する(独自の撃破処理を持つボスはEnemy_Base.Dieを通らないため鳴らない)
    [SerializeField] private MMF_Player _dieFeedback;
    public System.Action OnDowned = null;
    public System.Action OnSeriously = null;
    public System.Action OnHalfHP = null; // HPが半分を切った時(ダウン演出を伴わないボス用)
    public System.Action OnDied = null;
    public System.Action OnDieEnded = null;

    // 次の行動までの時間
    protected float _nextActionTime = 0f;
    protected float _currentActionTime = 0f;

    // HPゲージ（Inspectorでアサインしなければ非表示のまま）
    [SerializeField] private Slider _hpGauge;
    private Coroutine _hideGaugeCoroutine;

    protected override void _Setup() {
        base._Setup();
        if (_lockonMarkerAnim != null) {
            _lockonMarkerAnim.gameObject.SetActive(false);
        }
        if (_hitFlash != null) {
            _hitFlash.Attach(_sprite);
        }
        if (_hpGauge != null && _charaParam != null) {
            _charaParam.OnHPChanged += _UpdateHPGauge;
            _UpdateHPGauge(_charaParam.CurrentHP);
        }
    }

    /// <summary>
    /// HPゲージの表示更新
    /// </summary>
    private void _UpdateHPGauge(int current_hp) {
        if (_hpGauge == null || _charaParam == null) {
            return;
        }
        _hpGauge.value = (float)current_hp / _charaParam.MaxHP;

        if (_hideGaugeCoroutine != null) {
            StopCoroutine(_hideGaugeCoroutine);
            _hideGaugeCoroutine = null;
        }

        if (current_hp <= 0) {
            // HPが0になったら1秒後に非表示にする
            _hpGauge.gameObject.SetActive(true);
            _hideGaugeCoroutine = StartCoroutine(_HideGaugeDelayed());
        } else {
            // HPが全快の時は非表示、ダメージを受けたら表示
            _hpGauge.gameObject.SetActive(current_hp < _charaParam.MaxHP);
        }
    }

    private IEnumerator _HideGaugeDelayed() {
        yield return new WaitForSeconds(1.0f);
        _hpGauge.gameObject.SetActive(false);
        _hideGaugeCoroutine = null;
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        bool result = base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);
        // ダメージ0(演出中でダメージを無効化している等)の時は、効いたように見せない
        if (result && damage > 0 && _hitFeedback != null) {
            _hitFeedback.PlayFeedbacks();
        }
        return result;
    }

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
        if (_dieFeedback != null) {
            _dieFeedback.PlayFeedbacks();
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
                if (_HasWarpPoint(_leftWarpChecker)) {
                    return _leftWarpChecker;
                }
                if (_HasWarpPoint(_rightWarpChecker)) {
                    return _rightWarpChecker;
                }
                return null;
            }
            if (_HasWarpPoint(_leftWarpChecker)) {
                return _leftWarpChecker;
            }
        } else if (warp_direction == WarpControl.eWarpDirection.Right) {
            if (is_other_check) {
                // 反対側もチェック
                if (_HasWarpPoint(_rightWarpChecker)) {
                    return _rightWarpChecker;
                }
                if (_HasWarpPoint(_leftWarpChecker)) {
                    return _leftWarpChecker;
                }
                return null;
            }
            if (_HasWarpPoint(_rightWarpChecker)) {
                return _rightWarpChecker;
            }
        }
        return null;
    }

    /// <summary>
    /// WarpCheckerが割り当てられており、かつワープ可能な地点を持っているか
    /// （未アサインの敵プレハブでもNREにならないようにする）
    /// </summary>
    private bool _HasWarpPoint(WarpChecker checker) {
        return checker != null && checker.GetWarpPoint().HasValue;
    }

    /// <summary>
    /// ロックオン表示切替
    /// </summary>
    public void EnableLockOnMarker(bool enable) {
        if(_lockonMarkerAnim == null) {
            return;
        }
        _lockonMarkerAnim.gameObject.SetActive(enable);
        if(enable) {
            _lockonMarkerAnim.Play("LockOnMarker_Enable", 0, 0);
        }
    }
}
