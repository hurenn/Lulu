using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Result : MonoBehaviour
{
    public static int StageNumber = 1;
    public int step = 0;

    public TextMeshProUGUI Member;
    public TextMeshProUGUI goalTime;
    public TextMeshProUGUI FinalScore;
    public TextMeshProUGUI Rank;

    int friends;
    float memberBornus;

    public static float timer;
    public int firstTime;
    public int secondTime;
    public int thardTime;
    public int forthTime;
    float timerBornus = 1;

    int score;
    public int rankA = 3000;
    public int rankB = 2000;
    public int rankC = 1000;
    public int rankD = 500;
    int mathmatic;

    public bool timerDebug;
    public bool stepDebug;

    // Start is called before the first frame update
    void Start()
    {
        score = 0;
        friends = 1;

        if (Friends.Marlica)
            friends++;
        if (Friends.Nord)
            friends++;
        if (Friends.Pepe)
            friends++;

        switch (friends)
        {
            case 1:
                memberBornus = 1.2f;
                break;
            case 2:
                memberBornus = 1f;
                break;
            case 3:
                memberBornus = 0.8f;
                break;
            case 4:
                memberBornus = 0.5f;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (timerDebug)
            Debug.Log(timer);
        if (stepDebug)
            Debug.Log(step);

        if (step == 0 && GameManager.currentGameState == GameState.Playing)
        {
            timer += Time.deltaTime;
        }

        if (step == 1)
        {
            if (timer < firstTime)
            {
                timerBornus = 2f;
            }
            else if (timer < secondTime)
            {
                timerBornus = 1.5f;
            }
            else if (timer < thardTime)
            {
                timerBornus = 1f;
            }
            else if (timer < forthTime)
            {
                timerBornus = 0.8f;
            }
            else
            {
                timerBornus = 0.5f;
            }

            if(PlusScore.plusScore != 0)
            {
                return;
            }

            if (score == 0)
            {
                Member.text = "Member :    " + friends + "    --->   ×" + memberBornus;
                goalTime.text = "Time      :    " + timer.ToString("F3") + "    --->   ×" + timerBornus;
                FinalScore.text = "FinalScore :  " + CollectCoin.Collected;
                score = CollectCoin.Collected;
                mathmatic = (int)(score * memberBornus * timerBornus);
                wait();
            }
            else
            {
                if (mathmatic < score)
                {
                    score -= (score - mathmatic) / 10;
                    if (score - mathmatic <= 100)
                    {
                        score = mathmatic;
                        step = 2;
                        wait();
                    }
                }
                if (mathmatic > score)
                {
                    score += (mathmatic - score) / 10;
                    if (mathmatic - score <= 100)
                    {
                        score = mathmatic;
                        step = 2;
                        wait();
                    }
                }
                FinalScore.text = "FinalScore :  " + score;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                score = mathmatic;
                FinalScore.text = "FinalScore :  " + score;
                step = 2;

                wait();
            }
        }

        if (step >= 2)
        {
            if (friends >= 3)
            {
                Rank.text = "OK";
                GameObject.Find("GameManager").GetComponent<WorldManager>().SetScore(StageNumber, score, "OK");
            }
            else if (score >= rankA)
            {
                Rank.text = "A";
                GameObject.Find("GameManager").GetComponent<WorldManager>().SetScore(StageNumber, score, "A");
            }
            else if (score >= rankB)
            {
                Rank.text = "B";
                GameObject.Find("GameManager").GetComponent<WorldManager>().SetScore(StageNumber, score, "B");
            }
            else if (score >= rankC)
            {
                Rank.text = "C";
                GameObject.Find("GameManager").GetComponent<WorldManager>().SetScore(StageNumber, score, "C");
            }
            else if (score >= rankD)
            {
                Rank.text = "D";
                GameObject.Find("GameManager").GetComponent<WorldManager>().SetScore(StageNumber, score, "D");
            }
            else
            {
                Rank.text = "E";
                GameObject.Find("GameManager").GetComponent<WorldManager>().SetScore(StageNumber, score, "E");
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                timer = 0;
                step ++;
            }
        }

    }

    IEnumerator wait()
    {
        yield return new WaitForSeconds(3f);
    }
}
