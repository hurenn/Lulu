using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuruAnime : MonoBehaviour
{
    public static bool stop = false;

    Animator anim;
    SpriteRenderer rend;
    public static bool left = true;
    float speed = 0f;
    float box = 0f;
    public static bool freez = false;
    float walkSpeed = 0;
    float walkStop = 0;
    bool hanging = false;
    public static bool WarpStart = false;
    bool directionLock = false;//投げアニメの固定
    bool invincible = false;
    public GameObject WarpAnimation;
    public static bool grabB;
    public static bool throwB;

    bool jumping = false;
    public static int state = 0; //0:Stand 1:Walk 2:Fall 3:Jump 4:Warp 5:jump_fall 6:fall_stand 7:fall_walk 8:walk_stand 9:Crouch 10:stand_crouch 11:crouch_stand 12:Grab 13:BigThrow 14:warp_fall 15:surprise

    // Use this for initialization
    void Start()
    {
        anim = GetComponent<Animator>();
        rend = GetComponent<SpriteRenderer>();
        box = this.transform.position.y;
    }

    public void ResetWalk()
    {
        WarpStart = false;
        state = 1;
        anim.Play("Walk right");
    }
    // Update is called once per frame
    void FixedUpdate()
    {

        if (stop == true)
        {
            if(jumping == false)
            {
                anim.Play("Stand right");
            }
            return;
        }

        if (state == 1)//ブレーキアニメかかるまでの時間
        {
            walkStop += Time.deltaTime;
        }
        if (Life.over == true)//ダメージ演出
        {
            anim.Play("Damage right");
            return;
        }
        /*
        if (GameObject.Find("WarpPad").GetComponent<WarpPad>().next == true)//次エリアへ移動演出
        {
            anim.Play("WarpStart");
            state = 4;
            return;
        }
        */
        if (state == 13)//アニメーション遷移なし
        {
            return;
        }
        if (Hang.imp == true && freez == false)//投げ
        {
            if (PlayerInput.right || PlayerInput.left || PlayerInput.up || PlayerInput.down)
            {
                freez = true;
                anim.Play("Throw Ground_Small right");
                StartCoroutine("wait");
            }
        }

        if (OnGround.jump == true)
        {
            if (grabB == true)
            {
                freez = true;
                GrabB();
                return;
            }
            if (throwB == true)
            {
                ThrowB();
                return;
            }
            else if (directionLock == true)
            {
                directionLock = false;
                freez = false;
            }
        }

        direction();

        speed = this.transform.position.y - box;
        box = this.transform.position.y;

            if (PlayerInput.right)
                left = false;
            else if (PlayerInput.left)
                left = true;

        //if (WarpStart == true || (Input.GetKeyDown(KeyCode.Z) && OnGround.jump == false) || dash == true)//ワープアニメ
        if(WarpStart == true)
        {
            anim.Play("Fall right");
            anim.Play("Warp Effect");
            state = 14;
            WarpStart = false;
        }
        if (state == 14)
        {
            if(WarpControl_Old.overHeat == true)
            {
                state = 6;
            }
            else if (GetComponent<WarpControl_Old>().cool == true)
            {
                anim.Play("Warp_Fall");
                StartCoroutine("trans");
            }
            return;
        }

        if (Player.invinceTime < Player.maxInvince && !Player.avoidAnim)//回避アニメ
        {
            Player.avoidAnim = true;
            Instantiate(WarpAnimation, this.gameObject.transform.position, Quaternion.identity);
        }


        //Debug.Log("true = " + OnGround.jump + ", false = " + freez + ", false = " + jumping + ", state = " + state);
        if (OnGround.jump == true && freez == false)
        {

            if (grabB == true)
            {
                state = 12;
                if (PlayerInput.left)
                {
                    if (left == false)
                        anim.Play("Grab Left right");
                    else
                        anim.Play("Grab Right right");
                }
                else if (PlayerInput.up)
                    anim.Play("Grab Up right");
                else if (PlayerInput.down)
                    anim.Play("Grab Down right");
                else
                {
                    if (left == false)
                        anim.Play("Grab Right right");
                    else
                        anim.Play("Grab Left right");
                }
                return;
            }
            if (state == 12)
                StartCoroutine("trans");

            if (Hang.hanging == true && hanging == false)//pick up
            {
                anim.Play("Crouch_Stand right");
                state = 11;
                hanging = true;
                StartCoroutine("trans");
            }
            if (Hang.hanging == false)
            {
                hanging = false;
            }

            if (PlayerInput.down && !PlayerInput.right && !PlayerInput.left && !Input.GetKey(KeyCode.Z) && !Input.GetKey(KeyCode.X))//しゃがみ
            {
                if (state != 9 && state != 10)
                {
                    anim.Play("Stand_Crouch right");
                    state = 10;
                    StartCoroutine("trans");
                }
                else if (state != 10)
                {
                    anim.Play("Crouch right");
                    state = 9;
                }
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                jumping = true;
                freez = false;
                anim.Play("Jump right");
                state = 3;
                SE.playnum = 6;
            }
            else if (PlayerInput.left || PlayerInput.right)
            {
                if (jumping == false)
                    //if (PlayerInput.dash == false)
                    {
                        if (state == 2 || state == 5)
                        {
                            anim.Play("Fall_Walk right");
                            state = 7;
                            StartCoroutine("trans");
                        }
                        else if (state != 7)
                        {
                        //Debug.Log("dash = " + PlayerInput.dash);//ダッシュ時、なぜかここまで来ない
                        if (GetComponent<PlayerInput>().GetDash())
                        {
                            //anim.Play("Dash right");
                            anim.Play("Walk right");
                        }
                        else
                        {
                            anim.Play("Walk right");
                        }
                            state = 1;
                        }
                        if (walkSpeed < 0.3f)
                        {
                            walkSpeed += Time.deltaTime;
                        }
                        else
                        {
                            SE.playnum = 14;
                            walkSpeed = 0;
                        }
                    }
                    else
                    {
                        //anim.Play("Dash right");
                    }
                if (walkSpeed < 0.15f)
                {
                    walkSpeed += Time.deltaTime;
                }
                else
                {
                    SE.playnum = 14;
                    walkSpeed = 0;
                }
            }
            else
            {
                if (jumping == false)
                {
                    if (state == 2 || state == 5)
                    {
                        anim.Play("Fall_Stand right");
                        state = 6;
                        StartCoroutine("trans");
                    }
                    else if (state != 6)
                    {
                        if ((state == 1 && walkStop > 0.5f) || state == 7)
                        {
                            anim.Play("Walk_Stand right");
                            state = 8;
                            StartCoroutine("trans");
                        }
                        else if (state != 8 && state != 6 && !PlayerInput.down)
                        {
                            if (state == 9)
                            {
                                anim.Play("Crouch_Stand right");
                                state = 11;
                                StartCoroutine("trans");
                            }
                            else if (state != 11)
                            {
                                anim.Play("Stand right");
                                state = 0;
                            }
                        }
                    }
                }
                walkStop = 0;
            }

        }
        else if (OnGround.jump == false && freez == false)
        {
            if (speed < 0)
            {
                jumping = false;
                if (state == 3)
                {
                    anim.Play("Jump_Fall right");
                    state = 5;
                    StartCoroutine("trans");
                }
                else if (state != 5)
                {
                    anim.Play("Fall right");
                    state = 2;
                }
            }

        }

    }
    void direction()//左向きor右向き
    {
        if (freez == false)
        {
            if (left == false)
                rend.flipX = false;
            else
                rend.flipX = true;
        }
    }

    IEnumerator wait()
    {
        SE.playnum = 11;
        yield return new WaitForSeconds(0.3f);
        freez = false;
    }
    IEnumerator trans()
    {
        if (state == 5)//Jump_Fall
        {
            yield return new WaitForSeconds(0.3f);
            state = 2;
        }
        if (state == 14)//Warp_Fall
        {
            yield return new WaitForSeconds(0.4f);
            state = 2;
        }
        if (state == 6)//Fall_Stand
        {
            yield return new WaitForSeconds(0.3f);
            if (PlayerInput.right || PlayerInput.left)
            {
                state = 0;
                yield break;
            }
            state = 0;
        }
        if (state == 7)//Fall_Walk
        {
            yield return new WaitForSeconds(0.3f);
            state = 1;
        }
        if (state == 8)//Walk_Stand
        {
            yield return new WaitForSeconds(0.3f);
            state = 0;
        }
        if (state == 10)//Stand_Crouch
        {
            //yield return new WaitForSeconds(0.15f);
            if (!PlayerInput.down)
            {
                state = 9;
                yield break;
            }
            state = 9;
        }
        if (state == 11)//Crouch_Stand
        {
            freez = true;
            yield return new WaitForSeconds(0.2f);
            state = 0;
            freez = false;
        }/*
        if (state == 15)
        {
            anim.Play("Warp Effect");
            yield return new WaitForSeconds(0.2f);
            anim.Play("Surprise right");
            yield return new WaitForSeconds(0.2f);
            es = false;
            cape = true;
            state = 2;
        }*/
    }


    void ThrowB()
    {
        state = 13;

        if (PlayerInput.right && directionLock == false)
        {
            if (left == false)
                anim.Play("Throw Right_Big_Side right");
            else
                anim.Play("Throw Left_Big_Side right");
            directionLock = true;
        }
        else if (PlayerInput.left && directionLock == false)
        {
            if (left == false)
                anim.Play("Throw Left_Big_Side right");
            else
                anim.Play("Throw Right_Big_Side right");
            directionLock = true;
        }
        else if (PlayerInput.up && directionLock == false)
        {
            anim.Play("Throw Up_Big_Side right");
            directionLock = true;
        }
        else if (PlayerInput.down && directionLock == false)
        {
            anim.Play("Throw Down_Big_Side right");
            directionLock = true;
        }
        else
        {
            state = 0;
        }
        state = 0;
    }

    void GrabB()
    {
        state = 12;
        if (PlayerInput.left)
        {
            if (left == false)
                anim.Play("Grab Left right");
            else
                anim.Play("Grab Right right");
        }
        else if (PlayerInput.up)
            anim.Play("Grab Up right");
        else if (PlayerInput.down)
            anim.Play("Grab Down right");
        else
        {
            if (left == false)
                anim.Play("Grab Right right");
            else
                anim.Play("Grab Left right");
        }
    }
}