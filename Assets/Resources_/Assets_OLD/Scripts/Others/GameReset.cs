using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameReset : MonoBehaviour
{
    public static int resetNumber = 0;
    // Use this for initialization

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (resetNumber)
        {
            case 1:     //リスタート
                RestartParameter();
                StartCoroutine("wait");
                break;
            case 2:     //タイトル
                StartCoroutine("wait2");
                break;
            case 3:     //終了
                StartCoroutine("wait3");
                break;
        }
    }
    IEnumerator wait()
    {
        CommonWait();
        yield return new WaitForSeconds(0.2f);
        FadeManager.Instance.LoadScene(SceneManager.GetActiveScene().name, 0.15f);
        MessageList.MessageNow = false;
    }
    IEnumerator wait2()
    {
        CommonWait();
        yield return new WaitForSeconds(0.2f);
        FadeManager.Instance.LoadScene("Title", 0.15f);
        MessageList.MessageNow = false;
    }
    IEnumerator wait3()
    {
        CommonWait();
        Time.timeScale = 1f;
        yield return new WaitForSeconds(0.2f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                     UnityEngine.Application.Quit();
#endif
    }

    void CommonWait()
    {
        resetNumber = 0;
        SE.playnum = 32;
    }

    //ゲーム終了リセット
    public static void GameResetParameter()
    {
        Friends.Marlica = false;
        Friends.Nord = false;
        Friends.Pepe = false;
        CollectCoin.Collected = 0;
        Life.saveCoin = 0;

        NextStageParameter();
    }

    //リスタートリセット
    public static void RestartParameter()
    {
        Life.over = false;
        CollectCoin.Collected = Life.saveCoin;
        CommonReset();
    }

    //ステージ移動リセット
    public static void NextStageParameter()
    {
        StartPoint.pos = Vector3.zero;
        WarpControl_Old.ResetMax();
        PlusScore.plusScore = 0;
        CommonReset();
    }

    //共通リセット
    static void CommonReset()
    {
        if (GameObject.Find("Life Gage"))
            GameObject.Find("Life Gage").GetComponent<Life>().stopCor();
        WarpControl_Old.nowMagic = WarpControl_Old.maxMagic;
        MessageList.MessageNow = false;
        Life.nowLife = Life.maxLife;
    }
}
