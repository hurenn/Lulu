using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour {
    Rigidbody2D rb;
    private float timer = 0;
    public float movelong = 10.0f;
    public float power = 10f;
    bool ud = true;
    bool start = false;
    Vector2 force;
	// Use this for initialization
	void Start () {
            rb = this.GetComponent<Rigidbody2D>();
            force = new Vector2(power, 0f);
	}
	
	// Update is called once per frame
	void Update ()
    {
        rb.AddForce(force, ForceMode2D.Force);
        timer += Time.deltaTime;
        if(timer > movelong)
        {
            if (ud == true)
            {
                force = new Vector2(-power, 0f);
                ud = false;
            }
            else if(ud == false)
            {
                force = new Vector2(power, 0f);
                ud = true;
            }
            timer = 0;
        }
    }
}
