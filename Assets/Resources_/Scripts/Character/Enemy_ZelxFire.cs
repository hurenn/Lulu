using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボス「炎のゼルクス」
/// 移動はせず、Timelineの合図(ActivateMarker)で行動を開始し、一定間隔で弾幕を発射する
/// HPが半分を切ると、扇状の弾を左から順に1発ずつ連射し続けるようになる
/// </summary>
public class Enemy_ZelxFire : Enemy_Base, IActivatable {
    [SerializeField] private AudioClip _seFinish;
    [SerializeField] private AudioSource _audioBGM;
    private float _currentBGMVolume = 0.7f;

    [Header("弾幕設定")]
    [SerializeField] private ZelxFireBullet _bulletPrefab; // 発射する弾のプレハブ(プールで再利用する)
    [SerializeField] private Transform _muzzle;            // 弾の発射位置(未設定なら自身の位置)
    [SerializeField] private float _fireInterval = 1.0f;   // 弾幕の発射間隔
    [SerializeField] private int _bulletCount = 6;         // 1回の弾幕で発射する弾数
    [SerializeField] private float _fanAngle = 120f;       // 扇の開き角度(真下を中心に左右へ振り分ける)
    [SerializeField] private AudioClip _seShoot;           // 発射時の効果音

    [Header("HP半分以下の弾幕設定")]
    [SerializeField] private float _rapidFireInterval = 0.2f; // 1発ずつ連射する間隔

    private float _fireTimer;
    private readonly Queue<ZelxFireBullet> _bulletPool = new Queue<ZelxFireBullet>();
    private bool _isActive = false; // false中はTimelineの合図があるまで行動(弾幕発射)を止めておく
    private bool _isHalfHp = false; // true: HPが半分を切り、1発ずつの連射に切り替わった
    private int _rapidFireIndex = 0; // 連射時に次に撃つ扇の位置(0が左端)
    private bool _isRapidOffsetSweep = false; // true: 偶数回目の弾幕(奇数回目の弾の間の位置を撃つ)

    protected override void _Setup() {
        base._Setup();
        _fireTimer = _fireInterval;
    }

    // Timelineの合図で行動を開始させる
    public void StartActing() {
        _isActive = true;
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false, bool ignore_invincibility = false) {
        // 移動しないボスのため、被弾時の吹き飛びは無効にする
        bool result = base.Damage(damage, Vector2.zero, invincible_time, damage_reaction_time, is_trap_damage, ignore_invincibility);

        // HPが半分を切ったら連射に切り替え、HalfHPを通知する
        if (!_isHalfHp && !_isDead && _charaParam.CurrentHP <= _charaParam.MaxHP / 2.0f) {
            _isHalfHp = true;
            _rapidFireIndex = 0;
            _isRapidOffsetSweep = false;
            _fireTimer = _rapidFireInterval;
            OnHalfHP?.Invoke();
        }

        return result;
    }

    protected override void _UpdateSpecials() {
        base._UpdateSpecials();

        // 移動はしない
        _rb.linearVelocity = Vector2.zero;

        if (_isDead || !_isActive) {
            return;
        }

        _fireTimer -= Time.deltaTime;
        if (_fireTimer > 0f) {
            return;
        }

        if (_isHalfHp) {
            _fireTimer += _rapidFireInterval;
            _FireRapid();
        } else {
            _fireTimer += _fireInterval;
            _FireBarrage();
        }
    }

    // 発射位置から下方向へ扇状に弾幕を発射する
    private void _FireBarrage() {
        if (_bulletPrefab == null || _bulletCount <= 0) return;

        if (_seShoot != null) _audioSource?.PlayOneShot(_seShoot);

        for (int i = 0; i < _bulletCount; i++) {
            _FireBullet(i);
        }
    }

