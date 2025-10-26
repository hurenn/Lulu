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
        ThreeShoot,
        JumpShoot,
        SpecialAttack,
    }
    [SerializeField] private AudioClip _seFinish;
    [SerializeField] private AudioSource _audioBGM;
    [SerializeField] private AudioClip _bgmFlower;

    // レーザービームのプレハブ
    [SerializeField] private GameObject _laserPrefab;
    // 爆発のプレハブ
    [SerializeField] private GameObject _explosionPrefab;

    // パラメータ
    [SerializeField] private ExParameter _exParameter;

    [SerializeField] private Transform _shootPoint;
    [SerializeField] private Transform[] _rainShootPoints;
    [SerializeField] private Transform[] _threeShootPoints;
    [SerializeField] private Transform _jumpPoint;
    [SerializeField] private Transform _jumpShootPoint;
    [SerializeField] private Transform _jumpShootExplosionPoint;

    // 行動中フラグ
    private bool _isExecutingAction = false;

    protected override void _Setup() {
        base._Setup();
        _nextActionTime = _exParameter.ActionInterval;
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
        var action = _charaParam.CurrentHP <= _charaParam.MaxHP / 2 ?
            _ChooseSeriousAction() : _ChooseAction();

        switch (action) {
            case eExAction.LaserShoot:
                StartCoroutine(_ExecuteLaser(_exParameter.ShootTime, _shootPoint));
                break;
            case eExAction.RainShoot:
                StartCoroutine(_ExecuteLaser(_exParameter.RainShootTime, _rainShootPoints,
                    _charaParam.CurrentHP <= _charaParam.MaxHP / 2 ? 5 : 3));
                break;
            case eExAction.BurstShoot:
                StartCoroutine(_ExecuteBurst(_exParameter.BurstTime, _shootPoint));
                break;
            case eExAction.ThreeShoot:
                StartCoroutine(_ExecuteLaser(_exParameter.ThreeShootTime, _threeShootPoints, 3));
                break;
            case eExAction.JumpShoot:
                StartCoroutine(_ExecuteJumpLaser());
                break;
        }
        _isExecutingAction = true;
    }

    private eExAction _ChooseAction() {
        int totalWeight = _exParameter.ShootWeight + _exParameter.RainShootWeight + _exParameter.BurstWeight;

        int randomValue = Random.Range(0, totalWeight);
        if (randomValue < _exParameter.ShootWeight) {
            return eExAction.LaserShoot;
        } else if (randomValue < _exParameter.ShootWeight + _exParameter.RainShootWeight) {
            return eExAction.RainShoot;
        } else {
            return eExAction.BurstShoot;
        }
    }
    private eExAction _ChooseSeriousAction() {
        int totalWeight = _exParameter.ThreeShootWeight + _exParameter.JumpShootWeight + _exParameter.BurstWeight;

        int randomValue = Random.Range(0, totalWeight);
        if (randomValue < _exParameter.ThreeShootWeight) {
            return eExAction.ThreeShoot;
        } else if (randomValue < _exParameter.ThreeShootWeight + _exParameter.JumpShootWeight) {
            return eExAction.JumpShoot;
        } else {
            return eExAction.BurstShoot;
        }
    }

    private IEnumerator _ExecuteLaser(float waitTimer, Transform trans) {
        yield return _ExecuteLaser(waitTimer, new Transform[] { trans });
    }
    private IEnumerator _ExecuteLaser(float waitTimer, Transform[] startTranses, int count = 1) {
        // アニメーション再生
        _anim.SetTrigger("Shoot");
        yield return new WaitForSeconds(waitTimer);

        _ResetAction();

        // レーザービーム生成
        _anim.SetTrigger("Shoot");

        List<Transform> set_transforms = new List<Transform>();
        // ランダムで複数選択
        for (int i = 0; i < count; i++) {
            int rand_index = Random.Range(0, startTranses.Length);

            // 重複チェック
            if (set_transforms.Contains(startTranses[rand_index])) {
                i--;
                continue;
            }
            set_transforms.Add(startTranses[rand_index]);
        }

        foreach (var trans in set_transforms) {
            var laser_obj = Instantiate(_laserPrefab, trans.position, Quaternion.identity);
            laser_obj.transform.rotation = trans.transform.rotation;
            yield return new WaitForSeconds(_exParameter.ShootInterval);
        }

        yield return null;
    }

    private IEnumerator _ExecuteJumpLaser() {
        // 構え
        _anim.SetTrigger("JumpShoot");
        _anim.SetBool("Jumping", true);
        yield return new WaitForSeconds(_exParameter.JumpShootTime);

        // レーザービーム生成
        _anim.SetTrigger("JumpShoot");
        var laser_obj = Instantiate(_laserPrefab, transform.position, Quaternion.identity);
        laser_obj.transform.rotation = _jumpShootPoint.transform.rotation;

        // 爆発生成
        if (_jumpShootExplosionPoint != null) {
            yield return new WaitForSeconds(_exParameter.ShootExplosionTime);
            Instantiate(_explosionPrefab, _jumpShootExplosionPoint.position, Quaternion.identity);
        }

        _anim.SetBool("Jumping", false);

        // 爆発生成
        if (_jumpShootExplosionPoint != null) {
            yield return new WaitForSeconds(_exParameter.ShootExplosionTime);
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        }

        yield return null;
        _ResetAction();
    }

    private IEnumerator _ExecuteBurst(float waitTimer, Transform trans) {
        // アニメーション再生
        _anim.SetTrigger("Burst");
        yield return null;

        // 爆発生成
        _anim.SetTrigger("Burst");
        Instantiate(_explosionPrefab, trans.position, Quaternion.identity);
        yield return new WaitForSeconds(waitTimer);

        _ResetAction();
    }

    private void _ResetAction() {
        _isExecutingAction = false;
        _currentActionTime = 0;
    }

    protected override IEnumerator Die() {
        OnDied?.Invoke();

        if (_seFinish != null) {
            _audioSource?.PlayOneShot(_seDead);
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
        flash?.Flash(3.0f);

        var current_volume = _audioBGM != null ? _audioBGM.volume : 0.8f;
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

        // コイン生成
        if (_coinPrefab != null) {
            for (int i = 0; i < _exp; i++) {
                var coin_obj = Instantiate(_coinPrefab, transform.position, Quaternion.identity);
                var coin = coin_obj.GetComponent<Coin_Object>();
                if (coin != null) {
                    coin.InitializeAutoCollect();
                }
                yield return new WaitForSecondsRealtime(0.005f);
            }
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
            cinemachineManager.ShakeCamera(duration: 1f);
            _sprite.enabled = false;

            flash?.FadeIn(5.0f);
        }

        yield return new WaitForSeconds(1.0f);

        OnDieEnded?.Invoke();

        yield return new WaitForSeconds(5.0f);

        // BGM切り替え
        _audioBGM.volume = current_volume;
        _audioBGM.clip = _bgmFlower;
        _audioBGM.Play();

    }
}

