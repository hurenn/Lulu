using Fungus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin1 : MonoBehaviour
{
    public int value = 1;
    public int plusMagic = 15;
    public bool gem = false;//追尾機能付き
    public bool follow = false;//追尾開始
    float speed = 0f;//追尾速度
    float accel = 0.1f;
    public int plusPower = 5;
    float ScatterPower = 20;

    public Rigidbody2D rb;
    Vector2 force;
    Vector2 parentPos;
    GameObject GetGem;

    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GetGem = (GameObject)Resources.Load("Get Gem");
    }

    public void Scatter(Vector2 force, Vector2 pos, bool auto)
    {
        parentPos = pos;
        this.force = force * 2 * ScatterPower;
        GetComponent<CapsuleCollider2D>().isTrigger = false;
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(this.force, ForceMode2D.Impulse);
        if(auto)
            StartCoroutine("autoFollow");
    }
    void ScatterEnd()
    {
        //c2 = a2 + b2 - 2ab * cos<c
        double r = Math.Pow(transform.position.x - parentPos.x, 2) + Math.Pow(transform.position.y - parentPos.y, 2)
            - 2 * Math.Abs(transform.position.x - parentPos.x) * Math.Abs(transform.position.y - parentPos.y) * Math.Cos(1.5708);
        if (Math.Sqrt(r) > 5)
        {
            rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(0, 0);
            GetComponent<CapsuleCollider2D>().isTrigger = true;
        }
    }

    void FollowMove()
    {
        Vector3 player = GameObject.Find("Player").transform.position;
        Vector3 direction = player - transform.position;
        direction.Normalize();

        transform.position += direction * speed;
        if (speed < 0.5f)
            speed += accel;

        if (player.x + 1f > transform.position.x && player.x - 1f < transform.position.x && player.y + 1f > transform.position.y && player.y - 1f < transform.position.y)
        {
            transform.position = player;
        }
        transform.position += new Vector3(Mathf.Sin(0.1f), 0f, 0f);
    }

    IEnumerator autoFollow()
    {
        yield return new WaitForSeconds(1f);
        follow = true;
    }

    // Update is called once per frame
    void Update()
    {
        //散らばり
        if (parentPos != null)
        {
            ScatterEnd();
        }

        //吸い寄せ
        if (follow == true)
        {
            FollowMove();
        }
    }

    private void OnCollisionEnter2D(UnityEngine.Collision2D collision)
    {
        if (collision.gameObject.tag.Equals("Player"))
        {
            GameObject getEffect = (GameObject)Instantiate(GetGem, transform.position, Quaternion.identity);
            SE.playnum = 4;
            CollectCoin.Collected += value;
            if (!WarpControl.overHeat && !gem)
                WarpControl.nowMagic = Mathf.Clamp(WarpControl.nowMagic + plusMagic, 0, WarpControl.maxMagic);
            collision.gameObject.GetComponent<Lulu>().PowerUp(plusPower);
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("Player"))
        {
            GameObject getEffect = (GameObject)Instantiate(GetGem, transform.position, Quaternion.identity);
            SE.playnum = 4;
            CollectCoin.Collected += value;
            if(!WarpControl.overHeat && !gem)
                WarpControl.nowMagic = Mathf.Clamp(WarpControl.nowMagic + plusMagic, 0, WarpControl.maxMagic);
            collision.gameObject.GetComponent<Lulu>().PowerUp(plusPower);
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("CoinCheck"))
        {
            GetComponent<CapsuleCollider2D>().isTrigger = true;
            if (gem == true)
            {
                follow = true;
            }
        }
    }
}
