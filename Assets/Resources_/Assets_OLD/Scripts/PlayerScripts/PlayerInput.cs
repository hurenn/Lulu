using UnityEngine;
using System.Collections;

public class PlayerInput : MonoBehaviour
{
    //入力状態・ダッシュ状態などを管理するスクリプト

    public static float dashtime = 0.15f;
    float timer = 1; //ダッシュ継続判定（dashtime以下でダッシュ発生）
    int LR = 0;
    bool rightdash = false;
    bool leftdash = false;
    private bool dash = false;
    public static bool x = false;
    public static bool cool = false;
    public static bool up = false;
    public static bool right = false;
    public static bool down = false;
    public static bool left = false;
    public static bool z = false;

    public static bool keep;
    Rigidbody2D rb;

    public int getDash()
    {
        int ans = 0;
        if (rightdash || leftdash)
            ans = 1;
        if (dash)
            ans = 2;
        return ans;
    }

    //Player player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        //player = GetComponent<Player>();
    }

    public bool GetDash()
    {
        return dash;
    }
    public void SetDash(bool set)
    {
        timer = 0;
        dash = set;
    }

    public void setTimer()
    {
        timer = dashtime / 2;
    }

    void Update()
    {
        if (Life.over)
        {
            return;
        }

        if (GetComponent<Lulu>().GetStop())
        {
            return;
        }
        KeyInput();

        //Debug.Log("dash = " + dash + ", timer = " + timer + ", " + leftdash + "| " + rightdash + ", LR = " + LR);

        if (GetComponent<Lulu>().IsGround())
        {
            if (timer < dashtime)
            {
                timer += Time.deltaTime;
            }
            else if (rightdash == true || leftdash == true)
            {
                rightdash = false;
                leftdash = false;
                dash = false;
            }

            if (!dash)//歩き
            {
                if (rightdash && Input.GetKeyDown(KeyCode.RightArrow))
                {
                    rightdash = false;
                    dash = true;
                    GetComponent<Lulu>().GenerateSmoke();
                }
                if (leftdash && Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    leftdash = false;
                    dash = true;
                    GetComponent<Lulu>().GenerateSmoke();
                }

                if (Input.GetKey(KeyCode.RightArrow))
                {
                    rightdash = true;
                    leftdash = false;
                    timer = 0;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    rightdash = false;
                    leftdash = true;
                    timer = 0;
                }
            }
            else//走り
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    if (rightdash)
                    {
                        rightdash = false;
                        dash = true;
                    }
                }
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow))
                {
                    rightdash = true;
                    timer = 0;
                }

                if (timer > dashtime && !Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
                    dash = false;
            }
            if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftArrow))//少し同時押しでダッシュ開始
            {
                LR = Mathf.Clamp(LR + 1, 0, 100);
                if (dash == false && LR >= 100)
                {
                    dash = true;
                }
            }
            else
            {
                LR = 0;
            }
        }

        if (cool == true)
        {
            //player.SetDirectionalInput(new Vector2(0, 0));
            //rb.velocity = new Vector2(0, 0);
            if (keep == false)
                x = false;
            z = false;
            right = false;
            left = false;
            up = false;
            down = false;
            return;
        }
        if (keep == true)
        {
            if (!Input.GetKey(KeyCode.X))
                x = false;
            keep = false;
        }


    }

    void KeyInput()
    {
        if (Input.GetKey(KeyCode.X))
            x = true;
        else
            x = false;

        if (Input.GetKey(KeyCode.Z))
            z = true;
        else
            z = false;

        if (Input.GetKey(KeyCode.RightArrow))
            right = true;
        else
            right = false;

        if (Input.GetKey(KeyCode.LeftArrow))
            left = true;
        else
            left = false;

        if (Input.GetKey(KeyCode.UpArrow))
            up = true;
        else
            up = false;

        if (Input.GetKey(KeyCode.DownArrow))
            down = true;
        else
            down = false;

    }

    public static void setZero()
    {
        x = false;
        cool = false;
        up = false;
        right = false;
        down = false;
        left = false;
        z = false;

        keep = false;
        dashtime = 0.15f;
    }
}