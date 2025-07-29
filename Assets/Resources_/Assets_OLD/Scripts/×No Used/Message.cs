using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Message : MonoBehaviour
{

    private Text messageText;//メッセージUI
    public TextMeshProUGUI score;
    public TextMeshProUGUI rank;

    public string message;
    /*
    [SerializeField]
    int maxTextLength = 90;
    int textLength = 0;
    [SerializeField]
    int maxLine = 3;
    int nowLine = 0;
    [SerializeField]
    float textspeed = 0.05f;
    float elapsedTime = 0f;//経過時間
    int nowTextNum = -1;//現在の文字番号
    bool isOneMessage = false;//一回分のメッセージを表示したか
    bool isEndMessage = false;//すべてのメッセージを表示したか
    float timer = 16f;
    */
    public float nexttime = 20f;
    Vector2 tmp;
    public string objectName;
    bool show = false;

    string[] vs = new string[14];

    // Use this for initialization
    void Start()
    {
        vs[0] = "";/*
        vs[1] = "ステージ上に転がっている物は\n[X] を押しっぱなしで持つことができるぞ！\n" + "鍵を持って行って右の扉を開けてみよう！";
        vs[2] = "持ち上げた物はXを離すと投げる！\n" + "投げた物を敵に当てればダメージを与えられるぞ！";
        vs[3] = "物には重さがあるぞ！\n" + "基本的に重いほうが攻撃力が高いが扱いづらい！\n" + "状況と好みで使い分けよう！";
        vs[4] = "キミは空中で [Z] を押すか、\n方向キーを2連続で押すことで短距離のワープができる！\n足場に届かない時や先に進めない時などにどんどん活用しよう！";
        vs[5] = "ワープを使いすぎるとしばらくワープできなくなる！\n頭上に出ているゲージに注意して使おう！";
        vs[6] = "お金は一定量集めることで魔法石が手に入るぞ！\n左上に青い石のマークがある時に [Q] を押して使おう！\nしばらくワープが使い放題だ！";
        vs[7] = "これはワープパッドだ！\n[↑] を押しながら触れることで次のエリアへ移動するぞ！";
        vs[8] = "敵を倒した時、お金を落とす！\nその時できるだけ強い力で倒すと、多額のお金を落としやすい！";
        vs[9] = "空中で移動せずに [Z] を押すとその場にワープする！\nこの瞬間に触れた敵の攻撃は向きを反転させるぞ！\n無理せずにタイミングを見計らってカウンターだ！";
        vs[10] = "攻撃に触れると左上のHPが減る！\nHP0の状態で攻撃に当たってしまうとゲームオーバーだから注意だ！";*/
        vs[11] = "ステージ1 「はじまり」";
        vs[12] = "ステージ2 「つぎのひ」";
        vs[13] = "ステージ3 「おしまい」";
        tmp = GameObject.Find("Panel").transform.position;
        GameObject.Find("Panel").transform.position = new Vector2(tmp.x - 3000, 37);
        messageText = GetComponentInChildren<Text>();
        messageText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.Find("Panel").transform.position.x >= tmp.x + 995)
            GameObject.Find("Panel").transform.position = new Vector2(tmp.x-3000, 37);


        if (MessageSwitch.startMessage == true)
        {
            if(show == false)
            {
                messageText.text = "";
                iTween.MoveTo(GameObject.Find("Panel"), iTween.Hash("x", tmp.x, "time", 0.3));
                show = true;
                StartCoroutine("wait");
            }

            switch (objectName)
            {
                case "StartBoard":
                    {
                        messageText.text = vs[0];
                        break;
                    }
                case "CatchBoard":
                    {
                        messageText.text = vs[1];
                        break;
                    }
                case "ThrowBoard":
                    {
                        messageText.text = vs[2];
                        break;
                    }
                case "HeavyBoard":
                    {
                        messageText.text = vs[3];
                        break;
                    }
                case "WarpBoard":
                    {
                        messageText.text = vs[4];
                        break;
                    }
                case "BurstBoard":
                    {
                        messageText.text = vs[5];
                        break;
                    }
                case "StoneBoard":
                    {
                        messageText.text = vs[6];
                        break;
                    }
                case "NextBoard":
                    {
                        messageText.text = vs[7];
                        break;
                    }
                case "KillBoard":
                    {
                        messageText.text = vs[8];
                        break;
                    }
                case "ReflectBoard":
                    {
                        messageText.text = vs[9];
                        break;
                    }
                case "DamageBoard":
                    {
                        messageText.text = vs[10];
                        break;
                    }
                case "Stage1":
                    {
                        messageText.text = vs[11];
                        score.text = GameObject.Find("GameManager").GetComponent<WorldManager>().ShowScore(1).ToString();
                        rank.text = GameObject.Find("GameManager").GetComponent<WorldManager>().ShowRank(1);
                        break;
                    }
                case "Stage2":
                    {
                        messageText.text = vs[12];
                        score.text = GameObject.Find("GameManager").GetComponent<WorldManager>().ShowScore(2).ToString();
                        rank.text = GameObject.Find("GameManager").GetComponent<WorldManager>().ShowRank(2);
                        break;
                    }
                case "Stage3":
                    {
                        messageText.text = vs[13];
                        score.text = GameObject.Find("GameManager").GetComponent<WorldManager>().ShowScore(3).ToString();
                        rank.text = GameObject.Find("GameManager").GetComponent<WorldManager>().ShowRank(3);
                        break;
                    }
            }
        }
        else
        {
            messageText.text = "";
            score.text = "";
            rank.text = "";
        }
        /*
        if (MessageSwitch.startMessage == false)
        {
            if(show == true)
            {
                messageText.text = "";
                StartCoroutine("wait");
                iTween.MoveTo(GameObject.Find("Panel"), iTween.Hash("x", tmp.x + 3000, "time", 0.3));
                show = false;
            }
        }

        /*
        if (GameObject.Find("Message").GetComponent<MessageSwitch>().startMessage == true)
        {
            timer += Time.deltaTime;
            if (timer > nexttime)
            {
                switch (nowTextNum)
                {
                    case -1:
                        iTween.MoveTo(GameObject.Find("Panel"), iTween.Hash("x", tmp.x, "time", 0.3));
                        timer = 1.7f;
                        nowTextNum++;
                        break;

                    case 0:
                        messageText.text = "スイッチを押した";
                        timer = 0;
                        nowTextNum++;
                        break;

                    case 1:
                        messageText.text = "特に意味はないが";
                        timer = 0;
                        nowTextNum++;
                        break;

                    case 2:
                        messageText.text = "メッセージが出る";
                        timer = 0;
                        nowTextNum++;
                        break;

                    case 3:
                        messageText.text = "";
                        timer = 1.7f;
                        nowTextNum++;
                        break;

                    case 4:
                        iTween.MoveTo(GameObject.Find("Panel"), iTween.Hash("x", tmp.x + 995, "time", 0.3));
                        timer = 16f;
                        nowTextNum = -1;
                        GameObject.Find("Message").GetComponent<MessageSwitch>().startMessage = false;
                        GameObject.Find("Message").GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
                        break;
                }
            }
        }*/
    }

    IEnumerator wait()
    {
        yield return new WaitForSeconds(0.7f);
    }

}
