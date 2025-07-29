using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClearAnimation : MonoBehaviour
{
    Animator anim;
    public bool start;
    public bool reset;
    public bool black;
    GameObject Hit;
    GameObject Star;
    GameObject EffectUI;
    public GameObject stand;
    Vector3 firstTransform;
    
    public GameObject BlackUp;
    public GameObject BlackBottom;
    Vector3 upDefault;
    Vector3 bottomDefault;
    // Start is called before the first frame update
    void Start()
    {
        Hit = (GameObject)Resources.Load("UIEffect Light");
        Star = (GameObject)Resources.Load("UIEffect Star");
        EffectUI = GameObject.Find("Effect UI");
        anim = GetComponent<Animator>();
        firstTransform = stand.transform.position;
        upDefault = BlackUp.transform.position;
        bottomDefault = BlackBottom.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (start)
        {
            GameManager.Instance.SetCurrentState(GameState.Event);
            Animation();
            start = false;
        }
        if (reset)
        {
            PositionReset();
            reset = false;
        }
        if (black)
        {
            black = false;
            GameObject.Find("BGM").GetComponent<AudioSource>().volume = 0.1f;

            BlackUp.transform.position = upDefault + new Vector3(0, 200, 0);
            BlackBottom.transform.position = bottomDefault - new Vector3(0, 200, 0);
            blackON();
        }
    }

    void PositionReset()
    {
        stand.transform.position = firstTransform;
    }

    public void Animation()
    {
        StartCoroutine("clearAnimation");
    }

    public void hide() //HPゲージ隠し
    {
        iTween.MoveTo(stand, firstTransform + new Vector3(0, 200, 0), 0.5f);
    }
    
    void blackON()
    {
        hide();

        BlackUp.SetActive(true);
        BlackBottom.SetActive(true);

        iTween.MoveTo(BlackUp, upDefault, 0.5f);
        iTween.MoveTo(BlackBottom, bottomDefault, 0.5f);
    }
    IEnumerator blackOFF()
    {
        iTween.MoveTo(BlackUp, upDefault + new Vector3(0, 200, 0), 0.5f);
        iTween.MoveTo(BlackBottom, bottomDefault - new Vector3(0, 200, 0), 0.5f);

        yield return new WaitForSeconds(1f);
        BlackUp.SetActive(false);
        BlackBottom.SetActive(false);
    }

    IEnumerator clearAnimation()
    {
        GameObject.Find("BGM").GetComponent<AudioSource>().volume = 0.1f;
        blackON();

        yield return new WaitForSeconds(0.5f);
        anim.Play("Clear In");
        SE.playnum = 19;

        GameObject hitEffect = Instantiate(Star, transform.position, Quaternion.identity);
        hitEffect.transform.parent = EffectUI.transform;
        Destroy(hitEffect, 3f);

        GameObject lightEffect = Instantiate(Hit, transform.position, Quaternion.identity);
        lightEffect.transform.parent = EffectUI.transform;
        EffectUI.GetComponent<Canvas>().sortingOrder = 1;
        Destroy(lightEffect, 3f);

        yield return new WaitForSeconds(3f);
        anim.Play("Clear Rotate");

        yield return new WaitForSeconds(0.4f);
        anim.Play("Clear Out");

        StartCoroutine("blackOFF");
    }
}
