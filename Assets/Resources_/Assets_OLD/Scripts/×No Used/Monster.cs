using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;
    GameObject drop;

    Animator anim;
    SpriteRenderer rend;
    Rigidbody2D rb;
    public float hp = 300;
    public float whiteTime = 0.3f;
    public float impX = 0.1f;
    public float impY = 0.1f;
    bool end = false;
    float timer = 0;

    // Use this for initialization
    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponentInChildren<MonsterAtack>().MasterPos = transform.position;

        if (end == true)
        {
            if (hp < -20)
            {
                drop = Instantiate(coin3, transform.position, transform.rotation) as GameObject;
                impulse();
                hp += 50;
            }
            else if (hp < -10)
            {
                drop = Instantiate(coin2, transform.position, transform.rotation) as GameObject;
                impulse();
                hp += 3;
            }
            else if (hp < -5)
            {
                drop = Instantiate(coin1, transform.position, transform.rotation) as GameObject;
                impulse();
                hp += 1;
            }
            timer += Time.deltaTime;
            if (timer > 2.0f)
                Destroy(gameObject);
        }
        else
        {
            if (GetComponentInChildren<MonsterAtack>().timer < 150)
            {
                if (GameObject.Find("Player").transform.position.x - transform.position.x > 0 && GetComponentInChildren<MonsterAtack>().discover == true)
                    GetComponentInChildren<MonsterAtack>().left = false;
                else if (GameObject.Find("Player").transform.position.x - transform.position.x < 0 && GetComponentInChildren<MonsterAtack>().discover == true)
                    GetComponentInChildren<MonsterAtack>().left = true;
            }

            if (GetComponentInChildren<MonsterAtack>().left == true)
                rend.flipX = false;
            else
                rend.flipX = true;

            if (GetComponentInChildren<MonsterAtack>().timer > 150 && GetComponentInChildren<MonsterAtack>().timer < 160)
                anim.Play("MonsterAtack");
            else
                anim.Play("Monster");

        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (end == false)
        {
            if (collision.gameObject.tag.Equals("Object"))
            {
                Vector2 direction = collision.gameObject.transform.position - transform.position;
                collision.gameObject.GetComponent<Rigidbody2D>().AddForce(direction * 10, ForceMode2D.Impulse);
                //hp -= collision.gameObject.GetComponent<HangedObject>().atackPower;
                if (hp <= 0)
                {
                    this.gameObject.layer = LayerMask.NameToLayer("Default");
                    anim.Play("MonsterPunish");
                    end = true;
                    return;
                }
                SE.playnum = 8;
                anim.Play("MonsterWhite");
                StartCoroutine("Hoge");
            }
            if (collision.gameObject.tag.Equals("Trap"))
            {
                hp -= collision.gameObject.GetComponent<Needle>().EnemyDamage;
                if (hp <= 0)
                {
                    this.gameObject.layer = LayerMask.NameToLayer("Default");
                    anim.Play("MonsterPunish");
                    end = true;
                    return;
                }
                anim.Play("MonsterWhite");
                StartCoroutine("Hoge");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (end == false)
        {
            if (collision.gameObject.tag.Equals("PlayerAtack"))
            {
                hp -= 1000;
                if (hp <= 0)
                {
                    this.gameObject.layer = LayerMask.NameToLayer("Default");
                    SE.playnum = 13;
                    anim.Play("MonsterPunish");
                    end = true;
                    return;
                }
                anim.Play("MonsterWhite");
                StartCoroutine("Hoge");
            }
            if (collision.gameObject.tag.Equals("EnemyAtack"))
            {
                hp -= 100;
                if (hp <= 0)
                {
                    this.gameObject.layer = LayerMask.NameToLayer("Default");
                    SE.playnum = 13;
                    anim.Play("MonsterPunish");
                    end = true;
                    return;
                }
                anim.Play("MonsterWhite");
                StartCoroutine("Hoge");
            }
        }
    }

    IEnumerator Hoge()
    {
        yield return new WaitForSeconds(whiteTime);
        rb.AddForce(new Vector2(impX, impY), ForceMode2D.Impulse);
        anim.Play("Monster");
    }

    private void impulse()
    {
            drop.GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-4f, 4f), Random.Range(-5f, 5f)), ForceMode2D.Impulse);
    }
}
