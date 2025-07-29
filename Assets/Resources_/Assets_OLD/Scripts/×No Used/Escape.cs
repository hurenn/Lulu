using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Escape : MonoBehaviour
{
    public static bool start = false;
    public static bool move = false;
    public static bool end = false;
    bool inWall = false;
    GameObject player;
    int step = 0;
    bool left;
    Vector2 goal;
    Vector2 Default;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        Default = new Vector2(transform.position.x - player.transform.position.x, transform.position.y - player.transform.position.y);
    }

    // Update is called once per frame
    void Update()
    {
        if (start == true && end == false && move == false)
        {
            step = 0;
            transform.position = new Vector2(player.transform.position.x + Default.x, player.transform.position.y + Default.y);
            if (RuruAnime.left == true)
            {
                left = true;
                goal = new Vector2(transform.position.x + 1f, transform.position.y + 1f);
            }
            else
            {
                left = false;
                goal = new Vector2(transform.position.x - 1f, transform.position.y + 1f);
            }
            move = true;
            start = false;
        }

        if (move == true)
        {
            switch (step)
            {
                case 0:
                    if (left == true)
                    {
                        if (inWall == false && transform.position.x < goal.x)
                            transform.position = new Vector2(transform.position.x + 0.2f, transform.position.y);
                        else
                        {
                            transform.position = new Vector2(transform.position.x - 0.2f, transform.position.y);
                            step++;
                        }
                    }
                    else
                    {
                        if (inWall == false && goal.x < transform.position.x)
                            transform.position = new Vector2(transform.position.x - 0.2f, transform.position.y);
                        else
                        {
                            transform.position = new Vector2(transform.position.x + 0.2f, transform.position.y);
                            step++;
                        }
                    }
                    break;

                case 1:
                    if (inWall == false && transform.position.y < goal.y)
                        transform.position = new Vector2(transform.position.x, transform.position.y + 0.2f);
                    else
                    {
                        transform.position = new Vector2(transform.position.x, transform.position.y - 0.2f);
                        move = false;
                        end = true;
                    }
                    break;
            }


        }
    }
    private void OnTriggerEnter2D(Collider2D Collider)
    {
        if (Collider.tag == "Ground" || Collider.tag == "Enemy" || Collider.tag == "itemB")
            inWall = true;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Ground" || collision.tag == "Enemy" || collision.tag == "itemB")
            inWall = false;
    }
}
