using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Batch : MonoBehaviour
{
    public float yField = 2;
    public float xField = 10;
    public float ySpeed = 5;
    public float xSpeed = 1;
    Vector2 defaultPos;
    public GameObject parent;
    int timer = 0;

    // Start is called before the first frame update
    void Start()
    {
        defaultPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (parent)
        {
            if (timer > 10)
            {
                iTween.MoveTo(gameObject, parent.transform.position, 0.6f);
                timer = 0;
            }
            else
            {
                timer++;
            }
        }
        else
        {
            if (GetComponent<Enemy>().HP > 0)
                transform.position = new Vector2((float)System.Math.Cos(Time.time * xSpeed) * xField + defaultPos.x, (float)System.Math.Cos(Time.time * ySpeed) * yField + defaultPos.y);
        }
    }

}
