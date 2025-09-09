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

    public override void SetCharacterTransform(bool is_right, Transform chara_transform, CommonParameter common_param,  CharacterParameter chara_param) {
        base.SetCharacterTransform(is_right, chara_transform, common_param, chara_param);
        UpdateTransform(chara_transform.position, _inputDir);
    }

    public override eAbilityResult ExecuteSimple() {
        // オーバーヒート中は使用不可
        if (_cancelByOverheat) {
            return eAbilityResult.None;
        }

        var ability_result = _SimpleShot(_charaTransform.position);

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

        _currentShot++;
        return eAbilityResult.FireShot;
    }
}
