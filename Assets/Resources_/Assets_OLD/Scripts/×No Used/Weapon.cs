using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int PlayersAtack = 50;
    public int EnemysAtack = 100;
    public int state = 0;   //0=N 1=P's atack 2=E's atack
    // Start is called before the first frame update
    void Start()
    {
        //要らないスクリプト
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollision2D(Collision2D col)
    {
        if(col.gameObject.tag == "Ground" && state != 0)
        {
            state = 0;
        }
    }
}
