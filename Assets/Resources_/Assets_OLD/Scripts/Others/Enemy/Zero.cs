using Fungus;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Zero : MonoBehaviour
{
    float timer = 0;
    public float interval = 4f;
    public GameObject ballet;
    public bool left = true;
    Animator anim;
    SpriteRenderer rend;
    GameObject player;
    bool seFlag;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        anim = GetComponent<Animator>();
        rend = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Enemy>().HP <= 0)
        {
            return;
        }

        if (player.transform.position.x > transform.position.x)
            left = false;
        else
            left = true;

        if (left == true)
        {
            rend.flipX = false;
        }
        else
        {
            rend.flipX = true;
        }

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            anim.Play("Stand");
            timer = 0;
            Shot();
        }
        else if (timer >= interval - 1f)
        {
            anim.Play("Charge");
            if (!seFlag)
            {
                SE.playnum = 9;
                seFlag = true;
            }
        }
    }

    void Shot()
    {

        SE.playnum = 5;
        if (left == true)
        {
            GameObject @object = Instantiate(ballet, new Vector2(transform.position.x - 1f, transform.position.y + 1f), Quaternion.identity);
            @object.GetComponent<Rigidbody2D>().AddForce(new Vector2(-25, 10), ForceMode2D.Impulse);
        }
        else
        {
            GameObject @object = Instantiate(ballet, new Vector2(transform.position.x + 1f, transform.position.y + 1f), Quaternion.identity);
            @object.GetComponent<Rigidbody2D>().AddForce(new Vector2(25, 10), ForceMode2D.Impulse);
        }
        seFlag = false;
    }
}
