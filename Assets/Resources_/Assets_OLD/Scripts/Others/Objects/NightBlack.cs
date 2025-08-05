using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NightBlack : MonoBehaviour
{
    //float timer = 0;
    public float rimit = 5f;
    public int powerDown = 20;
    bool startFlag;
    // Start is called before the first frame update
    void Start()
    {
        startFlag = true;
        //(float)(255 - GameObject.Find("Player").GetComponent<Lulu>().GetPluspower()) / 255.0f - 0.025f);
        Friends.Marlica = true;
        Friends.Pepe = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!startFlag)
            return;

        GetComponent<SpriteRenderer>().color = new Vector4(0, 0, 0, (WarpControl_Old.defaultMagic / 100f) - (WarpControl_Old.nowMagic / 100f));//(float)(255 - GameObject.Find("Player").GetComponent<Lulu>().GetPluspower()) / 255.0f - 0.025f);
        /*
        timer += Time.deltaTime;
        if(timer > rimit)
        {
            timer = 0;
            GameObject.Find("Player").GetComponent<Lulu>().powerUp(-powerDown);
            GameObject.Find("Blue Gage").GetComponent<ChargeGage>().ColorChange("blue");
        }
        */
    }

    public void setStart()
    {
        startFlag = true;
    }
    public void setBlack()
    {
        GetComponent<SpriteRenderer>().color = new Vector4(0, 0, 0, 0.95f);
        startFlag = false;
    }

    public void power100()
    {
        WarpControl_Old.nowMagic = WarpControl_Old.maxMagic;
    }
}
