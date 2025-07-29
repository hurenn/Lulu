using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageLength : MonoBehaviour
{
    float startX;
    float goalX;
    Slider slider;
    GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        slider = GetComponent<Slider>();
        startX = player.transform.position.x;
        goalX = GameObject.Find("WarpPad").transform.position.x;
    }

    // Update is called once per frame
    void Update()
    {/*
        if (slider.value < (player.transform.position.x - startX) / (goalX - startX))
        {
            slider.value = (player.transform.position.x - startX) / (goalX - startX);
        }
     */

        slider.value = player.GetComponent<Lulu>().GetPluspower();
    }
}
