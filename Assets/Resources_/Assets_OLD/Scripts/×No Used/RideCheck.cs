using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RideCheck : MonoBehaviour
{
    public bool inGround = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (inGround == false)
            GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 0.5f, 0.5f);
        else
            GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0f, 0f, 0.5f);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag.Equals("Ground") || collision.tag.Equals("itemB") || collision.tag.Equals("Enemy") || collision.tag.Equals("Trap") || collision.tag.Equals("Broken"))
        {
            inGround = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag.Equals("Ground") || collision.tag.Equals("itemB") || collision.tag.Equals("Enemy") || collision.tag.Equals("Trap") || collision.tag.Equals("Broken"))
        {
            inGround = false;
        }
    }
}
