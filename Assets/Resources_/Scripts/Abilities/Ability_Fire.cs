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
    [SerializeField] private GameObject _specialExplosionObj;

    // 必殺技弾のオブジェクトプール
    private Queue<GameObject> _specialBulletPool = new Queue<GameObject>();
    private const int INITIAL_POOL_SIZE = 10; // 初期プールサイズ（2秒 ÷ 0.1秒 + 余裕）
    private const float BULLET_MIN_LIFETIME = 0.05f; // 弾の最小生存時間
    private const float BULLET_MAX_LIFETIME = 0.2f; // 弾の最大生存時間

    // 自動攻撃範囲
    [SerializeField] private Collider2D _autoAttackRange;
    // 範囲内の敵リスト
    private List<Enemy_Base> _enemiesInRange = new List<Enemy_Base>();
    // 自動攻撃間隔
    private float _autoAttackInterval = 0.4f;
    private float _currentAutoAttackInterval = 0.0f;
    
    // トリガーヘルパー参照
    private FireAutoAttackTrigger _triggerHelper;

    public override void UpdateParameter(bool is_right, Transform chara_transform, CommonParameter common_param,  CharacterParameter_Player chara_param, WarpControl warp_control, MotorStates motor_states) {
        base.UpdateParameter(is_right, chara_transform, common_param, chara_param, warp_control, motor_states);
        
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
        
        // 着弾エフェクト再生
        Instantiate(_specialExplosionObj, bullet.transform.position, Quaternion.identity);
        
        // カメラシェイク（微妙な揺れ）
        var cinemachineManager = CinemachineManager.Instance;
        if (cinemachineManager != null) {
            cinemachineManager.ShakeCamera(duration: 0.01f, intensity: 0.01f);
        }
        
        bullet.SetActive(false);
        _specialBulletPool.Enqueue(bullet);
    }

    protected override void _Update() {
        base._Update();

        // 位置更新
        UpdatePartnerTransform();

        // リスト内に敵がいれば自動攻撃
        if (_enemiesInRange.Count > 0 && _currentAutoAttackInterval <= 0 && !_isSpecialUsing) {
            // 既に死んでいる敵をリストから削除
            _enemiesInRange.RemoveAll(enemy => enemy == null || enemy.isDead);
            // 敵がリスト内にいれば攻撃
            if (_enemiesInRange.Count > 0 && !_cancelByOverheat) {
                _TryShot(true);
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
        return _TryShot(false);
    }

    /// <summary>
    /// 攻撃判定
    /// </summary>
    /// <param name="is_auto">オート攻撃によるものか</param>
    private eAbilityResult _TryShot(bool is_auto) {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat) {
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }

        // 必殺技発動
        if (_isSpecialCharged && !is_auto && 
                !_motorStates.isWarpDashing && !_motorStates.isSliding) {
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

        // 画面の高さを計算して、0.1秒で横断する速度を算出
        Vector3 topPoint = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, cameraDistance));
        Vector3 bottomPoint = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));
        float screenHeight = Mathf.Abs(topPoint.y - bottomPoint.y);
        float bulletSpeed = screenHeight / 0.1f; // 0.1秒で画面の高さ分移動

        // 画面の左上座標を取得
        Vector3 topLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, cameraDistance));
        topLeft.z = 0;
        
        // 画面の幅を計算
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, cameraDistance));
        topRight.z = 0;
        float screenWidth = Mathf.Abs(topRight.x - topLeft.x);

        // 生成地点を画面外左上に固定（画面高さの20%上）
        float spawnHeight = topLeft.y + screenHeight * 0.2f;
        Vector3 spawnPosition = new Vector3(topLeft.x, spawnHeight, 0);

        // 攻撃開始前の待機
        yield return new WaitForSeconds(0.5f);
        
        while (current_time < attack_duration) {
            // 一定間隔で弾を発射
            if (current_time >= next_spawn_time) {
                _SpawnSpecialBulletWithAngle(spawnPosition, bulletSpeed, screenWidth, screenHeight);
                next_spawn_time += spawn_interval;
            }

            current_time += Time.deltaTime;
            yield return null;
        }

        _onEndSpecialAttack?.Invoke();
    }

    /// <summary>
    /// 必殺技の弾を角度を変えて生成（画面左上から）
    /// </summary>
    private void _SpawnSpecialBulletWithAngle(Vector3 spawnPosition, float bulletSpeed, float screenWidth, float screenHeight) {
        if (_specialBulletObj == null) return;

        // プールから弾を取得
        GameObject bullet = _GetPooledBullet();
        bullet.transform.position = spawnPosition;
        
        // 発射角度を0度～80度の範囲でランダムに生成
        // 0度 = 真下、80度 = ほぼ横方向
        float randomAngle = Random.Range(0f, 80f);
        
        // 角度から方向ベクトルを計算（真下を0度として右方向に角度が増える）
        float angleInRadians = randomAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(angleInRadians), -Mathf.Cos(angleInRadians));
        
        // 弾の表示角度を進行方向に合わせる
        // Unityの回転は反時計回りが正なので、進行方向の角度を計算
        float rotationAngle = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
        
        // 弾に速度を設定（Rigidbody2Dがある場合）
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) {
            // 重力を無効化して一定速度で移動
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * bulletSpeed;
        }
        
        // 生存時間をランダムに決定（MINからMAXの間）
        float randomLifetime = Random.Range(BULLET_MIN_LIFETIME, BULLET_MAX_LIFETIME);
        
        // ランダムな時間後にプールに戻すコルーチンを開始
        StartCoroutine(_ReturnBulletAfterDelay(bullet, randomLifetime));
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
