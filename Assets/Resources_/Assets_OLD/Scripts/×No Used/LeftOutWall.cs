using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftOutWall : MonoBehaviour
{

    public GameObject FallShade;
    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag.Equals("Player"))
        {
            GameObject shade = Instantiate(FallShade, this.gameObject.transform.position, this.gameObject.transform.rotation) as GameObject;
            GameObject.Find("Player").transform.position = GameObject.Find("WarpReflect Right").transform.position;
        }
    }
}
