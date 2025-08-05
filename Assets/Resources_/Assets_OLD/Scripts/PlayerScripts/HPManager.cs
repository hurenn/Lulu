using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class HPManager : MonoBehaviour
{
    float invinceTime = -1;     //無敵時間タイマー
    public float maxInvince = 0.7f; //無敵時間
    int invinceFlag = 0;    //無敵フラグ
    public bool debug; //デバッグフラグ
    /*
    bool isInvince = false;
    bool isInvince2 = false;
    */
    public GameObject MPGage;
    GameObject CollapseHeart;
    GameObject CollapseCharge;
    // Start is called before the first frame update
    void Start()
    {
        invinceTime = maxInvince;
        CollapseHeart = (GameObject)Resources.Load("Collapse Heart");
        CollapseCharge = (GameObject)Resources.Load("Collapse Charge");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(invinceFlag);
        //Debug.Log(invinceFlag);
        if (debug)
        {
            GameObject.Find("InvinceTime").GetComponent<Text>().text = "invinceTime = " + invinceTime;
            GameObject.Find("InvinceFlag").GetComponent<Text>().text = "invinceFlag = " + invinceFlag;
            /*
            GameObject.Find("stop").GetComponent<Text>().text = "Stop = " + Lulu.GetStop() ;
            GameObject.Find("stopCounter").GetComponent<Text>().text = "Stop = " + GameObject.Find("Player").GetComponent<Lulu>().GetStopCounter();
            */
        }

        //Debug.Log("isInvince2 = " + isInvince2);
        //Debug.Log(invinceTime + " " + isInvince + " " + WarpControl.nowMagic);
        //if (invinceTime < maxInvince)
        if (invinceTime < maxInvince)   //無敵時間計測
        {
            invinceTime += Time.deltaTime;
        }
        //else if (isInvince)
        else if(invinceTime == maxInvince)
        {
            return;
        }
        else
        {
            GetComponent<WarpControl_Old>().SetIntervalTimer(0);    //無敵時間終了時、MP回復タイマーリセット
            //isInvince = false;
            invinceFlag--;
            invinceTime = maxInvince;
        }
    }

    public void Damage(int damage)  //ダメージ
    {
        if (GetInvince() == true || Life.nowLife <= 0)
        {
            return;
        }
        //if (GetInvince() == false && isInvince2 == false)

        if (WarpControl_Old.nowMagic - damage <= 0 && Friends.Pepe && !WarpControl_Old.overHeat)//かすりヒット
        {
            Instantiate(Resources.Load("HitEffect1"), transform.position + transform.up * 2, Quaternion.identity);
            GetComponent<WarpControl_Old>().EmergencyEffectStart();
            GetComponent<Lulu>().avoidance = false;
            SE.playnum = 5;
            GetComponent<Lulu>().DamageAnimation();
            GameObject collapse = (GameObject)Instantiate(CollapseHeart, transform.position, Quaternion.identity, GameObject.Find("Life Gage").transform);
            collapse.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);

            damage = Mathf.Clamp(damage - (int)WarpControl_Old.nowMagic, 5, (int)WarpControl_Old.maxMagic);
            WarpControl_Old.nowMagic = 0;

            if (Life.nowLife - (damage) > 0)
                DOTween.To
                (
                    () => Life.nowLife,       //何に
                    x => Life.nowLife = x,  //何を
                    (int)(Life.nowLife - (damage)),     //どこまで(最終的な値)
                    0.4f       //どれくらいの時間
                );
            else
                Life.nowLife = 0;

            WarpControl_Old.nowMagic = Mathf.Clamp(WarpControl_Old.nowMagic - damage, 0, WarpControl_Old.maxMagic);
            GameObject collapseMP = Instantiate(CollapseCharge, transform.position, Quaternion.identity, GameObject.Find("Back Gage").transform);
            collapseMP.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
        }
        else if (WarpControl_Old.overHeat || !Friends.Pepe)//直撃
        {
            Instantiate(Resources.Load("HitEffect1"), transform.position + transform.up * 2, Quaternion.identity);
            SE.playnum = 29;
            GetComponent<Lulu>().DamageAnimation();
            GameObject collapse = Instantiate(CollapseHeart, transform.position, Quaternion.identity, GameObject.Find("Life Gage").transform);
            collapse.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);

            if (Life.nowLife - (damage) > 0)
                DOTween.To
            (
                () => Life.nowLife,       //何に
                x => Life.nowLife = x,  //何を
                (int)(Life.nowLife - damage),     //どこまで(最終的な値)
                0.4f       //どれくらいの時間
            );
            else
                Life.nowLife = 0;
        }
        else//回避
        {
            GetComponent<WarpControl_Old>().EmergencyEffectStart();
            SE.playnum = 5;
            WarpControl_Old.nowMagic = Mathf.Clamp(WarpControl_Old.nowMagic - damage, 0, WarpControl_Old.maxMagic);
            GameObject collapseMP = (GameObject)Instantiate(CollapseCharge, transform.position, Quaternion.identity, GameObject.Find("Back Gage").transform);
            collapseMP.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);

            DOTween.To
        (
            () => Life.nowLife,       //何に
            x => Life.nowLife = x,  //何を
            (int)(Life.nowLife - 5),     //どこまで(最終的な値)
            0.4f       //どれくらいの時間
        );
        }

        //チャージ減少
        if (Friends.Nord)
            GameObject.Find("Player").GetComponent<Lulu>().PowerUp(-damage - (damage / 2));
        else
            GameObject.Find("Player").GetComponent<Lulu>().PowerUp(-300);
        InvinceStart();
        MPGage.SetActive(true);
        GetComponentInChildren<MPGage>().GetGage();

    }

    public void setInvince(bool set)    //無敵フラグセット
    {
        if (set)
        {
            invinceFlag++;
        }
        else
        {
            invinceFlag--;
        }

        //isInvince2 = set;
    }

    public void InvinceStart()  //無敵時間開始
    {
        invinceTime = 0f;
        //isInvince = true;
        invinceFlag++;
    }
    public bool GetInvince()    //無敵フラグ取得
    {
        
        if(invinceFlag > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
        
        //return isInvince;
    }

}
