using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class IronEye : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            collision.GetComponent<Lulu>().SetStop(true);
            collision.GetComponent<Lulu>().SetXplus(0);
            StartCoroutine("GoBack");
            collision.GetComponent<Lulu>().SetXplus(0);
            collision.GetComponent<Lulu>().SetStop(false);
        }
    }

    IEnumerator GoBack()
    {
        Time.timeScale = 0.9f;

        DOTween.To
        (
            () => GameObject.Find("White").GetComponent<Image>().color,       //何に
            x => GameObject.Find("White").GetComponent<Image>().color = x,  //何を
            new Color(1, 1, 1, 1),     //どこまで(最終的な値)
            0.5f       //どれくらいの時間
        );

        yield return new WaitForSeconds(0.5f);
        GameObject.Find("Player").transform.position = StartPoint.pos;
        yield return new WaitForSeconds(0.5f);

        DOTween.To
        (
            () => GameObject.Find("White").GetComponent<Image>().color,       //何に
            x => GameObject.Find("White").GetComponent<Image>().color = x,  //何を
            new Color(1, 1, 1, 0),     //どこまで(最終的な値)
            0.5f       //どれくらいの時間
        );

        Time.timeScale = 1f;
        yield return new WaitForSeconds(0.3f);
    }
}
