using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class ChargeGage : MonoBehaviour
{
    Image BlueGage;
    Image WhiteGage;
    int now = 0;
    Lulu lulu;
    public GameObject BackGage;
    Color blueColor;
    public bool Flashing = false;
    public float flashSpeed = 1;
    // Start is called before the first frame update
    void Start()
    {
        WhiteGage = GameObject.Find("White Gage").GetComponent<Image>();
        BlueGage = GetComponent<Image>();
        lulu = GameObject.Find("Player").GetComponent<Lulu>();
        blueColor = BlueGage.GetComponent<Image>().color;
    }

    // Update is called once per frame
    void Update()
    {
        //ゲージ変動アニメーション
        if(lulu.GetPluspower() != now)
        {
            now = lulu.GetPluspower();
            WhiteGage.fillAmount = (float)now / 300;
            DOTween.To
            (
                () => BlueGage.fillAmount,       //何に
                x => BlueGage.fillAmount = x,  //何を
                (float)now / 300,     //どこまで(最終的な値)
                0.8f       //どれくらいの時間
            );
            iTween.ShakePosition(BackGage, iTween.Hash("x", 1f, "y", 2f));
            StartCoroutine("wait");
        }

        //チャージ攻撃可能アニメーション
        if (lulu.GetPluspower() >= lulu.GetReadypower())
        {
            FlashAnimation();
        }
    }

    IEnumerator wait()
    {
        yield return new WaitForSeconds(0.8f);
        GetComponent<ChargeGage>().ColorChange("blue");
    }

    public void ColorChange(string color)
    {
        switch (color)
        {
            case "red":
                BlueGage.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f);
                break;

            case "blue":
                BlueGage.GetComponent<Image>().color = blueColor;
                break;

            case "yellow":
                BlueGage.GetComponent<Image>().color = Color.yellow;
                break;

            case "white":
                Flashing = true;
                break;
        }
    }

    void FlashAnimation()
    {
        BlueGage.GetComponent<Image>().color = new Color(BlueGage.GetComponent<Image>().color.r, BlueGage.GetComponent<Image>().color.g, BlueGage.GetComponent<Image>().color.b,
            ((float)System.Math.Cos(Time.time * flashSpeed) + 1.0f) / 2);
    }
}
