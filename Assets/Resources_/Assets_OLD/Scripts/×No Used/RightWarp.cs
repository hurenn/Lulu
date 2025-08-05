using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightWarp : MonoBehaviour
{
    float limittime = 0;
    public static bool flag = false;
    bool inGround = false;
    bool inTarget = false;
    Vector3 Goal;
    GameObject player;
    float dashtimer = 0;
    int dash = 0;
    int step = 0;
    bool over = false;
    bool right = false;
    bool upright = false;
    bool downright = false;
    public static bool cool = false;
    bool ride = false;
    Vector2 keep;
    bool exitKey = false;

    void Start()
    {
        player = GameObject.Find("Player");
    }

        // Update is called once per frame
        void FixedUpdate()
    {
        if (cool == true)//バグ処理
        {
            limittime += Time.deltaTime;
        }
        if (limittime > 3f && (cool == true || flag == true))
        {
            Player.stop = false;
            flag = false;
            cool = false;
            End();
        }

        if (inGround == false)
            GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 1f);
        else
            GetComponent<MeshRenderer>().material.color = new Color(1f, 0f, 0f);

        //Debug.Log("WarpCool = " + WarpControl.cool + " RightCool = " + cool + " right = " + right + " flag = " + flag);
        if (right == true)
            Right();
        if (upright == true)
            Upright();
        if (downright == true)
            Downright();

        if (cool == false && step > 0)
        {
            End();
        }

        if (OnGround.jump == false)
        {
            if (Input.GetKeyUp(KeyCode.Z))
            {
                exitKey = true;
            }
            if (exitKey == true && cool == false)
                if ((Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.RightArrow)) || (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.RightArrow)))
                {
                    exitKey = false;
                    WarpStart();
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        Goal = GameObject.Find("Warp Default UpRight").transform.position;
                        upright = true;
                    }
                    else if (Input.GetKey(KeyCode.DownArrow))
                    {
                        Goal = GameObject.Find("Warp Default DownRight").transform.position;
                        downright = true;
                    }
                    else
                    {
                        Goal = GameObject.Find("Warp Default Right").transform.position;
                        right = true;
                    }
                }
        }
        else
        {
            if (exitKey == true)
                exitKey = false;
        }

        //ダッシュ
        if (dashtimer < PlayerInput.dashtime)
        {
            dashtimer += Time.deltaTime;
            if (dash >= 2 && Input.GetKeyDown(KeyCode.RightArrow) && cool == false)
            {
                WarpStart();
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    Goal = GameObject.Find("Warp Default UpRight").transform.position;
                    upright = true;
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    Goal = GameObject.Find("Warp Default DownRight").transform.position;
                    downright = true;
                }
                else
                {
                    Goal = GameObject.Find("Warp Default Right").transform.position;
                    right = true;
                }
            }
        }
        else
        {
            dash = 0;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) && dash == 0)
        {
            dashtimer = 0;
            dash = 1;
        }
        if (dash >= 1 && Input.GetKeyUp(KeyCode.RightArrow))
        {
            dash = 2;
            dashtimer = 0;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            dash = 0;
        }
    }

    private void OnTriggerStay2D(Collider2D Collider)
    {
        if (Collider.tag.Contains("Ground") || Collider.tag.Contains("itemB") || Collider.tag.Contains("Enemy") || Collider.tag.Contains("Trap") || Collider.tag.Contains("Broken"))
            inGround = true;
        if (Collider.tag.Contains("WarpTarget"))
            inTarget = true;
    }
    private void OnTriggerExit2D(Collider2D Collider)
    {
        if (Collider.tag.Contains("Ground") || Collider.tag.Contains("itemB") || Collider.tag.Contains("Enemy") || Collider.tag.Contains("Trap") || Collider.tag.Contains("Broken"))
            inGround = false;
        if (Collider.tag.Contains("WarpTarget"))
            inTarget = false;
    }
    void Right()
    {
        if (transform.position.x < player.transform.position.x || transform.position.x > Goal.x + 3 || transform.position.y > Goal.y + 3 || transform.position.y < Goal.y - 3)//例外処理
        {
            transform.position = player.transform.position;
        }
        if (inGround == false && inTarget == true && step != 100)//ターゲットワープ
        {
            step = 100;
            flag = true;
            ride = false;
        }
        switch (step)
        {
            case 0:
                if (transform.position.x < Goal.x)
                {
                    transform.position = new Vector2(transform.position.x + 1f, transform.position.y);
                }
                else if (transform.position.x >= Goal.x)
                {
                    if (transform.position.x > Goal.x)
                    {
                        transform.position = new Vector2(Goal.x, transform.position.y);
                    }
                    if (inGround == true && GameObject.Find("Ride Check Right").GetComponent<RideCheck>().inGround == false)
                    {
                        ride = true;
                    }
                    step++;
                }
                break;

            case 1:
                if (inGround == true)
                {
                    if (ride == true)
                        transform.position = new Vector2(transform.position.x, transform.position.y + 1f);
                    else
                        transform.position = new Vector2(transform.position.x - 1f, transform.position.y);
                }
                else
                {
                    step++;
                }
                break;
            case 2:
                if (inGround == true)
                {
                    if (ride == true)
                        transform.position = new Vector2(transform.position.x, transform.position.y + 1f);
                    else
                        transform.position = new Vector2(transform.position.x - 1f, transform.position.y);
                }
                else
                {
                    step = 100;
                    flag = true;
                    ride = false;
                }
                break;
        }
    }
    void Upright()//右下・右上に容易に壁抜けできるバグ Stayにしたら直ったぜ！！
    {
        if (transform.position.x < player.transform.position.x || transform.position.x > Goal.x + 3 || transform.position.y > Goal.y + 3 || transform.position.y < Goal.y - 20)//例外処理
        {
            transform.position = player.transform.position;
        }
        if (inGround == false && inTarget == true && step != 100)//ターゲットワープ
        {
            step = 100;
            flag = true;
        }
        switch (step)
        {
            case 0:
                if (over == false)
                {
                    if (transform.position.x < Goal.x)
                        transform.position = new Vector2(transform.position.x + 1f, transform.position.y);
                    else
                        over = true;
                }
                else
                {
                    if (inGround == false)
                    {
                        if (transform.position.x > Goal.x)
                        {
                            transform.position = new Vector2(Goal.x, transform.position.y);
                        }
                        over = false;
                        step++;
                    }
                    else
                    {
                        transform.position = new Vector2(transform.position.x - 0.5f, transform.position.y);
                    }
                }
                break;

            case 1:
                if (transform.position.y < player.transform.position.y)
                {
                    transform.position = player.transform.position;
                }
                if (over == false)
                {
                    if (transform.position.y < Goal.y)
                        transform.position = new Vector2(transform.position.x, transform.position.y + 1f);
                    else
                        over = true;
                }
                else
                {
                    if (inGround == false)
                    {
                        if (transform.position.y > Goal.y)
                        {
                            transform.position = new Vector2(transform.position.x, Goal.y);
                        }
                        over = false;
                        step++;
                    }
                    else
                    {
                        transform.position = new Vector2(transform.position.x, transform.position.y - 0.5f);
                    }
                }
                break;

            case 2:
                if (inGround == false)
                {
                    if (transform.position.x > Goal.x)
                    {
                        transform.position = new Vector2(Goal.x, transform.position.y);
                    }
                    if (over == true || transform.position.x >= Goal.x)
                    {
                        step++;
                    }
                    else if (over == false)
                        transform.position = new Vector2(transform.position.x + 0.5f, transform.position.y);
                }
                else
                {
                    transform.position = new Vector2(transform.position.x - 0.5f, transform.position.y);
                    over = true;
                }
                break;

            case 3:
                if (inGround == false)
                {
                    if (transform.position.x > Goal.x)
                    {
                        transform.position = new Vector2(Goal.x, transform.position.y);
                    }
                    if (over == true || transform.position.x >= Goal.x)
                    {
                        flag = true;
                        step++;
                    }
                    else if (over == false)
                        transform.position = new Vector2(transform.position.x + 0.5f, transform.position.y);
                }
                else
                {
                    transform.position = new Vector2(transform.position.x - 0.5f, transform.position.y);
                    over = true;
                }
                break;
        }
    }
    void Downright()
    {
        if (transform.position.x < player.transform.position.x || transform.position.x > Goal.x + 3 || transform.position.y > Goal.y + 20 || transform.position.y < Goal.y - 3)//例外処理
        {
            transform.position = player.transform.position;
        }
        if (inGround == false && inTarget == true && step != 100)//ターゲットワープ
        {
            step = 100;
            flag = true;
        }
        switch (step)
        {
            case 0:
                if (over == false)
                {
                    if (transform.position.x < Goal.x)
                        transform.position = new Vector2(transform.position.x + 1f, transform.position.y);
                    else
                        over = true;
                }
                else
                {
                    if (inGround == false)
                    {
                        if (transform.position.x > Goal.x)
                        {
                            transform.position = new Vector2(Goal.x, transform.position.y);
                        }
                        over = false;
                        step++;
                    }
                    else
                    {
                        transform.position = new Vector2(transform.position.x - 0.5f, transform.position.y);
                    }
                }
                break;

            case 1:
                if (transform.position.y > player.transform.position.y)
                {
                    transform.position = player.transform.position;
                }
                if (over == false)
                {
                    if (transform.position.y > Goal.y)
                        transform.position = new Vector2(transform.position.x, transform.position.y - 1f);
                    else
                        over = true;
                }
                else
                {
                    if (inGround == false)
                    {
                        if (transform.position.y < Goal.y)
                        {
                            transform.position = new Vector2(transform.position.x, Goal.y);
                        }
                        over = false;
                        step++;
                    }
                    else
                    {
                        transform.position = new Vector2(transform.position.x, transform.position.y + 0.5f);
                    }
                }
                break;

            case 2:
                if (inGround == false)
                {
                    if (transform.position.x > Goal.x)
                    {
                        transform.position = new Vector2(Goal.x, transform.position.y);
                    }
                    if (over == true || transform.position.x >= Goal.x)
                    {
                        step++;
                    }
                    else if (over == false)
                        transform.position = new Vector2(transform.position.x + 0.5f, transform.position.y);
                }
                else
                {
                    transform.position = new Vector2(transform.position.x - 0.5f, transform.position.y);
                    over = true;
                }
                break;

            case 3:
                if (inGround == false)
                {
                    if (transform.position.x > Goal.x)
                    {
                        transform.position = new Vector2(Goal.x, transform.position.y);
                    }
                    if (over == true || transform.position.x >= Goal.x)
                    {
                        flag = true;
                        step++;
                    }
                    else if (over == false)
                        transform.position = new Vector2(transform.position.x + 0.5f, transform.position.y);
                }
                else
                {
                    transform.position = new Vector2(transform.position.x - 0.5f, transform.position.y);
                    over = true;
                }
                break;
        }
    }
    void End()
    {
        right = false;
        upright = false;
        downright = false;
        over = false;
        step = 0;
    }
    void WarpStart()
    {
        limittime = 0;
        RuruAnime.WarpStart = true;
        transform.position = new Vector2(player.transform.position.x, GameObject.Find("Warp Default Right").transform.position.y);
        cool = true;
        Player.stop = true;
        player.GetComponent<WarpControl_Old>().cool = true;
    }
}
