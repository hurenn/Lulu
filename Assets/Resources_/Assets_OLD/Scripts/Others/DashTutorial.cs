using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashTutorial : MonoBehaviour
{
    GameObject player;
    [SerializeField]
    GameObject right2;
    [SerializeField]
    Sprite rightOn;
    Sprite rightOff;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        rightOff = GetComponent<SpriteRenderer>().sprite;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(player.transform.position.x - this.transform.position.x) > 30.0f)
        {
            return;
        }

        //ダッシュ状態で点灯変化
        int ans = player.GetComponent<PlayerInput>().getDash();
        switch(ans)
        {
            case 1:
                GetComponent<SpriteRenderer>().sprite = rightOn;
                break;
            case 2:
                GetComponent<SpriteRenderer>().sprite = rightOn;
                right2.GetComponent<SpriteRenderer>().sprite = rightOn;
                break;
            case 0:
                GetComponent<SpriteRenderer>().sprite = rightOff;
                right2.GetComponent<SpriteRenderer>().sprite = rightOff;
                break;
        }
    }
}
