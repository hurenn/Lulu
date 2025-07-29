using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IllustInsert : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void WhiteFade()
    {
        GameObject panel = GameObject.Find("White");
        for(int i = 1; i < 50; i++)
        {
            panel.GetComponent<Image>().color = new Color(1f,1f,1f,i / 50);
        }
    }
    void Dressing1()
    {
        iTween.MoveTo(GameObject.Find("身支度1"), iTween.Hash("x", 11.4f, "y", 1.6f));
    }
    void Dressing2()
    {
        iTween.MoveTo(GameObject.Find("身支度2"), iTween.Hash("x", 6f, "y", -0.4f));
    }
    void Dressing3()
    {
        iTween.MoveTo(GameObject.Find("身支度3"), iTween.Hash("x", 8.7f, "y", 0.7f));
    }
}
