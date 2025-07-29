using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventWarpTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject.Find("Player").GetComponent<WarpControl>().EventWarp(transform.position);
        GameObject.Find("Player").GetComponent<Lulu>().SetXplus(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
