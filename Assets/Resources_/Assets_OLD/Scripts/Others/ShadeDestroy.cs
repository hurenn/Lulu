using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadeDestroy : MonoBehaviour
{
    private float time = 0f;
    public float cleartime = 0.5f;
    bool point = true;
    public GameObject generate;
    // Use this for initialization
    void Start()
    {
        time = 0f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        time += Time.deltaTime;
        if (time > cleartime)
        {
            if (generate)
            {
                Instantiate(generate, transform.position, Quaternion.identity);
            }
            Destroy(this.gameObject);
        }


    }
}
