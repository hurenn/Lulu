using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Friends : MonoBehaviour
{
    public static bool Marlica = false;
    public static bool Nord = false;
    public static bool Pepe = false;
    float recoverTimer = 0;

    [SerializeField]
    bool MarlicaIn;
    [SerializeField]
    bool NordIn;
    [SerializeField]
    bool PepeIn;

    // Start is called before the first frame update
    void Start()
    {
        if (MarlicaIn)
        {
            marlicaIn();
        }
        if (NordIn)
        {
            nordIn();
        }
        if (PepeIn)
        {
            pepeIn();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Marlica == true)
        {
            recoverTimer += Time.deltaTime;
            if (recoverTimer > 2f)
            {
                Life.nowLife += 1;
                recoverTimer = 0;
            }
        }
        if(Marlica == true && GameObject.Find("Life Fire").GetComponent<RectTransform>().localScale != new Vector3(0.6f, 1, 1))
        {
            GameObject.Find("Life Fire").GetComponent<RectTransform>().localScale = new Vector3(0.6f, 1, 1);
        }
        if(Nord == true && GameObject.Find("Life Ice").GetComponent<RectTransform>().localScale != new Vector3(0.6f, 0.6f, 1))
        {
            GameObject.Find("Life Ice").GetComponent<RectTransform>().localScale = new Vector3(0.6f, 0.6f, 1);
        }
        if(Pepe == true && GameObject.Find("Life Light").GetComponent<RectTransform>().localScale != new Vector3(1.5f, 1.5f, 1))
        {
            GameObject.Find("Life Light").GetComponent<RectTransform>().localScale = new Vector3(1.5f, 1.5f, 1);
        }
        
    }
    public void marlicaIn()
    {
        if (Marlica == false)
        {
            Marlica = true;
            GameObject.Find("Life Fire").GetComponent<RectTransform>().localScale = new Vector3(0.6f, 1, 1);

        }
    }
    public void marlicaOut()
    {
        if(Marlica == true)
        {
            Marlica = false;
            GameObject.Find("Life Fire").GetComponent<RectTransform>().localScale = new Vector3(0, 0, 0);
        }
    }

    public void nordIn()
    {
        if(Nord == false)
        {
            Nord = true;
            GameObject.Find("Life Ice").GetComponent<RectTransform>().localScale = new Vector3(0.6f, 0.6f, 1);
            Life.maxLife = 300;
            Life.nowLife = 300;
        }
    }
    public void nordOut()
    {
        if(Nord == true)
        {
            Nord = false;
            GameObject.Find("Life Ice").GetComponent<RectTransform>().localScale = new Vector3(0, 0, 0);
            Life.nowLife = 100;
            Life.maxLife = 100;
        }
    }
    public void pepeIn()
    {
        if (Pepe == false)
        {
            Pepe = true;
            GameObject.Find("Life Light").GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        }
    }
    public void pepeOut()
    {
        if(Pepe == true)
        {
            Pepe = false;
            GameObject.Find("Life Light").GetComponent<RectTransform>().localScale = new Vector3(0, 0, 0);
        }
    }
}
