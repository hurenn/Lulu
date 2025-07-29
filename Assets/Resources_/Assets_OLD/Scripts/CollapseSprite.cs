using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollapseSprite : MonoBehaviour
{
    public GameObject target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            target.GetComponent<Rigidbody2D>().AddForce(new Vector2(100, 100), ForceMode2D.Impulse);
            StartCoroutine("wait");
        }
    }
    IEnumerator wait()
    {
        yield return new WaitForSeconds(0.1f);
            target.GetComponent<Explodable>().explode();
            Destroy(target, 2f);
    }
}
