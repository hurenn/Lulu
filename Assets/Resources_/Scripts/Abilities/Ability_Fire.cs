using System.Collections.Generic;
using UnityEngine;

public class Ability_Fire : Ability_Base
{
    // 射撃アニメーション
    private const string _SHOT_ANIM = "Marlica_Shot";

    // 最大弾数
    private int _maxShoot = 3;
    private int _currentShot = 0;

    // 弾オブジェクト
    [SerializeField] private FireBullet _bulletObj;

    // 自動攻撃範囲
    [SerializeField] private Collider2D _autoAttackRange;
    // 範囲内の敵リスト
    private List<Enemy_Base> _enemiesInRange = new List<Enemy_Base>();
    // 自動攻撃間隔
    private float _autoAttackInterval = 0.4f;
    private float _currentAutoAttackInterval = 0.0f;
    
    // トリガーヘルパー参照
    private FireAutoAttackTrigger _triggerHelper;

    public override void Setup(bool is_right, Transform chara_transform, CommonParameter common_param,  CharacterParameter_Player chara_param, WarpControl warp_control) {
        base.Setup(is_right, chara_transform, common_param, chara_param, warp_control);
        
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

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat) {
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
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
