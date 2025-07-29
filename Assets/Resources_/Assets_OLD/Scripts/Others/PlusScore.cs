using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlusScore : MonoBehaviour
{
    public Text plusScoreText;//メッセージUI

    int startX;
    public static int plusScore = 0;
    static float bornus = 1;
    static int bornusCount = 0;
    GameObject player;

    public static bool end = false;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        startX = (int)player.transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {
        if(startX < (int)player.transform.position.x)
        {
            plusScore += (int)((player.transform.position.x - startX) * bornus);
            bornusCount += (int)(player.transform.position.x - startX);

            startX = (int)player.transform.position.x;
        }

        if (bornusCount > 100)
        {
            bornus += 0.1f;
            bornusCount = 0;
        }

        //plusScore += (int)((maxX - startX) * bornus);
        /*
        if (Player.invinceTime <= 0.2f)
        {
            if (StoneUI.use == false)
                Reset();
        }
        */
        if (end == true)
        {
            CollectCoin.Collected += plusScore;
            Reset();
            end = false;
        }

        plusScoreText.text = "ボーナス : " + plusScore.ToString();
    }

    private void Reset()
    {
        plusScore = 0;
        bornus = 1;
        bornusCount = 0;
        if (startX < player.transform.position.x)
            startX = (int)player.transform.position.x;
    }

}
