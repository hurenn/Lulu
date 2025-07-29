//using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraImpulse : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void StartImpulse()
    {
//        GameObject.Find("Main Camera").GetComponent<CinemachineImpulseSource>().GenerateImpulse(new Vector3(0, 1, 0));
    }
    public static void StartImpulse(Vector3 vector3)
    {
//        GameObject.Find("Main Camera").GetComponent<CinemachineImpulseSource>().GenerateImpulse(vector3);
    }
}
