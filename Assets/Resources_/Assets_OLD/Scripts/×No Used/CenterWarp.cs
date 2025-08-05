using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CenterWarp : MonoBehaviour
{
    float limittime = 0;
    public static bool flag = false;
    bool inGround = false;
    bool inTarget = false;
    GameObject player;
    int step = 0;
    bool over = false;
    public static bool cool = false;
    Vector2 keep;
    bool exitKey = true;
    bool center;
    float dashtimer = 0;
    int dash = 0;

    public GameObject counterArea;
    public static bool counter;
    public bool counterDebug;

    void Start()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (flag == true || (Friends.Pepe && !PlayerInput.right && !PlayerInput.left && !PlayerInput.up && !PlayerInput.down && OnGround.jump))
            counter = true;
        if (Friends.Pepe)
        {
            if (flag == false && (PlayerInput.right || PlayerInput.left || PlayerInput.up || PlayerInput.down || !OnGround.jump) && counter == true)
                counter = false;
        }
        else
        {
            if (flag == false && counter == true)
                counter = false;
        }

        if (counter)
        {
            counterArea.SetActive(true);
        }
        else
        {
            counterArea.SetActive(false);
        }

        if (counterDebug)
        {
            Debug.Log("flag = " + flag + ", counter = " + counter);
        }

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

        if (center == true)
            Center();

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
                if (Input.GetKeyDown(KeyCode.Z) && !PlayerInput.up && !PlayerInput.right && !PlayerInput.left && !PlayerInput.down)
                {
                    exitKey = false;
                    WarpStart();
                    center = true;
                }
        }
        else
        {
            if (exitKey == true)
                exitKey = false;
        }

        //ダッシュ
        if (PlayerInput.x && OnGround.jump == true)
        {

            if (dashtimer < PlayerInput.dashtime)
            {
                dashtimer += Time.deltaTime;
                if (dash >= 2 && Input.GetKeyDown(KeyCode.DownArrow) && cool == false)
                {
                    WarpStart();
                    player.GetComponent<WarpControl_Old>().cool = true;
                    center = true;

                }
            }
            else
            {
                dash = 0;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) && dash == 0)
            {
                dashtimer = 0;
                dash = 1;
            }
            if (dash >= 1 && Input.GetKeyUp(KeyCode.DownArrow))
            {
                dash = 2;
                dashtimer = 0;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                dash = 0;
            }
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
    void Center()
    {
        if (step != 100)
        {
            step = 100;
            flag = true;
        }
    }

    void End()
    {
        center = false;
        over = false;
        step = 0;
    }
    void WarpStart()
    {
        limittime = 0;
        RuruAnime.WarpStart = true;
        cool = true;
        Player.stop = true;
        player.GetComponent<WarpControl_Old>().cool = true;
    }
}
