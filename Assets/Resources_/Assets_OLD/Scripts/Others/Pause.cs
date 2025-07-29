using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pause : MonoBehaviour
{
    bool pauseNow;
    bool pauseBan;
    float pauseCooltime = 0;

    float timeSave;
    float volumeSave;
    int menuSelect = 1;
    public GameObject select;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void setBan(bool set)
    {
        pauseBan = set;
    }

    // Update is called once per frame
    void Update()
    {
        //プレイ状態復帰
        if (GameManager.currentGameState == GameState.Wait)
        {
            if (pauseCooltime > 0.2f)
            {
                GameObject.Find("BGM").GetComponent<AudioSource>().volume = volumeSave;
                GameManager.Instance.SetCurrentState(GameState.Playing);
                Time.timeScale = timeSave;
                pauseNow = false;
            }
        }

        if (pauseCooltime < 1f)
        {
            pauseCooltime += Time.unscaledDeltaTime;
        }

        //ゲームプレイ状態でスペースキーを押したとき
        if (Input.GetKeyDown(KeyCode.Space) && pauseBan == false && pauseCooltime > 0.2f)
        {
            pauseCooltime = 0;
            if (GameManager.currentGameState == GameState.Playing || GameManager.currentGameState == GameState.Pause)
            {
                //メニュー画面起動
                pauseStart();
            }
        }

        if (pauseNow)
        {
            //メニュー画面操作
            pauseControl();
        }

        if (pauseBan && pauseNow)
        {
            //メニュー画面解除
            pauseCansel();
        }
    }

    void pauseStart()
    {
        SE.playnum = 32;
        switch (pauseNow)
        {
            case false:
                menuSelect = 1;
                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(0).gameObject.GetComponent<Animator>().Play("PausePanel");
                timeSave = Time.timeScale; Time.timeScale = 0;
                volumeSave = GameObject.Find("BGM").GetComponent<AudioSource>().volume;
                GameObject.Find("BGM").GetComponent<AudioSource>().volume = 0.3f;

                GameManager.Instance.SetCurrentState(GameState.Pause);
                pauseNow = true;
                break;

            case true:
                pauseCansel();
                break;
        }
    }

    void pauseControl()
    {
        select.GetComponent<RectTransform>().anchoredPosition =
            transform.GetChild(0).transform.GetChild(menuSelect).GetComponent<RectTransform>().anchoredPosition;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SE.playnum = 2;
            menuSelect += 1;
            if (menuSelect > 3)
                menuSelect = 1;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SE.playnum = 2;
            menuSelect -= 1;
            if (menuSelect < 1)
                menuSelect = 3;
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            switch (menuSelect)
            {
                case 1:     //ゲームにもどる
                    SE.playnum = 32;
                    break;
                case 2:     //やりなおす
                    GameReset.resetNumber = 1;
                    break;
                case 3:     //タイトルへもどる
                    GameReset.resetNumber = 2;
                    break;
            }
            pauseCansel();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameReset.resetNumber = 3;
        }
    }

    public void pauseCansel()
    {
        transform.GetChild(0).gameObject.GetComponent<Animator>().Play("PauseCansel");

        GameManager.Instance.SetCurrentState(GameState.Wait);
    }
}
