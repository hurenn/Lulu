using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicUp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void magicUp()
    {
        SE.playnum = 31;
        Instantiate(Resources.Load("Flash"));
        Instantiate(Resources.Load("Get Gem"), transform.position, Quaternion.identity);
        WarpControl_Old.maxMagic += WarpControl_Old.warpCost;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("Player"))
        {
            magicUp();
            Destroy(gameObject);
        }
    }
}
