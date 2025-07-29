using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetCheck : MonoBehaviour
{
    public bool lockOn = false;
    public Vector2 target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if(col.gameObject.layer.Equals(16))
        {
            lockOn = true;
            target = col.gameObject.GetComponent<Transform>().position;
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.layer.Equals(16))
        {
            lockOn = false;
        }
    }
}
