using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynchroColor : MonoBehaviour
{
    SpriteRenderer rend;
    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        rend.color = transform.parent.gameObject.GetComponent<SpriteRenderer>().color;
    }
}
