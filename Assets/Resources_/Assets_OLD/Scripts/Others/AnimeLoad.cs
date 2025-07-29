using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MoonSharp.Interpreter;

public class AnimeLoad : MonoBehaviour
{
    GameObject player;
    public float Lange = 20;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (Math.Abs(transform.position.x - player.transform.position.x) < Lange
        && Math.Abs(transform.position.y - player.transform.position.y) < Lange)
        {
            GetComponent<Animator>().enabled = true;
        }
        else
        {
            GetComponent<Animator>().enabled = false;
        }
    }
}
