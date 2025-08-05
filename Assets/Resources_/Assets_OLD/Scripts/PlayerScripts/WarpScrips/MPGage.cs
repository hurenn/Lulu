using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class MPGage : MonoBehaviour
{
    private Image realGage;
    private Image greenGage;
    private Image redGage;
    private int DefaultMP = 200;
   // private Image underGage;

    public float underTime = 0.5f;

    // Use this for initialization
    void Start()
    {
            realGage = GameObject.Find("RealGage").GetComponent<Image>();
            greenGage = GameObject.Find("GreenGage").GetComponent<Image>();
            redGage = GameObject.Find("RedGage").GetComponent<Image>();
            //underGage = GameObject.Find("UnderGage").GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (WarpControl_Old.overHeat == true)
        {
            realGage.color = new Color(1f, 0.4f, 0.4f, 0.9f);
            greenGage.color = new Color(1f, 0.4f, 0.4f, 0.9f);
            redGage.color = new Color(1f, 0.4f, 0.4f, 0.9f);
        }
        else
        {
            realGage.color = new Color(1f, 1f, 1f, 1f);
            greenGage.color = new Color(1f, 1f, 1f, 1f);
            redGage.color = new Color(1f, 1f, 1f, 1f);
        }

            realGage.fillAmount = WarpControl_Old.nowMagic / DefaultMP;
            greenGage.fillAmount = (WarpControl_Old.nowMagic - DefaultMP) / DefaultMP;
            redGage.fillAmount = (WarpControl_Old.nowMagic - DefaultMP * 2) / DefaultMP;
            //underGage.fillAmount = WarpControl.nowMagic / WarpControl.maxMagic;
    }

    public void GetGage()
    {
        realGage.fillAmount = WarpControl_Old.nowMagic / DefaultMP;
        greenGage.fillAmount = (WarpControl_Old.nowMagic - DefaultMP) / DefaultMP;
        redGage.fillAmount = (WarpControl_Old.nowMagic - DefaultMP * 2) / DefaultMP;
    }
}
