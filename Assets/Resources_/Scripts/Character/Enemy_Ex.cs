using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボス敵キャラクターのAI
/// </summary>
public class Enemy_Ex : Enemy_Base {
    private enum eExAction {
        None,
        LaserShoot,
        RainShoot,
        BurstShoot,
        FastBurst,
        ThreeShoot,
        JumpShoot,
        FastSpecialShoot,
        SpecialAttack,
    }
    [SerializeField] private AudioClip _seFinish;
    [SerializeField] private AudioSource _audioBGM;
    [SerializeField] private AudioClip _bgmSerious;
    [SerializeField] private AudioClip _bgmFlower;

    // レーザービームのプレハブ
    [SerializeField] private GameObject _laserPrefab;
    [SerializeField] private GameObject _thinLaserPrefab;
    // 爆発のプレハブ
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private GameObject _explosionNotShakePrefab;

    // パラメータ
    [SerializeField] private ExParameter _exParameter;
    private bool _isHalfHp => _charaParam.CurrentHP <= _charaParam.MaxHP / 2.0f;
    private bool _isDowned = false;
    private float _seriousHpThreshold => _charaParam.MaxHP * 0.4f;
    private bool _isSeriously = false;

    [SerializeField] private Transform[] _fastRainShoot;
    [SerializeField] private Transform[] _fastRainShootReverse;
    [SerializeField] private Transform[] _rainShootPoints;
    [SerializeField] private Transform[] _threeShootPoints;
    [SerializeField] private Transform[] _specialShootPoints;
    [SerializeField] private Transform _specialExposionPoint;
    [SerializeField] private Transform[] _burstExplosionPoints;
    [SerializeField] private Transform[] _fastBurstPoints;
    [SerializeField] private Transform _jumpPoint;
    [SerializeField] private Transform _jumpShootPoint;
    [SerializeField] private Transform _jumpShootExplosionPoint;
    private Transform _playerTransform;
    private Transform _laserParent;

    // 行動中フラグ
    private bool _isExecutingAction = false;
    private bool _isSpecialActioned = false;
    private IEnumerator _currentActionCoroutine = null;

    // 強制爆発攻撃までのカウント
    int _forceBurstCount = 3;
    private float _currentBGMVolume = 0.7f;
    private bool _startBGM = false;
    
    private readonly PreciseWaitTimer _waitTimer = new PreciseWaitTimer();

    protected override void _Setup() {
        base._Setup();
        _currentBGMVolume = _audioBGM != null ? _audioBGM.volume : 0.7f;
        if (_audioBGM != null) {
            _audioBGM.enabled = false;
        }
        _nextActionTime = _exParameter.ActionInterval;
        _currentActionTime = _nextActionTime - 4.5f; // 最初の行動を少し遅らせる
        _playerTransform = GameObject.FindAnyObjectByType<Player_Character>()?.transform;
        _laserParent = new GameObject("LaserBeams").transform;
    }

