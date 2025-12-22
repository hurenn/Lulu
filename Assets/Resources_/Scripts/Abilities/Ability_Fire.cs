using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Ability_Fire : Ability_Base
{
    // 射撃アニメーション
    private const string _SHOT_ANIM = "Marlica_Shot";

    // 最大弾数
    private int _maxShoot = 3;
    private int _currentShot = 0;

    // 弾オブジェクト
    [SerializeField] private FireBullet _bulletObj;

    // 必殺オブジェクト
    [SerializeField] private GameObject _specialBulletObj;
    
    // 必殺技弾のオブジェクトプール
    private Queue<GameObject> _specialBulletPool = new Queue<GameObject>();
    private const int INITIAL_POOL_SIZE = 50; // 初期プールサイズ（2秒 ÷ 0.1秒 + 余裕）
    private const float BULLET_LIFETIME = 0.5f; // 弾の生存時間

    // 自動攻撃範囲
    [SerializeField] private Collider2D _autoAttackRange;
    // 範囲内の敵リスト
    private List<Enemy_Base> _enemiesInRange = new List<Enemy_Base>();
    // 自動攻撃間隔
    private float _autoAttackInterval = 0.4f;
    private float _currentAutoAttackInterval = 0.0f;
    
    // トリガーヘルパー参照
    private FireAutoAttackTrigger _triggerHelper;

    public override void UpdateParameter(bool is_right, Transform chara_transform, CommonParameter common_param,  CharacterParameter_Player chara_param, WarpControl warp_control) {
        base.UpdateParameter(is_right, chara_transform, common_param, chara_param, warp_control);
        
        // 自動攻撃範囲のトリガー設定
        if (_autoAttackRange != null) {
            _autoAttackRange.isTrigger = true;
            
            // トリガーイベントを受け取るヘルパーコンポーネント
            _triggerHelper = _autoAttackRange.gameObject.GetComponent<FireAutoAttackTrigger>();
            if (_triggerHelper == null) {
                _triggerHelper = _autoAttackRange.gameObject.AddComponent<FireAutoAttackTrigger>();
            }
            _triggerHelper.Setup(this);
        }
        
        // オブジェクトプールの初期化
        _InitializePool();
    }

    /// <summary>
    /// オブジェクトプールの初期化
    /// </summary>
    private void _InitializePool() {
        if (_specialBulletObj == null || _specialBulletPool.Count > 0) return;

        for (int i = 0; i < INITIAL_POOL_SIZE; i++) {
            GameObject bullet = Instantiate(_specialBulletObj);
            bullet.SetActive(false);
            _specialBulletPool.Enqueue(bullet);
        }
    }

    /// <summary>
    /// プールから弾を取得（なければ新規作成）
    /// </summary>
    private GameObject _GetPooledBullet() {
        if (_specialBulletPool.Count > 0) {
            GameObject bullet = _specialBulletPool.Dequeue();
            bullet.SetActive(true);
            return bullet;
        } else {
            // プールが空の場合は新規作成
            return Instantiate(_specialBulletObj);
        }
    }

    /// <summary>
    /// 弾をプールに戻す
    /// </summary>
    private void _ReturnBulletToPool(GameObject bullet) {
        if (bullet == null) return;
        
        bullet.SetActive(false);
        _specialBulletPool.Enqueue(bullet);
    }

    protected override void _Update() {
        base._Update();

        // 位置更新
        UpdatePartnerTransform();

        // リスト内に敵がいれば自動攻撃
        if (_enemiesInRange.Count > 0 && _currentAutoAttackInterval <= 0) {
            // 既に死んでいる敵をリストから削除
            _enemiesInRange.RemoveAll(enemy => enemy == null || enemy.isDead);
            // 敵がリスト内にいれば攻撃
            if (_enemiesInRange.Count > 0 && !_cancelByOverheat) {
                ExecuteSimple();
                _currentAutoAttackInterval = _autoAttackInterval;
            }
        }

        // 自動攻撃タイマー更新
        if (_currentAutoAttackInterval > 0) {
            _currentAutoAttackInterval -= Time.deltaTime;
        }
    }

    /// <summary>
    /// オート攻撃タイマーリセット
    /// </summary>
    public void ResetAutoAttackInterval() {
        _currentAutoAttackInterval = _autoAttackInterval;
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat) {
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }

        // 必殺技発動
        if(_isSpecialCharged) {
            _UseSpecial();
            return eAbilityResult.FireSpecial;
        }

        var ability_result = _SimpleShot();

        // 攻撃実行
        if (ability_result != eAbilityResult.None) {
            _AppearCheck(eAbilityType.Fire);
            return ability_result;
        }

        return eAbilityResult.None;
    }

    /// <summary>
    /// 一発発射
    /// </summary>
    private eAbilityResult _SimpleShot() {
        if(_currentShot >= _maxShoot) {
            return eAbilityResult.None;
        }

        // アニメーション再生
        _anim?.Play(_SHOT_ANIM, 0, 0.0f);

        var bullet = Instantiate(_bulletObj, transform.position, Quaternion.identity);
        bullet.SetCallback(() => _currentShot--);
        bullet.IsRight = _isRight;

        // 進行方向に合わせて反転
        var scale = bullet.transform.localScale;
        scale.x *= (_isRight ? 1 : -1);
        bullet.transform.localScale = scale;

        _currentShot++;
        return eAbilityResult.FireShot;
    }

    public override void UpdatePartnerTransform() {
        base.UpdatePartnerTransform();

        // 自動攻撃範囲の位置更新
        if (_autoAttackRange != null) {
            var range_pos = _autoAttackRange.transform.localPosition;
            range_pos.x = Mathf.Abs(range_pos.x) * (_isRight ? 1 : -1);
            _autoAttackRange.transform.localPosition = range_pos;
        }
    }

    /// <summary>
    /// 敵が範囲に入った（トリガーヘルパーから呼ばれる）
    /// </summary>
    public void OnEnemyEnter(Enemy_Base enemy) {
        if (enemy != null && !_enemiesInRange.Contains(enemy)) {
            _enemiesInRange.Add(enemy);
        }
    }

    /// <summary>
    /// 敵が範囲から出た（トリガーヘルパーから呼ばれる）
    /// </summary>
    public void OnEnemyExit(Enemy_Base enemy) {
        if (enemy != null && _enemiesInRange.Contains(enemy)) {
            _enemiesInRange.Remove(enemy);
        }
    }

    protected override void _OnSpecialCutInFinished(PlayableDirector obj) {
        base._OnSpecialCutInFinished(obj);
        StartCoroutine(_SpecialAttack());
    }

    /// <summary>
    /// 必殺技攻撃
    /// </summary>
    private IEnumerator _SpecialAttack() {
        float attack_duration = 2.0f;
        float current_time = 0.0f;
        float spawn_interval = 0.02f; // 弾の生成間隔
        float next_spawn_time = 0.0f;

        // カメラの画面範囲を取得
        Camera mainCamera = Camera.main;
        if (mainCamera == null) {
            Debug.LogError("Main Camera not found!");
            _onEndSpecialAttack?.Invoke();
            yield break;
        }

        // カメラからのZ距離を計算
        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z);

        // 画面の高さを計算して、0.2秒で横断する速度を算出
        Vector3 topPoint = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, cameraDistance));
        Vector3 bottomPoint = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));
        float screenHeight = Mathf.Abs(topPoint.y - bottomPoint.y);
        float bulletSpeed = screenHeight / 0.1f; // 0.1秒で画面の高さ分移動

        yield return new WaitForSeconds(0.5f); // 少し待ってから攻撃開始
        while (current_time < attack_duration) {
            // 一定間隔で弾を発射
            if (current_time >= next_spawn_time) {
                _SpawnSpecialBullet(mainCamera, bulletSpeed);
                next_spawn_time += spawn_interval;
            }

            current_time += Time.deltaTime;
            yield return null;
        }

        _onEndSpecialAttack?.Invoke();
    }

    /// <summary>
    /// 必殺技の弾を生成（画面外左上からランダムな位置に）
    /// </summary>
    private void _SpawnSpecialBullet(Camera camera, float bulletSpeed) {
        if (_specialBulletObj == null) return;

        // カメラからのZ距離を計算（カメラのZ座標の絶対値）
        float cameraDistance = Mathf.Abs(camera.transform.position.z);

        // 2D基準で画面の左上（ビューポート座標 0,1）のワールド座標を取得
        Vector3 topLeft = camera.ViewportToWorldPoint(new Vector3(0, 1, cameraDistance));
        topLeft.z = 0; // 2D基準でZ座標を0に固定
        
        // 2D基準で画面の右上（ビューポート座標 1,1）のワールド座標を取得
        Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, cameraDistance));
        topRight.z = 0; // 2D基準でZ座標を0に固定

        // 画面外の上空から降らせる（画面の高さの20%上）
        Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));
        bottomLeft.z = 0;
        float screenHeight = Mathf.Abs(topLeft.y - bottomLeft.y);
        float spawnHeight = topLeft.y + screenHeight * 0.2f;

        // X座標をランダムに（画面左端から右端まで）
        float randomX = Random.Range(topLeft.x, topRight.x);
        
        // 生成位置（2D基準でZ=0）
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, 0);

        // プールから弾を取得
        GameObject bullet = _GetPooledBullet();
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.identity;
        
        // 弾に下向きの一定速度を設定（Rigidbody2Dがある場合）
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) {
            // 重力を無効化して一定速度で落下
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.down * bulletSpeed; // 下向きに一定速度で落下
        }
        
        // 1秒後にプールに戻すコルーチンを開始
        StartCoroutine(_ReturnBulletAfterDelay(bullet, BULLET_LIFETIME));
    }

    /// <summary>
    /// 指定時間後に弾をプールに戻す
    /// </summary>
    private IEnumerator _ReturnBulletAfterDelay(GameObject bullet, float delay) {
        yield return new WaitForSeconds(delay);
        _ReturnBulletToPool(bullet);
    }
}

/// <summary>
/// 自動攻撃範囲のトリガーヘルパークラス
/// </summary>
public class FireAutoAttackTrigger : MonoBehaviour {
    private Ability_Fire _parentAbility;

    public void Setup(Ability_Fire ability) {
        _parentAbility = ability;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy")) {
            var enemy = other.GetComponent<Enemy_Base>();
            if (enemy != null) {
                _parentAbility?.OnEnemyEnter(enemy);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy")) {
            var enemy = other.GetComponent<Enemy_Base>();
            if (enemy != null) {
                _parentAbility?.OnEnemyExit(enemy);
            }
        }
    }
}
