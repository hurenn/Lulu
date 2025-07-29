using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;

public class EventManager : MonoBehaviour
{
    Flowchart flowchart;
    public static string EventName = "Start 1";
    bool massage;
    // Start is called before the first frame update
    void Start()
    {
        flowchart = FindObjectOfType<Flowchart>();
        MessageReceived[] receivers = GameObject.FindObjectsOfType<Fungus.MessageReceived>();
        if (receivers != null)
        {
            foreach (var receiver in receivers)
            {
                receiver.OnSendFungusMessage(EventName);
                massage = true;
            }
        }
    }

    // Update is called once per frame
    void Update()//イベント終了後
    {

    }

    public void EndEvent()
    {
        massage = false;
        LoadScene();
    }

    public void LoadScene()
    {
        GameObject.Find("SceneLoad").GetComponent<NowLoading>().LoadScene();
    }
}
