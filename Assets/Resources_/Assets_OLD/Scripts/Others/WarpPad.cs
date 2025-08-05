using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Runtime.CompilerServices;

public class WarpPad : MonoBehaviour
{
    public string LoadScene;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadScene = SceneManager.GetActiveScene().name;
            StageLoad();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadScene = "Title";
            StageLoad();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (Life.nowLife < Life.maxLife)
                Life.nowLife = Life.maxLife;
            if (WarpControl_Old.nowMagic < WarpControl_Old.maxMagic)
                WarpControl_Old.nowMagic = WarpControl_Old.maxMagic;

            if (GetComponent<BlockTrigger>()) //イベントがあれば再生、なければステージ移動。
            {
                GetComponent<BlockTrigger>().MessageStart();
            }
            else
            {
                SceneLoad();
            }
        }
    }

    public void SceneLoad()
    {
        GameReset.RestartParameter();
        StartCoroutine("wait");
    }
    public void StageLoad()
    {
        GameReset.NextStageParameter();
        StartCoroutine("wait");
    }


    IEnumerator wait()
    {
        StartPoint.pos = Vector3.zero;
        Instantiate(Resources.Load("Warp Animation"), transform.position, Quaternion.identity);
        GameObject.Find("Player").GetComponent<SpriteRenderer>().enabled = false;
        GameObject.Find("GameManager").GetComponent<WhiteFade>().WhiteIn();
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(LoadScene);
        MessageList.MessageNow = false;
        yield return new WaitForSeconds(0.5f);
        GameObject.Find("GameManager").GetComponent<WhiteFade>().WhiteOut();
    }
}
