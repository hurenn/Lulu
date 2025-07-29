using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ballet : MonoBehaviour
{

    public GameObject WarpEnd;
    public GameObject ParticleEffect;
    public int direct = 0; //0=左　1=停止　2=右
    Vector3 startPos;
    public Vector3 Target;
    public float speed = 0.5f;
    

    // Use this for initialization
    void Start()
    {
        this.gameObject.GetComponent<DamageZone>().Stay = true;
        startPos = transform.position;

        //StartCoroutine("wait");   チャージ演出
        //Target = GameObject.Find("Player").transform.position + (GameObject.Find("Player").transform.position - transform.position) * 100;    //追尾
        //iTween.ShakePosition(gameObject, iTween.Hash("x", 0.4f, "y", 0.5f, "time", 0.8f));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (direct == 0)
        {
            transform.position = new Vector2(transform.position.x - speed, transform.position.y);
        }
        else if (direct == 2)
        {
            transform.position = new Vector2(transform.position.x + speed, transform.position.y);
        }

        if (this.gameObject.GetComponent<DamageZone>().PlayersAtack && this.gameObject.GetComponent<DamageZone>().Stay != false)
        {
            this.gameObject.GetComponent<DamageZone>().Stay = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("Enemy") || collision.gameObject.tag.Equals("Ground") || collision.gameObject.tag.Equals("Object"))
        {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        //Debug.Log("Player Hit ," + " counter = " + WarpControl.counter);

        if (this.gameObject.GetComponent<DamageZone>().EnemysAtack)
        {
            if (collision.gameObject.tag.Equals("CounterArea"))// && WarpControl.counter == true)
            {
                StartCoroutine("Counter");
                this.gameObject.GetComponent<DamageZone>().PlayersAtack = true;
                this.gameObject.GetComponent<DamageZone>().EnemysAtack = false;
            }
            if(collision.gameObject.tag.Equals("Player"))// && WarpControl.counter == false)
            {
                Needle.PlayerHit(GetComponent<Weapon>().EnemysAtack);
                Destroy(this.gameObject);
            }
        }
    }

    IEnumerator Counter()
    {
        if (direct == 0)
        {
            Time.timeScale = 0.7f;
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0f);
            Instantiate(WarpEnd, this.transform);
            direct = 1;

            yield return new WaitForSeconds(0.2f);

            Time.timeScale = 1.0f;
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            Instantiate(ParticleEffect, this.transform);
            direct = 2;
        }
        else
        {
            Time.timeScale = 0.7f;
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0f);
            Instantiate(WarpEnd, this.transform);
            direct = 1;

            yield return new WaitForSeconds(0.2f);

            Time.timeScale = 1.0f;
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            Instantiate(ParticleEffect, this.transform);
            direct = 0;
        }

    }
}

