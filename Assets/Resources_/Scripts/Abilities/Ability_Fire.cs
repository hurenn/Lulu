using UnityEngine;

public class Ability_Fire : Ability_Base
{
    // 射撃アニメーション
    private const string _SHOT_ANIM = "Shot";

    // 最大弾数
    private int _maxShoot = 3;
    private int _currentShot = 0;

    // 弾オブジェクト
    [SerializeField] private FireBullet _bulletObj;

    public override void SetCharacterTransform(bool isRight, Vector3 character_pos) {
        base.SetCharacterTransform(isRight, character_pos);
        UpdateTransform(character_pos, _inputDir);
    }

    public override eAbilityResult ExecuteSimple() {
        var ability_result = _SimpleShot(_characterPosition);
        if(ability_result != eAbilityResult.None) {
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
        if (!_anim.GetCurrentAnimatorStateInfo(0).IsName(_SHOT_ANIM)) {
            Instantiate(_warpAnimationPrefab, transform.position, Quaternion.identity);
        }
        _anim?.Play(_SHOT_ANIM, 0, 0.0f);

        var bullet = Instantiate(_bulletObj, transform.position, Quaternion.identity);
        bullet.SetCallback(() => _currentShot--);
        bullet.IsRight = _isRight;

        _currentShot++;
        return eAbilityResult.FireShot;
    }
}
