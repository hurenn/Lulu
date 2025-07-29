using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoor : MonoBehaviour {
    Animator anim;
    BoxCollider2D col;

    // Use this for initialization
    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Equals("Key"))
        {
            StartCoroutine("hoge");
            anim.Play("OpenDoor");
            SE.playnum = 13;
            col.enabled = false;
        }
    }

    IEnumerator hoge()
    {
        yield return new WaitForSeconds(0.1f);
        iTween.ShakePosition(gameObject, iTween.Hash("x",0.2f,"y",0.2f,"time",1f));
        yield return new WaitForSeconds(0.3f);
    }


}
