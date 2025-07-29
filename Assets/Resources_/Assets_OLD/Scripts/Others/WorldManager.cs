using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class StageScore
{
    static int score;
    static string rank;

    public void setScore(int getScore, string getRank)
    {
        if (score < getScore){
            score = getScore;
            rank = getRank;
        }
        //Debug.Log("setScore " + score);
    }
    public int showScore()
    {
        return score;
    }
    public string showRank()
    {
        return rank;
    }
    public void resetScore()
    {
        score = 0;
        rank = "";
    }
}
public class WorldManager : MonoBehaviour
{
    StageScore stage1 = new StageScore();
    StageScore stage2 = new StageScore();
    StageScore stage3 = new StageScore();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("stage1.score = " + stage1.showScore() + " " + stage1.showRank());
    }

    public void SetScore(int number, int score, string rank)
    {
        //Debug.Log(number + " : " + score + " " + rank);
        switch (number)
        {
            case 1:
                stage1.setScore(score, rank);
                break;

            case 2:
                stage2.setScore(score, rank);
                break;

            case 3:
                stage3.setScore(score, rank);
                break;
        }
    }

    public int ShowScore(int number)
    {
        switch (number)
        {
            case 1:
                return stage1.showScore();

            case 2:
                return stage2.showScore();

            case 3:
                return stage3.showScore();
        }

        //Debug.Log("Error");
        return 0;
    }

    public string ShowRank(int number)
    {
        switch (number)
        {
            case 1:
                return stage1.showRank();
            case 2:
                return stage2.showRank();
            case 3:
                return stage3.showRank();
        }
        return "";
    }

    public void ScoreReset()
    {
        stage1.resetScore();
        stage2.resetScore();
        stage3.resetScore();
    }

}
