using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStart : MonoBehaviour
{
    public GameObject NextStageTitle;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            StartCoroutine("wait");
        }
    }
    IEnumerator wait()
    {
        GetComponent<SelectArrow>().selectUnable();
        Instantiate(Resources.Load("Warp Animation"), transform.position, Quaternion.identity);
        GameReset.GameResetParameter();
        SE.playnum = 32;
        GameObject.Find("GameManager").GetComponent<WhiteFade>().WhiteIn();
        yield return new WaitForSeconds(1f);
        NextStageTitle.SetActive(true);
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(GetComponent<WarpPad>().LoadScene);
    }
}
