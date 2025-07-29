using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class SelectArrow : MonoBehaviour
{
    public List<string> stageList = new List<string>(); //ステージリスト
    int number = 0;
    Text stageName;
    bool selectAble = true;

    // Start is called before the first frame update
    void Start()
    {
        stageName = GetComponent<Text>();
    }

    public void selectUnable()
    {
        selectAble = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (selectAble == false)
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SE.playnum = 1;
            number++;
            if(number >= stageList.Count)
            {
                number = 0;
            }
            transform.GetChild(0).GetComponent<RectTransform>().DOPunchScale(
               new Vector3(1.1f, 1.1f),
               0.1f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SE.playnum = 1;
            number--;
            if(number < 0)
            {
                number = stageList.Count - 1;
            }
            transform.GetChild(1).GetComponent<RectTransform>().DOPunchScale(
               new Vector3(1.1f, 1.1f),
               0.1f);
        }

        switch(number){
            case 0:
                stageName.text = "はじめから";
                GetComponent<GameStart>().NextStageTitle.GetComponentInChildren<Text>().text
                    = "チャプター1\n賢者の娘";
                break;
            case 1:
                stageName.text = "チャプター1　ボス戦";
                GetComponent<GameStart>().NextStageTitle.GetComponentInChildren<Text>().text
                    = "チャプター1\n賢者の娘";
                break;
            case 2:
                stageName.text = "チャプター2";
                GetComponent<GameStart>().NextStageTitle.GetComponentInChildren<Text>().text
                    = "チャプター2\n氷の盾";
                break;
            case 3:
                stageName.text = "チャプター2　ボス戦";
                GetComponent<GameStart>().NextStageTitle.GetComponentInChildren<Text>().text
                    = "チャプター2\n氷の盾";
                break;
            case 4:
                stageName.text = "Ex　ボス戦";
                GetComponent<GameStart>().NextStageTitle.GetComponentInChildren<Text>().text
                    = "Ex　ボス戦";
                break;
        }

        GetComponent<WarpPad>().LoadScene = stageList[number];

    }
}
