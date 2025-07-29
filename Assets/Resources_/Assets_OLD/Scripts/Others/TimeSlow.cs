using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSlow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void slowStart()
    {
        Time.timeScale = 0.2f;
    }
    public void slowEnd()
    {
        Time.timeScale = 1f;
    }
}
