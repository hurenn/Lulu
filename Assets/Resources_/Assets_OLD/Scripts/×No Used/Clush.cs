using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clush : MonoBehaviour {
    
    public string earthTag = "Ground";//除外するタグ
    public float velo = 0;//速度
    public float forcePower = 1;//吹き飛ばす強さ
    float time = 0;
    Vector2 pos;
    Vector2 col;
    Rigidbody2D rb;

	// Use this for initialization
	void Start ()
    {
        rb = this.gameObject.GetComponent<Rigidbody2D>();
        pos = this.gameObject.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        time += Time.deltaTime;
        if(time > 0.2)
        {
            velo = Mathf.Abs(this.gameObject.transform.position.x - pos.x);
            pos = this.gameObject.transform.position;
            time = 0;
        }

    }
    void OnTriggerEnter2D(Collider2D Collider)
    {
        Debug.Log("clush");
        if(Collider.tag == "Player")
        {
            Debug.Log("burst");
            rb = Collider.GetComponent<Rigidbody2D>();
            col = Collider.transform.position;
            Vector2 toVec = -this.gameObject.transform.position + Collider.transform.position;
            rb.AddForce(toVec * velo,ForceMode2D.Impulse);
        }
        if (Collider.tag == earthTag)
            return;

    }
}
