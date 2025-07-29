using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HangedObject : MonoBehaviour
{
    GameObject Player;
    SpriteRenderer rend;
    Rigidbody2D rb;
    public Vector2 force;
    Vector2 defaultPos;

    public ParticleSystem explosion;
    public PhysicsMaterial2D ItemFire;
    public PhysicsMaterial2D Item;
    float fireTimer = 1.5f;
    public bool velocityDebug;

    //個体値
    public float power = 30f;
    public float posx = 0.8f;
    public float posy = 0.12f;
    public float heavy = 1f;
    int atackPower;

    //計算
    public float speed = 0;
    public bool hanged = false;

    // Use this for initialization
    void Start()
    {
        defaultPos = new Vector2(posx, posy);
        Player = GameObject.Find("Player");
        rend = GetComponent<SpriteRenderer>();
        rb = this.GetComponent<Rigidbody2D>();
        atackPower = GetComponent<DamageZone>().EnemyDamage;
    }

    // Update is called once per frame
    void Update()
    {
        if (velocityDebug)
            Debug.Log(rb.linearVelocity);

        if (this.gameObject.GetComponent<DamageZone>().EnemysAtack == false && this.gameObject.GetComponent<DamageZone>().PlayersAtack == false &&
            this.gameObject.GetComponent<DamageZone>().NeutralAtack == false && rb.linearVelocity.y > 1.5f)
        {
            this.gameObject.GetComponent<DamageZone>().PlayersAtack = true;
        }

            if (PlayerInput.x == true && Hang.hanging == false)
        {
            gameObject.layer = LayerMask.NameToLayer("ItemH");
        }
        else
        {
            gameObject.layer = LayerMask.NameToLayer("ItemG");
        }

        speed = Mathf.Abs(rb.linearVelocity.x) + Mathf.Abs(rb.linearVelocity.y);
        if (speed < 1)
        {
            speed = 1;
        }

        if (hanged == true)
        {
            Marlica();

            transform.rotation = new Quaternion(0, 0, 0, 0);
            rb.linearVelocity = new Vector2(0, 0);
            /*
            if (RuruAnime.left == false)
            {
                this.transform.position = new Vector2(Player.transform.position.x + posx, Player.transform.position.y + posy);
                rend.flipX = false;
            }
            else
            {
                this.transform.position = new Vector2(Player.transform.position.x - posx, Player.transform.position.y + posy);
                rend.flipX = true;
            }
            */
            this.transform.parent = Player.transform;

            Calculation();

        }
        else
        {
            if (fireTimer < 1.5f)
            {
                fireTimer += Time.deltaTime;
            }
            else
            {
                if (GetComponent<Rigidbody2D>().gravityScale == 0)
                {
                    power = 30f;
                    transform.GetChild(0).gameObject.SetActive(false);
                    GetComponent<Rigidbody2D>().GetComponent<Material>().Equals(Item);
                    GetComponent<Rigidbody2D>().gravityScale = 4;
                }
            }
        }
    }

    void Marlica()
    {
        if (Friends.Marlica)
        {
            GetComponent<DamageZone>().EnemyDamage = atackPower * 2;
            power = 50f;
            transform.GetChild(0).gameObject.SetActive(true);
            GetComponent<Rigidbody2D>().GetComponent<Material>().Equals(ItemFire);
            GetComponent<Rigidbody2D>().gravityScale = 0;
            fireTimer = 0;
        }
        else
        {
            GetComponent<DamageZone>().EnemyDamage = atackPower;
            power = 30f;
            transform.GetChild(0).gameObject.SetActive(false);
            GetComponent<Rigidbody2D>().GetComponent<Material>().Equals(Item);
            GetComponent<Rigidbody2D>().gravityScale = 4;
        }
    }

    //追加分　武器判定
    void OnCollisionStay2D(Collision2D col)
    {
        if ((col.gameObject.tag == "Ground" || col.gameObject.tag == "Object" || col.gameObject.tag == "Trap" || col.gameObject.tag == "itemB") &&
            rb.linearVelocity.x < 0.5f && rb.linearVelocity.x > -0.5f && rb.linearVelocity.y < 0.5f && rb.linearVelocity.y > -0.5f)
        {
            if (this.gameObject.GetComponent<DamageZone>().EnemysAtack == true)
                this.gameObject.GetComponent<DamageZone>().EnemysAtack = false;
            if (this.gameObject.GetComponent<DamageZone>().PlayersAtack == true)
                this.gameObject.GetComponent<DamageZone>().PlayersAtack = false;
            if (this.gameObject.GetComponent<DamageZone>().NeutralAtack == true)
                this.gameObject.GetComponent<DamageZone>().NeutralAtack = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer.Equals(16) && Friends.Marlica && GetComponent<DamageZone>().PlayersAtack)
        {
            Instantiate(explosion, this.transform);
        }
    }

    void Calculation()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                force = new Vector2(power * 0.7f, power * 0.5f);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                force = new Vector2(power * 0.7f, -power * 0.5f);
            }
            else
            {
                force = new Vector2(power, power * 0.2f);
            }
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                force = new Vector2(-power * 0.7f, power * 0.5f);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                force = new Vector2(-power * 0.7f, -power * 0.5f);
            }
            else
            {
                force = new Vector2(-power, power * 0.2f);
            }
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            force = new Vector2(0, power * 0.6f);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            force = new Vector2(0, -power * 0.6f);
        }
        else
        {
            force = new Vector2(0, 0);

        }
    }
}
