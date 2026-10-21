using UnityEngine;

public class Ability_Water : Ability_Base
{
    // 射撃アニメーション
    private const string _SHOT_ANIM = "Marlica_Shot";

    // 最大弾数
    private int _maxShoot = 3;
    private int _currentShot = 0;

    // 弾オブジェクト
    [SerializeField] private WaterBullet _bulletObj;
    // 発射地点
    [SerializeField] private Transform _shotPoint;

    protected override void _Update() {
        base._Update();

        // 位置更新
        UpdatePartnerTransform();
    }

    public override eAbilityResult ExecuteSimple() {
        return _TryShot();
    }

    /// <summary>
    /// 攻撃判定
    /// </summary>
    private eAbilityResult _TryShot() {
        // オーバーヒート中は使用不可
        if (_cannotUse) {
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }

        var ability_result = _SimpleShot();

        // 攻撃実行
        if (ability_result != eAbilityResult.None) {
            _AppearCheck(eAbilityType.Water);
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

        var shot_pos = transform.position;
        if(_shotPoint != null) {
            var shot_offset = _shotPoint.localPosition;
            shot_offset.x = Mathf.Abs(shot_offset.x) * (_isRight ? 1 : -1);
            shot_pos += shot_offset;
        }

        var bullet = Instantiate(_bulletObj, shot_pos, Quaternion.identity);
        bullet.SetCallback(() => _currentShot--);
        bullet.IsRight = _isRight;
        // 発射者の横移動速度のみを弾の初速に乗せる(縦方向は無視)
        var player = PlayerCharacterManager.Current;
        float inherited_x = player != null ? player.Velocity.x : 0f;
        bullet.SetInheritedVelocity(new Vector2(inherited_x, 0f));

        // 進行方向に合わせて反転
        var scale = bullet.transform.localScale;
        scale.x *= (_isRight ? 1 : -1);
        bullet.transform.localScale = scale;

        _currentShot++;
        return eAbilityResult.FireShot;
    }
}
