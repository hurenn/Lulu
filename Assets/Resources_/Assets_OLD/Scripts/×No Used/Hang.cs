using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hang : MonoBehaviour
{
    public static bool hanging = false;//掴んだ
    public static bool cool = false;//他からの静止
    public static bool hang = false;//掴みボタン押した
    public static bool imp = false;//投げました合図
    GameObject itemH;
    GameObject player;
    Rigidbody2D rb;
    Vector2 force;

    // Use this for initialization
    void Start()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("x = " + PlayerInput.x + ", hang = " + hang + ", cool = " + cool);
        //Debug.Log("imp = " + imp + ", hanging = " + hanging);
        if (imp == true)
        {
            imp = false;
        }
        if (PlayerInput.x == true && hanging == false && cool == false)//掴み準備
        {
            hang = true;
        }
        if (PlayerInput.x == false && cool == false)//投げる
        {
            hang = false;
            if (hanging == true)
            {
                itemH.GetComponent<DamageZone>().PlayersAtack = true;
                StartCoroutine("Stop");
                imp = true;
                Impulse();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Object" && hang == true)//掴む
        {
            StartCoroutine("Stop");
            SE.playnum = 12;
            //collision.gameObject.tag = ("Hanged");
            itemH = collision.gameObject;
            itemH.GetComponent<HangedObject>().hanged = true;
            //itemH.tag = "Hanged";
            rb = itemH.GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.SetRotation(0);
            hanging = true;
        }
    }

    void Impulse()
    {
        force = itemH.GetComponent<HangedObject>().force;
        itemH.GetComponent<HangedObject>().hanged = false;
        itemH.transform.parent = null;
        rb.freezeRotation = false;
        rb.AddForce(force, ForceMode2D.Impulse);
        itemH.tag = "Object";

        hanging = false;
    }

    IEnumerator Stop()
    {
        //Player.stop = true;
        yield return new WaitForSeconds(0.1f);
        //Player.stop = false;
    }
}
