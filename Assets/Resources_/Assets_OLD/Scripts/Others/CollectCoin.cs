using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectCoin : MonoBehaviour {

    public Text scoreText, Time;//メッセージUI
    public static int Collected = 0;

    // Use this for initialization
    void Start ()
    {
        scoreText.text = "0";
	}
	
	// Update is called once per frame
	void Update () {
        scoreText.text = ""+Collected.ToString();
        Time.text = Result.timer.ToString("F3");
        /*
        if(StoneUI.level < 3)
        subText.text = "Lv." + StoneUI.level + " Next : " + StoneUI.nowCoin.ToString();
        else
            subText.text = "Lv." + StoneUI.level + " Next : ---";
            */
    }
}
