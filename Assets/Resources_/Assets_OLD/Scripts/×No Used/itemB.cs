using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class itemB : MonoBehaviour
{
    public bool Breakable = false;
    public bool grab = false;
    int step = 0;
    bool over = false;
    GameObject check;
    bool rightThrow = false;
    bool leftThrow = false;
    bool upThrow = false;
    bool downThrow = false;
    bool throwing = false;
    Animator anim;
    public bool end = false;
    Vector2 limit;
    bool down = false;
    bool touch = false;
    bool warpControl = false;
    GameObject player;
    public int PlayersAtack = 300;
    bool AtackFlag = false;
    float debugTime = 0;
    Vector2 checkPos = new Vector2(-0.04f, -0.37f);
    

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(Player.grabB);
        if(debugTime >= 1)
        {
            debugTime += Time.deltaTime;
        }
        if(debugTime >= 7f)
        {
            Reset();
        }
        //Debug.Log("step = "+step +", end = "+ end + ", AtackFlag = " + AtackFlag + ", debugTime = " + debugTime);

        touch = transform.GetChild(2).GetComponent<Touch>().touch;
        if (rightThrow == true)
        {
            Right();
        }
        else if (leftThrow == true)
        {
            Left();
        }
        else if (upThrow == true)
        {
            Up();
        }

        if (throwing == true && end == true && warpControl == false)
        {
            warpControl = true;
            StartCoroutine("warp");
        }

        if (grab == true || throwing == true)
            Direction();

        //Debug.Log("stop = " + Player.stop + ", breakable = " + Breakable);
        if (throwing == false && grab == false && touch == true && Hang.hanging == false)
        {
            grab = true;
            if (OnGround.jump == true)
            {
                GameObject.Find("Player").GetComponent<WarpControl>().SetBan(true);
                Player.grabB = true;
                RuruAnime.grabB = true;
            }
        }


        if (grab == true)//掴み
        {
            Grab();
            if (PlayerInput.x == false)//投げ
            {
                if (OnGround.jump == true)
                {
                    //Player.grabB = false;
                    Player.throwB = true;
                    RuruAnime.throwB = true;
                    RuruAnime.grabB = false;
                }
                throwing = true;
            }
        }
        if (throwing == true && grab == true)
        {
            grab = false;
            Throw();
        }
    }

    void Grab()
    {
        Hang.cool = true;   //アイテム同時投げの不可
        transform.GetChild(0).tag = "itemB Check";
        check = GameObject.FindWithTag("itemB Check");

        //Debug.Log("1 stop = " + Player.stop + ", Warp = " + WarpControl.ban + ", throwH = " + Hang.cool);

        if (transform.position.x < GameObject.Find("Player").transform.position.x)
            RuruAnime.left = true;
        else
            RuruAnime.left = false;
    }
    void Throw()
    {
        if (WarpControl.overHeat)
        {
            Reset();
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.UpArrow))
        {
            WarpControl warpControl = player.GetComponent<WarpControl>();
            warpControl.Cost();
            anim.Play("Warp Effect 0");
            GetComponent<Rigidbody2D>().gravityScale = 0;
            //GetComponent<BoxCollider2D>().isTrigger = true;   チェックも判定してしまうからダメ
            this.gameObject.layer = LayerMask.NameToLayer("NotTouch");
            AtackFlag = true;
            debugTime = 1;
        }
        else
        {
            Reset();
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (AtackFlag == true)
        {
            if (collision.gameObject.layer.Equals(16))
            {/*
                collision.gameObject.GetComponent<Enemy>().EnemyHit(PlayersAtack);
                */
            }
            if (Player.invinceTime >= Player.maxInvince && collision.gameObject.tag == "Player")//自傷ダメージ
            {
                Needle.PlayerHit(5);
            }
            if (collision.gameObject.tag == "Ground")
            {
                debugTime = 0;
                AtackFlag = false;
                if (Breakable == true)
                {
                    StartCoroutine("Break");
                }
                else
                {
                    End();
                }
            }
        }
    }

    IEnumerator warp()
    {
        //Debug.Log("2 stop = " + Player.stop);
        transform.position = transform.GetChild(0).position;
        transform.GetChild(0).position = (Vector2)transform.position + checkPos;
        anim.Play("Warp Effect_none");
        yield return new WaitForSeconds(0.6f);
        Player.grabB = false;
        Player.throwB = false;//投げアニメ
        RuruAnime.grabB = false;
        RuruAnime.throwB = false;

        this.gameObject.layer = LayerMask.NameToLayer("Obstacle");

        yield return new WaitForSeconds(0.3f);
        GetComponent<Rigidbody2D>().gravityScale = 30;
        transform.GetChild(0).tag = "Untagged";

        End();
    }

    void End()
    {
        GameObject.Find("Player").GetComponent<WarpControl>().SetBan(false);
        end = false;
        throwing = false;
        rightThrow = false;
        leftThrow = false;
        upThrow = false;
        downThrow = false;
        over = false;
        down = false;
        warpControl = false;
        Hang.cool = false;//アイテム投げ復活
        //this.gameObject.layer = LayerMask.NameToLayer("Obstacle");
        transform.GetChild(0).tag = "Untagged";
    }
    void Reset()
    {
        RuruAnime.freez = false;
        grab = false;
        GameObject.Find("Player").GetComponent<WarpControl>().SetBan(false);
        Player.grabB = false;
        Player.throwB = false;
        RuruAnime.grabB = false;
        RuruAnime.throwB = false;
        end = false;
        throwing = false;
        rightThrow = false;
        leftThrow = false;
        upThrow = false;
        downThrow = false;
        over = false;
        down = false;
        warpControl = false;
        AtackFlag = false;
        debugTime = 0;
        Hang.cool = false;//アイテム投げ復活
        //this.gameObject.layer = LayerMask.NameToLayer("Obstacle");
        //gameObject.tag = "itemB";
        transform.GetChild(0).tag = "Untagged";
    }

    IEnumerator Break()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponent<SpriteRenderer>().color = new Color(1f,1f,1f,0);
        yield return new WaitForSeconds(0.1f);
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
        yield return new WaitForSeconds(0.1f);
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0);
        yield return new WaitForSeconds(0.1f);
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
        yield return new WaitForSeconds(0.1f);
        gameObject.SetActive(false);
    }

    void Right()
    {
        if(step == 0 && check.GetComponent<itemBcheck>().inWall == true)
        {
            check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
            return;
        }

        if (GameObject.Find("TargetCheck Right").GetComponent<TargetCheck>().lockOn == true)
        {
            //Debug.Log("LockOn");
            if (transform.position.x != GameObject.Find("TargetCheck Right").GetComponent<TargetCheck>().target.x)
            {
                check.transform.position = new Vector2(GameObject.Find("TargetCheck Right").GetComponent<TargetCheck>().target.x, check.transform.position.y);
            }
                limit = new Vector2(GameObject.Find("Warp Default Up").transform.position.x, GameObject.Find("Warp Default Up").transform.position.y);

            if (over == false)
            {
                if (check.transform.position.y < limit.y)
                    check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
                else
                    over = true;
            }
            else
            {
                if (check.GetComponent<itemBcheck>().inWall == false)
                {
                    over = false;
                    end = true;
                }
                else
                {
                    check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y - 0.5f);
                }
            }
        }
        else
        {
            //Debug.Log("Normal");
            switch (step)
            {
                case 0:
                    if (over == false)
                    {
                        if (check.transform.position.x < limit.x)
                            check.transform.position = new Vector2(check.transform.position.x + 1f, check.transform.position.y);
                        else
                            over = true;
                    }
                    else
                    {
                        if (check.GetComponent<itemBcheck>().inWall == false)
                        {
                            over = false;
                            step++;
                        }
                        else
                        {
                            check.transform.position = new Vector2(check.transform.position.x - 0.5f, check.transform.position.y);
                        }
                    }
                    break;

                case 1:
                    if (over == false)
                    {
                        if (check.transform.position.y < limit.y)
                            check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
                        else
                            over = true;
                    }
                    else
                    {
                        if (check.GetComponent<itemBcheck>().inWall == false)
                        {
                            over = false;
                            step++;
                        }
                        else
                        {
                            check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y - 0.5f);
                        }
                    }
                    break;

                case 2:
                    if (check.GetComponent<itemBcheck>().inWall == false)
                    {
                        if (over == false)
                            check.transform.position = new Vector2(check.transform.position.x + 1f, check.transform.position.y);
                        if (over == true || check.transform.position.x > limit.x)
                        {
                            end = true;
                            step++;
                        }
                    }
                    else
                    {
                        check.transform.position = new Vector2(check.transform.position.x - 0.5f, check.transform.position.y);
                        over = true;
                    }
                    break;
            }
        }
    }

    void Up()
    {
        if (end == false)
        {
            if (over == false)
            {
                if (check.transform.position.y < limit.y)
                    check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
                else
                    over = true;
            }
            else
            {
                if (check.GetComponent<itemBcheck>().inWall == false)
                {
                    over = false;
                    end = true;
                }
                else
                {
                    check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y - 0.5f);
                }
            }
        }
    }

    void Left()
    {
        if (step == 0 && check.GetComponent<itemBcheck>().inWall == true)
        {
            check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
            return;
        }

        if (GameObject.Find("TargetCheck Left").GetComponent<TargetCheck>().lockOn == true)
        {
            //Debug.Log("targetAtack");
            if (transform.position.x != GameObject.Find("TargetCheck Left").GetComponent<TargetCheck>().target.x)
            {
                check.transform.position = new Vector2(GameObject.Find("TargetCheck Left").GetComponent<TargetCheck>().target.x, check.transform.position.y);
            }
            limit = new Vector2(GameObject.Find("Warp Default Up").transform.position.x, GameObject.Find("Warp Default Up").transform.position.y);

            if (over == false)
            {
                if (check.transform.position.y < limit.y)
                    check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
                else
                    over = true;
            }
            else
            {
                if (check.GetComponent<itemBcheck>().inWall == false)
                {
                    over = false;
                    end = true;
                }
                else
                {
                    check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y - 0.5f);
                }
            }
        }
        else
            switch (step)
            {
                case 0:
                    if (over == false)
                    {
                        if (check.transform.position.x > limit.x)
                            check.transform.position = new Vector2(check.transform.position.x - 1f, check.transform.position.y);
                        else
                            over = true;
                    }
                    else
                    {
                        if (check.GetComponent<itemBcheck>().inWall == false)
                        {
                            over = false;
                            step++;
                        }
                        else
                        {
                            check.transform.position = new Vector2(check.transform.position.x + 0.5f, check.transform.position.y);
                        }
                    }
                    break;

                case 1:
                    if (over == false)
                    {
                        if (check.transform.position.y < limit.y)
                            check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y + 1f);
                        else
                            over = true;
                    }
                    else
                    {
                        if (check.GetComponent<itemBcheck>().inWall == false)
                        {
                            over = false;
                            step++;
                        }
                        else
                        {
                            check.transform.position = new Vector2(check.transform.position.x, check.transform.position.y - 0.5f);
                        }
                    }
                    break;

                case 2:
                    if (check.GetComponent<itemBcheck>().inWall == false)
                    {
                        if (over == false)
                            check.transform.position = new Vector2(check.transform.position.x - 1f, check.transform.position.y);
                        if (over == true || check.transform.position.x < limit.x)
                        {
                            end = true;
                            step++;
                        }
                    }
                    else
                    {
                        check.transform.position = new Vector2(check.transform.position.x + 0.5f, check.transform.position.y);
                        over = true;
                    }
                    break;
            }
    }

    void Direction()
    {
        if ((Input.GetKey(KeyCode.RightArrow) && throwing == false) && down == false)
        {
            down = true;
            limit = new Vector2(GameObject.Find("Warp Default Right").transform.position.x, GameObject.Find("Warp Default Up").transform.position.y);
            end = false;
            upThrow = false;
            leftThrow = false;
            downThrow = false;
            rightThrow = true;
            over = false;
            step = 0;
            transform.GetChild(0).position = (Vector2)transform.position + checkPos;
        }
        if ((Input.GetKey(KeyCode.LeftArrow) && throwing == false) && down == false)
        {
            down = true;
            limit = new Vector2(GameObject.Find("Warp Default Left").transform.position.x, GameObject.Find("Warp Default Up").transform.position.y);
            end = false;
            upThrow = false;
            downThrow = false;
            rightThrow = false;
            leftThrow = true;
            over = false;
            step = 0;
            transform.GetChild(0).position = (Vector2)transform.position + checkPos;
        }
        if ((Input.GetKey(KeyCode.UpArrow) && throwing == false && down == false))
        {
            down = true;
            limit = new Vector2(GameObject.Find("Warp Default Up").transform.position.x, GameObject.Find("Warp Default Up").transform.position.y);
            end = false;
            rightThrow = false;
            leftThrow = false;
            downThrow = false;
            upThrow = true;
            over = false;
            step = 0;
            transform.GetChild(0).position = (Vector2)transform.position + checkPos;
        }
        if ((Input.GetKeyDown(KeyCode.DownArrow) && throwing == false && down == false))
        {
            down = true;
            end = false;
            rightThrow = false;
            leftThrow = false;
            upThrow = false;
            downThrow = true;
            over = false;
            step = 0;
            transform.GetChild(0).position = (Vector2)transform.position + checkPos;
        }

        if(Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.UpArrow))
        {
            down = false;
        }
    }
}
