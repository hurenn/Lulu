using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageSwitch : MonoBehaviour
{

    public static bool startMessage = false;
    public bool boad = false;
    public bool messageSwitch = false;
    float windowActive = 0;
    public GameObject Window;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (startMessage == false)
        {
            if (windowActive > 0)
            {
                windowActive -= 0.2f;
            }
        }
        else
        {
            if (windowActive < 0.7f)
            {
                windowActive += 0.2f;
            }
            else
                GameObject.Find("MessageUI").GetComponent<Message>().objectName = gameObject.name;
        }
        Window.GetComponent<Image>().color = new Color(1, 1, 1, windowActive);
    }
    private void OnTriggerEnter2D(Collider2D Collider)
    {
        if (startMessage == false)
        {
            if (Collider.gameObject.tag.Equals("Player"))
            {
                startMessage = true;
            }

        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (startMessage == true)
        {
            if (collision.gameObject.tag.Equals("Player"))
            {
                GameObject.Find("MessageUI").GetComponent<Message>().objectName = "StartBoard";
                startMessage = false;
            }
        }
    }
}
