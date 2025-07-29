using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartAnimation : MonoBehaviour
{
    Animator anim;
    public bool start;
    GameObject Hit;
    GameObject Sparcle;
    GameObject EffectUI;
    public GameObject stand;
    Vector3 firstTransform;
    public GameObject BlackUp;
    public GameObject BlackBottom;
    Vector3 upDefault;
    Vector3 bottomDefault;

    public GameObject Boss;
    // Start is called before the first frame update
    void Start()
    {
        EffectUI = GameObject.Find("Effect UI");
        Hit = (GameObject)Resources.Load("UIEffect Hit");
        Sparcle = (GameObject)Resources.Load("UIEffect Sparcle");
        anim = GetComponent<Animator>();

        upDefault = BlackUp.transform.position;
        bottomDefault = BlackBottom.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (start)
        {
            GameManager.Instance.SetCurrentState(GameState.Event);
            start = false;
            Animation();
        }
    }

    void blackON()
    {
        hide();

        BlackUp.transform.position = upDefault + new Vector3(0, 200, 0);
        BlackBottom.transform.position = bottomDefault - new Vector3(0, 200, 0);
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
    public void Animation()
    {
        blackON();

        anim.Play("Start In");
        StartCoroutine("startAnimation");
    }

    public void hide()
    {
        firstTransform = stand.transform.position;
        stand.GetComponent<RectTransform>().position = firstTransform + new Vector3(0, 250, 0);
    }

    public void show()
    {
        iTween.MoveTo(stand, firstTransform, 0.5f);
    }

    IEnumerator startAnimation()
    {
        GameObject.Find("Player").GetComponent<Lulu>().SetStop(true);
        EffectUI = GameObject.Find("Effect UI");

        yield return new WaitForSeconds(0.1f);
        GameObject.Find("Player").GetComponent<WarpControl>().SetBan(true);
        GameObject hitEffect = (GameObject)Instantiate(Hit, transform.position, Quaternion.identity);
        hitEffect.transform.parent = EffectUI.transform;
        EffectUI.GetComponent<Canvas>().sortingOrder = -1;
        SE.playnum = 18;

        yield return new WaitForSeconds(0.4f);
        anim.Play("Start Rotate");
        hitEffect = (GameObject)Instantiate(Sparcle, transform.position, Quaternion.identity);
        hitEffect.transform.parent = EffectUI.transform;
        EffectUI.GetComponent<Canvas>().sortingOrder = 1;
        Destroy(hitEffect, 3f);
        
        yield return new WaitForSeconds(2f);
        anim.Play("Start Out");

        StartCoroutine("blackOFF");

        yield return new WaitForSeconds(0.4f);
        show();
        GameObject.Find("Player").GetComponent<Lulu>().SetStop(false);
        GameObject.Find("Player").GetComponent<WarpControl>().SetBan(false);

        if (Boss)
        {
            Boss.GetComponent<Enemy>().battle = true;
        }

        GameManager.Instance.SetCurrentState(GameState.Playing);
    }
}
