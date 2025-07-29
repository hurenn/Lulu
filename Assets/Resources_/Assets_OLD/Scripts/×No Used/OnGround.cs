using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnGround : MonoBehaviour {

    public static bool jump = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	}

    private void OnTriggerStay2D(Collider2D Collider)
    {
        if(Collider.tag == "Ground" || Collider.tag == "Object" || Collider.tag == "Trap" || Collider.tag == "itemB")
        jump = true;
    }
    private void OnTriggerExit2D(Collider2D Collider)
    {
        if (Collider.tag == "Ground" || Collider.tag == "Object" || Collider.tag == "Trap" || Collider.tag == "itemB")
            jump = false;
    }
}
