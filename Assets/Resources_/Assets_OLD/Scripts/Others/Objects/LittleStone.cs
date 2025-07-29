using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LittleStone : MonoBehaviour
{
    public bool get = false;
    float timer = 0;
    public float popTime = 7f;
    bool pop = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        PopAnimation();

        DestAndPop();

    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player" && timer == 0)
        {
            GameObject.Instantiate(GameObject.Find("StoneUI").GetComponent<StoneUI>().flash);
            get = true;
        }
    }

    void PopAnimation()
    {
        if (popTime - 1f <= timer)
        {
            if (pop == false)
            {
                transform.localScale = new Vector3(1,1,1);
                pop = true;
            }
            else
            {
                transform.localScale = new Vector3(0, 0, 0);
                pop = false;
            }
        }
    }
    void DestAndPop()
    {

        if (get == true)
        {
            if (timer == 0)
                WarpControl.nowMagic = WarpControl.maxMagic;


            timer += Time.deltaTime;
            transform.localScale = new Vector3(0, 0, 0);
        }


        if (popTime <= timer)
        {
            get = false;
            timer = 0;
            transform.localScale = new Vector3(1, 1,1);
        }
    }
}
