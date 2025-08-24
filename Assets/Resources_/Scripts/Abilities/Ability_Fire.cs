using UnityEngine;

public class Ability_Fire : Ability_Base
{
    // 最大弾数
    private int _maxShoot = 3;
    private int _currentShot = 0;

    // 弾オブジェクト
    [SerializeField] private FireBullet _bulletObj;

    public override eAbilityResult ExecuteSimple() {
        var ability_result = _SimpleShot();
        if(ability_result != eAbilityResult.None) {
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

        var bullet = Instantiate(_bulletObj, transform.position, Quaternion.identity);
        bullet.SetCallback(() => _currentShot--);

        _currentShot++;
        return eAbilityResult.FireShot;
    }
}
