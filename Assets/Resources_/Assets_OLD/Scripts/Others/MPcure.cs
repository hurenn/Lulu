using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPcure : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Cure()
    {
        SE.playnum = 31;
        Instantiate(Resources.Load("Flash"));
        Instantiate(Resources.Load("Get Gem"), transform.position, Quaternion.identity);
        GameObject.Find("Player").GetComponent<Lulu>().PowerUp(300);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("Player"))
        {
            Cure();
        }
    }
}