    protected override void _UpdateSpecials() {
        if (_isDead) return;

        // 行動タイマー更新
        if (_currentActionTime < _nextActionTime) {
            _currentActionTime += Time.deltaTime;
            return;

        }
        if (_isExecutingAction) {
            return;
        }

        // いずれかの行動を実行
        var action = _isSeriously ? _ChooseSeriousAction() : _ChooseAction();
        if (_playerTransform != null) {
            _isRight = _playerTransform.position.x > transform.position.x;
        }
        if (_startBGM == false) {
            // BGMスタート
            _startBGM = true;
            if (_audioBGM != null) {
                _audioBGM.enabled = true;
                _audioBGM.Play();
            }
        }

        // HP半分以下で強制爆発攻撃カウントダウン
        if (_isHalfHp && _forceBurstCount > 0) {
            _forceBurstCount--;
            if (_forceBurstCount == 0) {
                action = eExAction.BurstShoot;
            }
        }

        // HP半分以下でスペシャル攻撃
        if (_isHalfHp && _isSpecialActioned == false) {
            action = eExAction.SpecialAttack;
            _isSpecialActioned = true;
        }

        _anim.Play("Stand");
        switch (action) {
            case eExAction.LaserShoot:
                bool is_reverse = Random.value > 0.5f;
                _currentActionCoroutine = (_ExecuteLaser(_exParameter.ShootTime, is_reverse ? _fastRainShootReverse : _fastRainShoot,
                    count: _isSeriously ? _fastRainShoot.Length : 2, is_random: _isSeriously ? false : true,
                    interval_rate: 0.5f, reset_time: -1.0f, is_thin: true));
                break;
            case eExAction.RainShoot:
                _currentActionCoroutine = (_ExecuteLaser(_exParameter.RainShootTime, _rainShootPoints,
                    _isSeriously ? 5 : 3, is_random: true));
                break;
            case eExAction.BurstShoot:
                _currentActionCoroutine = (_ExecuteBurst(_burstExplosionPoints.Length, _burstExplosionPoints, is_special_burst: true));
                break;
            case eExAction.FastBurst:
                _currentActionCoroutine = (_ExecuteBurst(1, _fastBurstPoints, reset_time: -1.0f, is_random:true, is_special_burst: false));
                break;
            case eExAction.ThreeShoot:
                _currentActionCoroutine = (_ExecuteLaser(_exParameter.ThreeShootTime, _threeShootPoints, 3));
                break;
            case eExAction.JumpShoot:
                _currentActionCoroutine = (_ExecuteJumpLaser());
                break;
            case eExAction.FastSpecialShoot:
                _currentActionCoroutine = (_FastSpecialAttack());
                break;
            case eExAction.SpecialAttack:
                _currentActionCoroutine = (_ExecuteSpecialAttack());
                break;
        }
        StartCoroutine(_currentActionCoroutine);
        _isExecutingAction = true;
    }

    private eExAction _ChooseAction() {
        int totalWeight = _exParameter.ShootWeight + _exParameter.RainShootWeight + _exParameter.FastBurstWeight;

        int randomValue = Random.Range(0, totalWeight);
        if (randomValue < _exParameter.ShootWeight) {
            return eExAction.LaserShoot;
        } else if (randomValue < _exParameter.ShootWeight + _exParameter.RainShootWeight) {
            return eExAction.RainShoot;
        } else {
            return eExAction.FastBurst;
        }
    }
    private eExAction _ChooseSeriousAction() {
        int totalWeight = _exParameter.ShootWeight + _exParameter.ThreeShootWeight + _exParameter.FastSpecialWeight +
            _exParameter.RainShootWeight + _exParameter.BurstWeight + _exParameter.FastBurstWeight;

        int randomValue = Random.Range(0, totalWeight);
        if (randomValue < _exParameter.ShootWeight) {
            return eExAction.LaserShoot;
        } else if (randomValue < _exParameter.ShootWeight + _exParameter.ThreeShootWeight) {
            return eExAction.ThreeShoot;
        } else if (randomValue < _exParameter.ShootWeight + _exParameter.ThreeShootWeight + _exParameter.RainShootWeight) {
            return eExAction.RainShoot;
        } else if (randomValue < _exParameter.ShootWeight + _exParameter.ThreeShootWeight + _exParameter.RainShootWeight + _exParameter.FastBurstWeight) {
            return eExAction.FastBurst;
        }else if(randomValue < _exParameter.ShootWeight + _exParameter.ThreeShootWeight + _exParameter.RainShootWeight + _exParameter.FastBurstWeight + _exParameter.FastSpecialWeight) {
            return eExAction.FastSpecialShoot;
        } else {
            return eExAction.BurstShoot;
        }
    }

