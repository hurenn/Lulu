using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public int PlayerDamage = 100;
    public int EnemyDamage = 100;
    public bool PlayersAtack = false;
    public bool EnemysAtack = false;
    public bool NeutralAtack = false;
    public float diray = 0;

    public bool Stay = false;   //trueで攻撃が通らなくなる
    public bool Disappear = false;

    public bool HitDebug;
    public bool invinceTimeSpecific;
    public float SpecialInvinceTime = 5;

    public bool Attakable = true;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (diray > 0 && Attakable == false)
        {
            diray -= Time.deltaTime;
        }
        if (diray < 0 && Attakable == false)
        {
            Attakable = true;
        }
    }
    private void OnTriggerStay2D(Collider2D Collider)//Trigger版ダメージ判定
    {
        if (Stay == false)
        {
            /*
            if (HitDebug)
            {
                if (Collider.gameObject.layer.Equals(16))
                    Debug.Log(Collider.gameObject.GetComponent<Enemy>().invinceTime + " >?" + Collider.gameObject.GetComponent<Enemy>().GetMaxInvince());
                else
                    Debug.Log(Collider.gameObject.layer);
            }*/

            if (EnemysAtack == true || NeutralAtack == true)    //プレイヤーダメージ判定
                if (Collider.gameObject.tag.Contains("Player"))
                {
                    if (HitDebug)
                        Debug.Log("TriggerHit Player");

                    //プレイヤーダメージ
                    PlayerHit(PlayerDamage, Collider.gameObject);
                }

            if (PlayersAtack == true || NeutralAtack == true)   //敵ダメージ判定
                if (Collider.gameObject.layer.Equals(16)    //レイヤーネーム「エネミー」
                    && Collider.gameObject.GetComponent<Enemy>().invinceTime >= Collider.gameObject.GetComponent<Enemy>().GetMaxInvince())
                {
                    if (HitDebug)
                        Debug.Log("TriggerHit Enemy");

                    //Collider.gameObject.GetComponent<Enemy>().HP -= EnemyDamage;

                    //敵ダメージ
                    Collider.GetComponent<Enemy>().Damage(EnemyDamage, 0);
                    if (invinceTimeSpecific)
                    {
                        Collider.gameObject.GetComponent<Enemy>().invinceTime = SpecialInvinceTime;
                    }
                    else
                    {
                        Collider.gameObject.GetComponent<Enemy>().invinceTime = 0;
                    }
                    if (Disappear)
                        Destroy(this.gameObject);
                }
        }
    }
    private void OnCollisionStay2D(Collision2D Collider)    //Collision版ダメージ判定
    {
        if (Stay == false)
        {
            if (EnemysAtack == true || NeutralAtack == true)
                if (Collider.gameObject.tag == "Player")
                {
                    if (HitDebug)
                        Debug.Log("TriggerHit Player");
                    PlayerHit(PlayerDamage, Collider.gameObject);
                }

            if (PlayersAtack == true || NeutralAtack == true)
                if (Collider.gameObject.layer.Equals(16)    //レイヤーネーム「エネミー」
                    && Collider.gameObject.GetComponent<Enemy>().invinceTime >= Collider.gameObject.GetComponent<Enemy>().GetMaxInvince())
                {
                    if (HitDebug)
                        Debug.Log("TriggerHit Enemy");
                    //Collider.gameObject.GetComponent<Enemy>().HP -= EnemyDamage;
                    Collider.gameObject.GetComponent<Enemy>().Damage(EnemyDamage, 0);
                    if (invinceTimeSpecific)
                    {
                        Collider.gameObject.GetComponent<Enemy>().invinceTime = SpecialInvinceTime;
                    }
                    else
                    {
                        Collider.gameObject.GetComponent<Enemy>().invinceTime = 0;
                    }
                    if (Disappear)
                        Destroy(this.gameObject);
                }
        }
    }
    void PlayerHit(int PlayerDamage, GameObject player) //プレイヤーダメージ処理
    {
        if (Attakable)
            player.GetComponent<HPManager>().Damage(PlayerDamage);
    }

}
