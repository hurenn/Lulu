using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAtack : MonoBehaviour
{
    public bool discover = false;
    public bool shake = false;

    bool exitStart = false;
    float exitCount = 0;
    bool countStart = false;
    public static bool atackStart = false;
    public int timer = 0;
    public GameObject ballet;
    Vector3 shot;
    public bool left = true;
    SpriteRenderer rend;
    Vector2 rightpos;
    public Vector3 MasterPos;

    // Use this for initialization
    void Start()
    {
        rend = GetComponentInParent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(rend.flipX == true)
        {
            transform.position = new Vector2(MasterPos.x + 27, MasterPos.y);
        }
        else
        {
            transform.position = new Vector2(MasterPos.x, MasterPos.y);
        }

        if (countStart == true)
        {
            timer += 1;
        }
        if (timer == 150)
        {
            StartCoroutine("Shoot");
        }

    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag.Equals("Player"))
        {
            discover = true;
            countStart = true;
        }/*
        if (collision.tag.Equals("EnemyAtack"))
        {
            collision.GetComponent<Ballet>().Master = gameObject.transform.position;
        }*/
    }

    private void OnTriggerExit2D(Collider2D collision)
    {

        if (collision.tag.Equals("Player"))
        {
            discover = false;
        }
    }

    IEnumerator Shoot()
    {
        SE.playnum = 9;
            if (left == true)
                shot = new Vector3(MasterPos.x - 3, MasterPos.y, MasterPos.z);
            else
                shot = new Vector3(MasterPos.x + 3, MasterPos.y, MasterPos.z);
            GameObject atack = Instantiate(ballet, shot, transform.rotation);
            if(atack.transform.parent == null)
                atack.transform.parent = transform;

        shake = true;
        yield return new WaitForSeconds(1f);
        shake = false;
        yield return new WaitForSeconds(0.5f);

        SE.playnum = 5;
        atackStart = true;

            timer = 0;
            countStart = false;
    }
}
