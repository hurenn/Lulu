using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashWarpTutorial : MonoBehaviour
{
    GameObject player;
    [SerializeField]
    GameObject down2;
    [SerializeField]
    Sprite downOn;
    Sprite downOff;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        downOff = GetComponent<SpriteRenderer>().sprite;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(player.transform.position.x - this.transform.position.x) > 30.0f)
        {
            return;
        }

        int ans = player.GetComponent<WarpControl>().DashFlug();
        switch (ans)
        {
            case 1:
                GetComponent<SpriteRenderer>().sprite = downOn;
                break;
            case 2:
                GetComponent<SpriteRenderer>().sprite = downOn;
                down2.GetComponent<SpriteRenderer>().sprite = downOn;
                break;
            case 0:
                GetComponent<SpriteRenderer>().sprite = downOff;
                down2.GetComponent<SpriteRenderer>().sprite = downOff;
                break;
        }
    }
}
