using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{
    int filenumber = 0;
    public bool ready;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (ready == true)
            {
                ScreenCapture.CaptureScreenshot(filenumber + ".png");
                filenumber++;
            }
        }
    }
}