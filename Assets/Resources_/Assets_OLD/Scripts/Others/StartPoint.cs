using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPoint : MonoBehaviour
{
    public static Vector3 pos = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        if(!pos.Equals(Vector3.zero))
            GameObject.Find("Player").transform.position = pos;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(pos);
    }
}
