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

    public override void Setup(bool is_right, Transform chara_transform, CommonParameter common_param,  CharacterParameter_Player chara_param, WarpControl warp_control) {
        base.Setup(is_right, chara_transform, common_param, chara_param, warp_control);
        UpdatePartnerTransform();
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat) {
            UpdatePartnerTransform(); // 位置更新
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity); // 召喚エフェクト再生
            return eAbilityResult.None;
        }

        var ability_result = _SimpleShot(_playerTransform.position);

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
    private eAbilityResult _SimpleShot(Vector3 character_pos) {
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
}
