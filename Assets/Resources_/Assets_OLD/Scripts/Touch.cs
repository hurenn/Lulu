using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Touch : MonoBehaviour
{
    public bool touch = false;//物持ち処理
    public static bool grab = false;//何かしら持ってる合図
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(touch == true && !Input.GetKey(KeyCode.X))
        {
            touch = false;
            grab = false;
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("Player") && Input.GetKey(KeyCode.X))
        {
            touch = true;
            grab = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("Player"))
        {
            touch = false;
            grab = false;
        }
    }
}