    // HP半分以下: 扇の左端から順に1発ずつ撃ち、右端まで撃ったら左端に戻る
    // 奇数回目の弾幕は通常の扇の位置、偶数回目の弾幕は奇数回目の弾同士の間の位置(弾数は1発少ない)を撃つ
    private void _FireRapid() {
        if (_bulletPrefab == null || _bulletCount <= 0) return;

        if (_seShoot != null) _audioSource?.PlayOneShot(_seShoot);

        // 弾が1発しか無い扇では間の位置が存在しないため、ずらさない
        bool is_offset = _isRapidOffsetSweep && _bulletCount > 1;
        _FireBullet(is_offset ? _rapidFireIndex + 0.5f : _rapidFireIndex);

        _rapidFireIndex++;
        int sweep_count = is_offset ? _bulletCount - 1 : _bulletCount;
        if (_rapidFireIndex >= sweep_count) {
            _rapidFireIndex = 0;
            _isRapidOffsetSweep = !_isRapidOffsetSweep;
        }
    }

    // 扇のindex番目(0が左端、小数なら隣の弾との間)の方向へ弾を1発撃つ
    private void _FireBullet(float index) {
        // 真下(-90度)を中心に、_fanAngleの範囲へ等間隔に並べる
        float t = _bulletCount > 1 ? index / (_bulletCount - 1) : 0.5f;
        float angle = (-90f - _fanAngle * 0.5f + _fanAngle * t) * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        Vector3 spawnPos = _muzzle != null ? _muzzle.position : transform.position;
        var bullet = _GetBullet(spawnPos);
        bullet.Fire(direction, _ReturnBullet, this);
    }

    // プールから弾を取り出す(空なら新規生成)
    private ZelxFireBullet _GetBullet(Vector3 position) {
        while (_bulletPool.Count > 0) {
            var pooled = _bulletPool.Dequeue();
            if (pooled == null) continue; // シーン遷移等で破棄済みのものは捨てる
            pooled.transform.position = position;
            pooled.gameObject.SetActive(true);
            return pooled;
        }
        return Instantiate(_bulletPrefab, position, Quaternion.identity);
    }

    // 弾をプールへ返却する
    private void _ReturnBullet(ZelxFireBullet bullet) {
        // 本体が先に破棄されていた場合は、返却先が無いので弾ごと消す
        if (this == null) {
            Destroy(bullet.gameObject);
            return;
        }
        bullet.gameObject.SetActive(false);
        _bulletPool.Enqueue(bullet);
    }

    protected override IEnumerator Die() {
        OnDied?.Invoke();

        if (_seFinish != null) {
            if (_seDead != null) _audioSource?.PlayOneShot(_seDead);
            _audioSource?.PlayOneShot(_seFinish);
        }

        var cinemachineManager = CinemachineManager.Instance;
        var flash = ScreenFlash.Instance;

        // === 2. カメラズーム ===
        cinemachineManager.ZoomOnTarget(transform);
        yield return new WaitForSeconds(0.05f);

        // === 1. ヒット時演出 ===
        Time.timeScale = 0.1f;
        flash?.Flash();
        _anim.Play("Die");
        yield return new WaitForSecondsRealtime(0.2f);
        //flash?.Flash(3.0f);

        // BGM停止
        if (_audioBGM != null) {
            _audioBGM.volume = 0f;
        }

        yield return new WaitForSecondsRealtime(4.0f);

        cinemachineManager.ReturnToPlayer();

        // 徐々に時間を戻す
        float timeScale = Time.timeScale;
        while (timeScale < 1f) {
            timeScale += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Min(timeScale, 1f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // === 4. 爆発 ===
        if (_dieExplosion != null) {
            // 爆発エフェクト生成
            flash?.Flash();
            yield return new WaitForSecondsRealtime(0.2f);
            flash?.Flash();
            yield return new WaitForSecondsRealtime(0.5f);

            Instantiate(_dieExplosion, transform.position, Quaternion.identity);
            cinemachineManager.ShakeCamera(duration: 0.5f);
            _sprite.enabled = false;
        }

        yield return new WaitForSeconds(1.0f);

        OnDieEnded?.Invoke();

        yield return new WaitForSeconds(5.0f);

        // BGM切り替え
        _audioBGM.volume = _currentBGMVolume;
    }
}
