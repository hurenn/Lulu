using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Manhole : MonoBehaviour
{
    public GameObject stick;
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
            StartCoroutine("wait");
        }
    }

    IEnumerator wait()
    {
        iTween.ShakeRotation(gameObject, iTween.Hash("z", 1f));
        yield return new WaitForSeconds(0.4f);

        GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-10f, 10f), 3000f));
        SE.playnum = 27;

        CameraImpulse.StartImpulse();
        DOTween.To
            (
                () => stick.transform.localScale,       //何に
                x => stick.transform.localScale = x,  //何を
                new Vector3(1, 1, 1),     //どこまで(最終的な値)
                1f       //どれくらいの時間
            ).SetEase(Ease.OutElastic);

        yield return new WaitForSeconds(0.6f);

        stick.GetComponent<Animator>().enabled = true;
        Destroy(stick.GetComponent<DamageZone>());
        Destroy(gameObject);
    }
}
