using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpTutorial : MonoBehaviour
{
    GameObject player;
    [SerializeField]
    GameObject previewZ;
    [SerializeField]
    Sprite Zon;
    Sprite Zoff;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        Zoff = previewZ.GetComponent<SpriteRenderer>().sprite;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(player.transform.position.x - this.transform.position.x) > 30.0f)
        {
            return;
        }

        //空中にいる時、右矢印を実体化
        if (player.GetComponent<Lulu>().IsGround() == false)
        {
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);

            if (Input.GetKey(KeyCode.RightArrow))
            {
                previewZ.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);

                if (Input.GetKeyDown(KeyCode.Z))
                {
                    previewZ.GetComponent<SpriteRenderer>().sprite = Zon;
                }
                if (Input.GetKeyUp(KeyCode.Z))
                {
                    previewZ.GetComponent<SpriteRenderer>().sprite = Zoff;
                }
            }
            else
            {
                previewZ.GetComponent<SpriteRenderer>().sprite = Zoff;
                previewZ.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.3f);
            }
        }
        else
        {
            GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.3f);
            previewZ.GetComponent<SpriteRenderer>().sprite = Zoff;
            previewZ.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.3f);
        }
    }
}
