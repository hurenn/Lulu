using System.Collections;
using UnityEngine;

public class Enemy_Base : Character_Base
{
    [SerializeField]
    private int _exp = 1; // 経験値

    protected override IEnumerator Die() {
        // 経験値取得
        PlayerParameter.Instance.AddExp(_exp);

        return base.Die();
    }
}
