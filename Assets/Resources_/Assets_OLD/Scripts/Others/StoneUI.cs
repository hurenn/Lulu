using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoneUI : MonoBehaviour
{
    public GameObject flash;
    SpriteRenderer rend;
    public GameObject magicStone;
    public static bool use = false;

    bool set = false;
    float timer = 0;
    public int cost = 100;
    public float InvinceTime = 5;
    int step = 0;

    public static bool useStone = false;

    // Use this for initialization
    void Start()
    {
        rend = GameObject.Find("Player").GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (100 <= CollectCoin.Collected && use == false && set == false)  //取得
        {
            magicStone.SetActive(true);
            set = true;
        }

        if (OnGround.jump == true && Input.GetKey(KeyCode.DownArrow))
        {
            if (Input.GetKey(KeyCode.X))
            {
                step = 1;
            }
            if (step == 1 && !Input.GetKey(KeyCode.X))
            {
                step = 2;
                useStone = true;
            }
        }
        if (step != 0)
        {
            if (!OnGround.jump || !Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.UpArrow))
                step = 0;
        }

        if (set == true)
        {
            if (useStone == true)   //使用
            {
                use = true;
                useStone = false;
                GameObject.Instantiate(flash);
            }
        }
        if (use == true)
        {
            UseStone();
        }
    }

    void UseStone()
    {
        if (timer == 0)
        {
            PlusScore.end = true;
            cost = CollectCoin.Collected / 10;
            if (cost < 100)
                cost = 100;

            CollectCoin.Collected -= cost;
            rend.color = new Color(1f, 1f, 0.7f, 1f);
            magicStone.SetActive(false);
        }

        Life.nowLife = Life.maxLife;
        WarpControl.nowMagic = WarpControl.maxMagic;

        timer += Time.deltaTime;
        if (timer > InvinceTime)
        {
            rend.color = new Color(1f, 1f, 1f, 1f);
            use = false;
            set = false;
            timer = 0;
        }
    }
}
