using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Impulse : MonoBehaviour{
    Rigidbody2D rb;
    public float power = 5.0f;
	// Use this for initialization
	void Start () {
        rb = this.GetComponent<Rigidbody2D>();
        Vector2 force = new Vector2(power, power * 2);
        rb.AddForce (force, ForceMode2D.Impulse);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