    private IEnumerator _ExecuteLaser(float waitTimer, Transform trans) {
        yield return _ExecuteLaser(waitTimer, new Transform[] { trans });
    }
    private IEnumerator _ExecuteLaser(float waitTimer, Transform[] startTranses, int count = 1,
        bool is_random = false, bool is_thin = false, float interval_rate = 1.0f, float reset_time = 0f) {
        // アニメーション再生
        _anim.SetTrigger("Shoot");
        yield return _waitTimer.Wait(waitTimer);

        // レーザービーム生成
        _anim.SetTrigger("Shoot");

        var set_transforms = _GetTransforms(count, startTranses, is_random);

        for (int i = 0; i < count; i++) {
            var trans = set_transforms[i % set_transforms.Length];

            var laser_obj = Instantiate(
                is_thin ? _thinLaserPrefab : _laserPrefab,
                trans.position, Quaternion.identity);
            laser_obj.transform.rotation = trans.transform.rotation;
            laser_obj.transform.parent = _laserParent;
            yield return _waitTimer.Wait(_exParameter.ShootInterval * interval_rate);
        }
        _ResetAction(reset_time);
    }

    private Transform[] _GetTransforms(int count, Transform[] target_transes, bool is_random = false) {
        List<Transform> set_transforms = new List<Transform>();
        // ランダムで複数選択
        for (int i = 0; i < count; i++) {
            // 選択数が最大に達したら終了
            if (set_transforms.Count >= target_transes.Length) {
                break;
            }

            int rand_index = 0;

            if (is_random) {
                rand_index = Random.Range(0, target_transes.Length);
            } else {
                rand_index = i % target_transes.Length;
            }

            // 重複チェック
            if (set_transforms.Contains(target_transes[rand_index])) {
                i--;
                continue;
            }
            set_transforms.Add(target_transes[rand_index]);
        }
        return set_transforms.ToArray();
    }

    private IEnumerator _ExecuteJumpLaser() {
        // 構え
        _anim.SetTrigger("JumpShoot");
        _anim.SetBool("Jumping", true);
        yield return _waitTimer.Wait(_exParameter.JumpShootTime);

        // レーザービーム生成
        _anim.SetTrigger("JumpShoot");
        var laser_obj = Instantiate(_laserPrefab, transform.position, Quaternion.identity);
        laser_obj.transform.rotation = _jumpShootPoint.transform.rotation;

        // 爆発生成
        if (_jumpShootExplosionPoint != null) {
            yield return _waitTimer.Wait(_exParameter.ShootExplosionTime);
            Instantiate(_explosionPrefab, _jumpShootExplosionPoint.position, Quaternion.identity);
        }

        _anim.SetBool("Jumping", false);

        // 爆発生成
        if (_jumpShootExplosionPoint != null) {
            yield return _waitTimer.Wait(_exParameter.ShootExplosionTime);
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        }

        yield return null;
        _ResetAction();
    }

    private IEnumerator _ExecuteBurst(int count, Transform[] trans, float reset_time = 0f, bool is_random = false, bool is_special_burst = false) {
        // アニメーション再生
        _anim.SetTrigger("Burst");

        var set_transforms = _GetTransforms(count, trans, is_random);

        yield return _ExecuteExplosion(set_transforms, set_transforms.Length == 1 ? true : false);

        if (is_special_burst) {
            yield return _waitTimer.Wait(1.5f);

            yield return _ExecuteExplosion(_specialExposionPoint, 1.7f);
        }

        // 爆発生成
        _anim.SetTrigger("Burst");

        // 強制爆発カウント終了
        if(_isHalfHp) {
            _forceBurstCount = -1;
        }

        _ResetAction(reset_time);
    }

    // 必殺攻撃
    private IEnumerator _ExecuteSpecialAttack() {
        var camera = CinemachineManager.Instance;
        camera.ShakeCamera(duration: 0.1f, intensity: 0.1f);

        yield return (_ExecuteLaser(_exParameter.SpecialShootTime, _specialShootPoints, _exParameter.SpecialLaserCount, is_thin: true,
            is_random:true, interval_rate: 0.5f, reset_time: -_exParameter.FastActionInterval));

        yield return _ExecuteExplosion(_specialExposionPoint, 1.7f);
    }

