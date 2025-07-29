using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewInput : MonoBehaviour
{
    [SerializeField]
    Sprite InputOn;
    Sprite InputOff;
    SpriteRenderer rend;
    bool flug = false;
    int direction = 0;

    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        InputOff = rend.sprite;
        //時計回りで方向を設定
        switch (rend.sprite.name)
        {
            case "Up":
                direction = 1;
                break;
            case "Right":
                direction = 2;
                break;
            case "Down":
                direction = 3;
                break;
            case "Left":
                direction = 4;
                break;
            case "Z":
                direction = 5;
                break;
        }
    }

    //黒と黄色を入れ替え、フラグを切り替える
    void ColorON()
    {
        if (rend.sprite != InputOn)
            rend.sprite = InputOn;
    }
    void ColorOFF()
    {
        if (rend.sprite != InputOff)
            rend.sprite = InputOff;
    }

    //透明度変更
    public void setAlpha(float set)
    {
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, set);
    }

    void ChangeFlug()
    {
        flug = (flug == true) ? false : true;
    }

    // Update is called once per frame
    void Update()
    {
        switch (direction)
        {
            case 1:
                if (Input.GetKeyDown(KeyCode.UpArrow)
                    || Input.GetKeyUp(KeyCode.UpArrow))
                {
                    ChangeFlug();
                }
                break;
            case 2:
                if (Input.GetKeyDown(KeyCode.RightArrow)
                    || Input.GetKeyUp(KeyCode.RightArrow))
                {
                    ChangeFlug();
                }
                break;
            case 3:
                if (Input.GetKeyDown(KeyCode.DownArrow)
                    || Input.GetKeyUp(KeyCode.DownArrow))
                {
                    ChangeFlug();
                }
                break;
            case 4:
                if (Input.GetKeyDown(KeyCode.LeftArrow)
                    || Input.GetKeyUp(KeyCode.LeftArrow))
                {
                    ChangeFlug();
                }
                break;
            case 5:
                if (Input.GetKeyDown(KeyCode.Z)
                    || Input.GetKeyUp(KeyCode.Z))
                {
                    ChangeFlug();
                }
                break;
        }

        //半透明（使用不可状態）のとき、色を戻す
        if (GetComponent<SpriteRenderer>().color.a < 1f && flug)
        {
            ColorOFF();
            return;
        }

        if (flug)
        {
            ColorON();
        }
        else
        {
            ColorOFF();
        }
    }
}
