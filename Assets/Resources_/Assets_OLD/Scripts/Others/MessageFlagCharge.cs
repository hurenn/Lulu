using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageFlagCharge : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Life.over)
            return;
        if (GameObject.Find("Player").GetComponent<Lulu>().GetPluspower() >= GameObject.Find("Player").GetComponent<Lulu>().GetReadypower())
            GetComponent<BoxCollider2D>().enabled = true;
    }
}
