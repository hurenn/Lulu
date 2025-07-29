using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;
using UnityEngine.Playables;

public class BlockTrigger : MonoBehaviour
{
    public string blockName;
    Flowchart flowchart;
    float coolTime = 0;

    //SpriteRenderer rend;
    // Start is called before the first frame update
    void Start()
    {
        //rend = transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (coolTime > 0)
        {
            coolTime -= Time.deltaTime;
        }
    }

    void MessageEnd()
    {
        GameManager.Instance.SetCurrentState(GameState.Playing);
        coolTime = 0.3f;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
            MessageStart();
    }
    public void MessageStart()
    {
        flowchart = FindObjectOfType<Flowchart>();
        //GameManager.Instance.SetCurrentState(GameState.Event);
        if (!blockName.Equals(null))
        {
            flowchart.ExecuteBlock(blockName);
        }

        if (GetComponent<PlayableDirector>())
        {
            GetComponent<PlayableDirector>().enabled = true;
        }
    }
    #region 吹き出し奴
    /*
    private void OnTriggerStay2D(Collider2D Collider)
    {
        if (Collider.gameObject.tag.Contains("Player") && GameManager.currentGameState == GameState.Playing && coolTime <= 0)
        {
            rend.color = new Color(1f, 1f, 1f, 1f);
            if (Input.GetKeyDown(KeyCode.UpArrow) && !PlayerInput.right && !PlayerInput.left && !PlayerInput.down && !PlayerInput.z && PlayerInput.cool == false)
            {
                GameManager.Instance.SetCurrentState(GameState.Event);
                if (!blockName.Equals(null))
                {
                    flowchart.ExecuteBlock(blockName);
                }
            }
        }


    }
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.tag.Contains("Player"))
            rend.color = new Color(1f, 1f, 1f, 0f);
    }
    */
    #endregion
}
