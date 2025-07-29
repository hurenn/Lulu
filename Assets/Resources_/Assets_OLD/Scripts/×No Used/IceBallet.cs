using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceBallet : MonoBehaviour
{
    public GameObject ballet;
    public int max = 4;
    float timer = 0;

    GameObject[] tagObjects;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {/*
        if (RuruAnime.left)
        {
            ballet.transform.position = new Vector2(transform.position.x - 0.7f, transform.position.y);
            ballet.GetComponent<Ballet>().direct = 0;
            ballet.GetComponent<SpriteRenderer>().flipY = false;
        }
        else
        {
            ballet.transform.position = new Vector2(transform.position.x + 0.7f, transform.position.y);
            ballet.GetComponent<Ballet>().direct = 2;
            ballet.GetComponent<SpriteRenderer>().flipY = true;
        }
        */
        if (GameManager.currentGameState == GameState.Playing)
            Check();

    }

    void Check()
    {
        tagObjects = GameObject.FindGameObjectsWithTag("IceBallet");
        if (tagObjects.Length >= max || timer > 0)
        {
            if(timer <= 0f)
            {
                timer = 1f;
            }
            timer -= Time.deltaTime;
            return;
        }
        if (timer <= 0f && tagObjects.Length < max)
        {
            if (Friends.Nord && !Hang.hanging)
            {
                if (Input.GetKeyUp(KeyCode.X))
                {
                    Instantiate(ballet);
                }
            }
        }
    }
}
