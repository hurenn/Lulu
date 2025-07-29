using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageStart1 : MonoBehaviour
{
    bool inputFlug = false;
    Animator anim;
    GameObject player;
    bool GameStart = false;
    [SerializeField]
    GameObject BGM;
    [SerializeField]
    GameObject previewInput_Z;

    // Start is called before the first frame update
    void Start()
    {
        GameObject.Find("Pause").GetComponent<Pause>().setBan(true);
        BGM.SetActive(false);
        Time.timeScale = 0.9f;
        player = GameObject.Find("Player");
        player.GetComponent<Lulu>().SetStop(true);
        player.GetComponent<WarpControl>().SetBan(true);
        anim = player.GetComponent<Animator>();
        anim.Play("Fall");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(player.GetComponent<WarpControl>().ban);
        if (GameStart == true)
        {
            //→キー押している間、操作制限解除
            if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
            {
                player.GetComponent<WarpControl>().ZeroBan();

                previewInput_Z.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                player.GetComponent<WarpControl>().SetBan(true);

                previewInput_Z.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.3f);
            }
        }
        else
        {
            player.GetComponent<WarpControl>().SetBan(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameStart == false && other.gameObject.tag == "Player")
        {
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
            GameStart = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            GameObject.Find("Pause").GetComponent<Pause>().setBan(false);
            Time.timeScale = 1f;
            BGM.SetActive(true);
            player.GetComponent<Lulu>().SetStop(false);
            player.GetComponent<WarpControl>().ZeroBan();
            GetComponent<BoxCollider2D>().isTrigger = false;
            Destroy(gameObject);
        }
    }
}