    // 簡易必殺攻撃
    private IEnumerator _FastSpecialAttack() {
        yield return (_ExecuteLaser(_exParameter.ShootTime, _specialShootPoints, _exParameter.SpecialLaserCount / 2, is_thin: true,
            is_random: true, interval_rate: 0.5f, reset_time: -_exParameter.FastActionInterval));
    }

    private IEnumerator _ExecuteExplosion(Transform startTrans, float scale_rate = 1.0f) {
        return _ExecuteExplosion(new Transform[] { startTrans }, true, scale_rate);
    }
    // 連爆
    private IEnumerator _ExecuteExplosion(Transform[] startTranses, bool is_shake, float scale_rate = 1.0f) {
        for (int i = 0; i < startTranses.Length; i++) {
            var explosion_obj = Instantiate(
                is_shake ? _explosionPrefab : _explosionNotShakePrefab, 
                startTranses[i].position, Quaternion.identity);
            explosion_obj.transform.localScale *= scale_rate;
            yield return _waitTimer.Wait(_exParameter.ShootInterval / 2);
        }
    }

    private void _ResetAction(float wait_time = 0) {
        _currentActionTime = wait_time;
        _isExecutingAction = false;
    }

    public override bool Damage(int damage, Vector2 blow_power_right, float invincible_time, float damage_reaction_time, bool is_trap_damage = false) {
        var result = base.Damage(damage, blow_power_right, invincible_time, damage_reaction_time, is_trap_damage);

        if (_isHalfHp && !_isDowned) {
            StartCoroutine(_Down());
            _isDowned = true;
        }

        // HPが一定以下で本気モードに移行
        if (_charaParam.CurrentHP <= _seriousHpThreshold && !_isSeriously) {
            _isSeriously = true;
            StartCoroutine(_Seriously());
        }

        return result;
    }

    /// <summary>
    /// ダウン演出
    /// </summary>
    private IEnumerator _Down() {
        var cinemachineManager = CinemachineManager.Instance;
        var flash = ScreenFlash.Instance;

        _isExecutingAction = true;
        IsEventInvincible = true;

        if (_seFinish != null) {
            _audioSource?.PlayOneShot(_seDead);
            _audioSource?.PlayOneShot(_seFinish);
        }
        OnDowned?.Invoke();
        _ResetLaser();

        if (_audioBGM != null) {
            _audioBGM.volume = 0f;
        }

        // === 1. ヒット時演出 ===
        flash?.Flash();
        _anim.Play("Down");
        yield return new WaitForSeconds(0.2f);
        flash?.Flash(2.0f);

        yield return new WaitForSeconds(1.0f);

        _coinSpawner.SpawnCoin(200);

        yield return new WaitForSeconds(1.0f);
        IsEventInvincible = false;
        yield return new WaitForSeconds(1.0f);

        // BGM切り替え
        _audioBGM.volume = _currentBGMVolume;
        _audioBGM.clip = _bgmSerious;
        _audioBGM.Play();

        // 現在の行動をリセット
        _nextActionTime = _exParameter.FastActionInterval;
        _ResetAction();
    }

    /// <summary>
    /// 本気モード移行
    /// </summary>
    private IEnumerator _Seriously() {
        OnSeriously?.Invoke();
        yield return null;
    }

    protected override IEnumerator Die() {
        OnDied?.Invoke();

        if (_seFinish != null) {
            _audioSource?.PlayOneShot(_seDead);
            _audioSource?.PlayOneShot(_seFinish);
        }
        _ResetLaser();

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
        flash?.Flash(3.0f);

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
        _audioBGM.clip = _bgmFlower;
        _audioBGM.Play();
    }

    private void _ResetLaser() {
        // レーザービーム削除
        foreach (Transform child in _laserParent) {
            Destroy(child.gameObject);
        }
        if (_currentActionCoroutine != null) {
            StopCoroutine(_currentActionCoroutine);
            _currentActionCoroutine = null;
        }
    }

}

