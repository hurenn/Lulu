using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class NextStage : MonoBehaviour
{
    public GameObject FadePanel;
    public string nextStage = "stage1-2";
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    IEnumerator wait()
    {
        DOTween.To
        (
            () => FadePanel.GetComponent<Image>().color,       //何に
            x => FadePanel.GetComponent<Image>().color = x,  //何を
            new Color(1,1,1,1),     //どこまで(最終的な値)
            0.5f       //どれくらいの時間
        );

        MessageList.MessageNow = false;
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(nextStage);
        StartPoint.pos = Vector3.zero;
    }

    public void Next()
    {
            StartCoroutine("wait");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Next();
        }
    }
}
