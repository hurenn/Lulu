using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Fungus;

public class WhiteFade : MonoBehaviour
{
    GameObject fade;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void WhiteIn()
    {
        if (!fade)
        {
            GameObject whitefade = (GameObject)Instantiate(Resources.Load("WhiteFade"));
            fade = whitefade.transform.GetChild(0).gameObject;
        }
        DOTween.To
        (
            () => fade.GetComponent<Image>().color,       //何に
            x => fade.GetComponent<Image>().color = x,  //何を
            new Color(1f, 1f, 1f, 1f),     //どこまで(最終的な値)
            0.5f       //どれくらいの時間
        );
        //シーンを切り替えてもこのゲームオブジェクトを削除しないようにする
        DontDestroyOnLoad(fade);
    }
    public void WhiteOut()
    {
        if (!fade)
        {
            GameObject whitefade = (GameObject)Instantiate(Resources.Load("WhiteFade"));
            fade = whitefade.transform.GetChild(0).gameObject;
            fade.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        }
        DOTween.To
        (
            () => fade.GetComponent<Image>().color,       //何に
            x => fade.GetComponent<Image>().color = x,  //何を
            new Color(1f, 1f, 1f, 0f),     //どこまで(最終的な値)
            0.5f       //どれくらいの時間
        );
        Destroy(fade.gameObject, 1f);
    }
    public void PlayerStopIn()
    {
        //Debug.Log("PlayerStopIn");
        GameObject.Find("Player").GetComponent<Lulu>().SetStop(true);
        GameObject.Find("Player").GetComponent<WarpControl_Old>().SetBan(true);
    }
    public void PlayerStopOut()
    {
        //Debug.Log("PlayerStopOut");
        if (GameObject.Find("Player").GetComponent<Lulu>().GetStopCounter() > 0)
        {
            GameObject.Find("Player").GetComponent<Lulu>().SetStop(false);
        }
        GameObject.Find("Player").GetComponent<WarpControl_Old>().SetBan(false);
    }
}
