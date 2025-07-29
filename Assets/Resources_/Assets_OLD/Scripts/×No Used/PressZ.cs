using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PressZ : MonoBehaviour {

    public bool start = false;
    float timer = 0;
    float alpha;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        timer += Time.deltaTime;
        if (timer >= 1.5f)
            GetComponent<Image>().color = new Color(1f, 1f, 1f, 0);
        if(timer >= 2.3f)
        {
            GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            if(start == false)
                timer = 0;
        }


        if(start == true)
        {
            GameObject.Find("Up").GetComponent<RectTransform>().localPosition += new Vector3(0, timer * 10, 0);
            GameObject.Find("Down").GetComponent<RectTransform>().localPosition -= new Vector3(0, timer * 10, 0);
            GameObject.Find("Panel").GetComponent<Image>().color += new Color(0, 0, 0, timer * 10);
        }

        if(start == true && timer >= 3f)
        {
            Friends.Marlica = true;
            Friends.Nord = true;
            Friends.Pepe = true;
            SceneManager.LoadScene("EventScene");


            NowLoading.SceneName = "stage1";
            Friends.Marlica = false;
            Friends.Nord = false;
            Friends.Pepe = false;

                CollectCoin.Collected = 0;
                Life.saveCoin = 0;
                EventManager.EventName = "Start 1";
                wait();
                GameObject.Find("SceneLoad").GetComponent<NowLoading>().LoadEvent();
        }


        if (Input.GetKeyDown(KeyCode.Space) && start == false)
        {
            SE.playnum = 2;
            start = true;
            timer = 0;
        }

    }
    IEnumerator wait()
    {
        yield return new WaitForSeconds(3f);
    }
}
