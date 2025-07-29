using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cheat : MonoBehaviour
{
    GameObject Player;
    public bool ScorePlus100 = false;
    public bool MaxHP = false;
    public bool MaxMP = false;
    public bool Reset = false;
    public bool Damage = false;

    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (ScorePlus100 == true)
        {
            CollectCoin.Collected += 100;
            ScorePlus100 = false;
        }
        if (Reset)
        {
            Scene loadScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(loadScene.name);
        }
        if (Damage)
        {
            WarpControl.nowMagic = 0;
            Player.GetComponent<HPManager>().Damage(10);
        }
    }

    public void Max()
    {
        if (MaxHP == false)
        {
            MaxHP = true;
            MaxMP = true;
        }
        else
        {
            MaxHP = false;
            MaxMP = false;
        }
    }
}
