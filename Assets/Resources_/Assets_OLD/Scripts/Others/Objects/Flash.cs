using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Flash : MonoBehaviour
{
    public float startTime = 0.2f, endTime = 0.2f; //白くなる時間、元に戻る時間
    // Start is called before the first frame update
    void Start()
    {
        //シーンを切り替えてもこのゲームオブジェクトを削除しないようにする
        DontDestroyOnLoad(gameObject.transform.parent);
        GetComponent<Image>().color = new Color(1f,1f,1f,0f);
        StartCoroutine("wait");
    }

    IEnumerator wait()
    {
        DOTween.To
        (
            () => GetComponent<Image>().color,       //何に
            x => GetComponent<Image>().color = x,  //何を
            new Color(1f, 1f, 1f, 1f),     //どこまで(最終的な値)
            startTime    //どれくらいの時間
        );

        yield return new WaitForSeconds(0.1f);

        DOTween.To
        (
            () => GetComponent<Image>().color,       //何に
            x => GetComponent<Image>().color = x,  //何を
            new Color(1f, 1f, 1f, 0f),     //どこまで(最終的な値)
            endTime      //どれくらいの時間
        );


        yield return new WaitForSeconds(0.5f);

        Destroy(transform.parent.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
