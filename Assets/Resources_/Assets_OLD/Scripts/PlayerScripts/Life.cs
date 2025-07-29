using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Life : MonoBehaviour
{
    Animator anim;
    GameObject player;
    public Image lifeGage;
    public Image IceGage;
    public static int saveCoin = 0; //途中経過セーブ

    public static float nowLife = 100;  //ライフ残量
    public static float maxLife = 100;  //ライフ上限
    public static bool over = false;    //ゲームオーバーフラグ
    Coroutine overCoroutine;

    // Use this for initialization
    void Start()
    {
        player = GameObject.Find("Player");
        anim = player.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (nowLife <= 0 && over == false)  //ゲームオーバー
        {
            nowLife = 0;
            overCoroutine = StartCoroutine("GameOver");
        }
        if (nowLife > maxLife)
            nowLife = maxLife;

        //ライフゲージ表示処理
        lifeGage = GameObject.Find("Red Gage").GetComponent<Image>();
        IceGage = GameObject.Find("Life Ice").GetComponent<Image>();
        lifeGage.fillAmount = nowLife / maxLife;
        IceGage.fillAmount = nowLife / maxLife;

        if (GameObject.Find("GameManager").GetComponent<Cheat>().MaxHP && nowLife != maxLife)//体力無限チート
            nowLife = maxLife;
    }

    IEnumerator GameOver()  //ゲームオーバー処理
    {
        //やられ演出・操作不能
        GameObject.Find("Message Window").SetActive(false);
        GameObject.Find("Player").GetComponent<Lulu>().SetEnd();
        anim.Play("Damage");
        over = true;
        DOTween.To
        (
            () => GameObject.Find("BGM").GetComponent<AudioSource>().volume,       //何に
            x => GameObject.Find("BGM").GetComponent<AudioSource>().volume = x,  //何を
            0,     //どこまで(最終的な値)
            2f     //どれくらいの時間
        );

        //フラッシュ
        Instantiate(Resources.Load("Flash"));
        yield return new WaitForSeconds(0.3f);
        Instantiate(Resources.Load("Flash"));
        yield return new WaitForSeconds(1f);

        //逃げ
        //GameObject.Find("Player").GetComponent<Animator>().Play("Revive");
        GameObject flush = (GameObject)Instantiate(Resources.Load("Warp Animation"));
        flush.transform.position = GameObject.Find("Player").transform.position;
        flush.transform.localScale *= 2;
        yield return new WaitForSeconds(0.1f);
        GameObject.Find("Player").SetActive(false);

        //リセット
        yield return new WaitForSeconds(2f);
        GameObject.Find("GameManager").GetComponent<WhiteFade>().WhiteIn();
        yield return new WaitForSeconds(0.5f);
        GameReset.RestartParameter();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        MessageList.MessageNow = false;
        yield return new WaitForSeconds(0.5f);
        GameObject.Find("GameManager").GetComponent<WhiteFade>().WhiteOut();

    }

    public void stopCor()
    {
        DOTween.Clear();
        //StopCoroutine(overCoroutine);
    }

    public static void RestartParameter()   //ゲームオーバーリセット
    {
        GameReset.RestartParameter();
        Instantiate(Resources.Load("Flash"));
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
