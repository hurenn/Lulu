using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Needle : MonoBehaviour
{
    public int EnemyDamage = 400;
    public int PlayerDamage = 100;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnCollisionStay2D(Collision2D Col)
    {
        if (Col.gameObject.tag == "Player")
        {
            Col.gameObject.GetComponent<HPManager>().Damage(PlayerDamage);
        }
    }

    public static void PlayerHit(int PlayerDamage)
    {
        
        #region 没
        /*
        if (Player.invinceTime >= Player.maxInvince)
        {
            Player.invinceTime = 0f;
            if (!WarpControl.overHeat)
            {
                if (WarpControl.nowMagic < PlayerDamage)
                {
                    Life.nowLife -= PlayerDamage - WarpControl.nowMagic;
                    Player.invinceTime = 0f;
                }
                else
                {
                    WarpControl.nowMagic -= PlayerDamage;
                }
            }
            else
            {
                Life.nowLife -= PlayerDamage;
                Player.invinceTime = 0f;
            }
            if (Life.over == false)
                WarpControl.nowMagic -= PlayerDamage;
        }
        */
        #endregion
    }

}
