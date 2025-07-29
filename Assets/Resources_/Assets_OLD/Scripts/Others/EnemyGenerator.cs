using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Fungus;

public class EnemyGenerator : MonoBehaviour
{
    int Enemys = 0;
    int wave = 1;
    bool action;
    string blockName;
    public GameObject wave2;
    public GameObject wave4;

    // Start is called before the first frame update
    void Start()
    {
        blockName = "ステージクリア";
    }

    // Update is called once per frame
    void Update()
    {
        Enemys = GameObject.FindGameObjectsWithTag("Enemy").Length;
        //Debug.Log(Enemys);
        if (Enemys == 0 && action == false)
        {
            action = true;
            switch (wave)
            {
                case 1:
                    StartCoroutine("Butch2");
                    break;
                case 2:
                    wave2.SetActive(true);
                    StartCoroutine("Zero3");
                    break;
                case 3:
                    StartCoroutine("Light2");
                    break;
                case 4:
                    wave4.SetActive(true);
                    StartCoroutine("Butch3");
                    break;
                case 5:
                    StartCoroutine("ButchZero2");
                    break;
                case 6:
                    StartCoroutine("LastWave");
                    break;
            }
        }
    }

    IEnumerator Butch2()
    {
        yield return new WaitForSeconds(3f);
        Instantiate(Resources.Load("wave1"), transform.position, Quaternion.identity);
        yield return new WaitForSeconds(3f);
        wave++;
        action = false;
    }
    IEnumerator Zero3()
    {
        yield return new WaitForSeconds(0.5f);
        Instantiate(Resources.Load("wave2"), transform.position, Quaternion.identity);
        yield return new WaitForSeconds(3f);
        wave++;
        action = false;
    }
    IEnumerator Light2()
    {
        yield return new WaitForSeconds(0.2f);
        Instantiate(Resources.Load("wave3"), transform.position, Quaternion.identity);
        yield return new WaitForSeconds(3f);
        Destroy(GameObject.FindGameObjectWithTag("Trap"), 2.5f);
        wave++;
        action = false;
    }
    IEnumerator Butch3()
    {
        yield return new WaitForSeconds(0.1f);
        Instantiate(Resources.Load("wave4"), transform.position, Quaternion.identity);
        yield return new WaitForSeconds(3f);
        wave++;
        action = false;
    }
    IEnumerator ButchZero2()
    {
        yield return new WaitForSeconds(0.1f);
        Instantiate(Resources.Load("wave5"), transform.position, Quaternion.identity);
        yield return new WaitForSeconds(3f);
        GameObject Rolling = (GameObject)Instantiate(Resources.Load("wave6"), transform.position, Quaternion.identity);
        wave++;
        action = false;
    }
    IEnumerator LastWave()
    {
        GameObject.Find("WarpTarget Left").SetActive(false);
        GameObject.Find("WarpTarget Right").SetActive(false);
        GameObject.Find("Pole Left").GetComponent<DamageZone>().PlayerDamage = 30;
        GameObject.Find("Pole Right").GetComponent<DamageZone>().PlayerDamage = 30;
        yield return new WaitForSeconds(0.7f);
        DOTween.To
               (
                   () => GameObject.Find("Pole Left").transform.position,       //何に
                   x => GameObject.Find("Pole Left").transform.position = x,  //何を
                   new Vector3(50, 5.4f),     //どこまで(最終的な値)
                   5f       //どれくらいの時間
               ).SetEase(Ease.InSine);

        DOTween.To
               (
                   () => GameObject.Find("Pole Right").transform.position,       //何に
                   x => GameObject.Find("Pole Right").transform.position = x,  //何を
                   new Vector3(-50, 5.4f),     //どこまで(最終的な値)
                   5f       //どれくらいの時間
               ).SetEase(Ease.InSine);
        wave++;


        yield return new WaitForSeconds(8f);

        if (Life.nowLife >= 0)
        {
            GameObject.Find("Player").GetComponent<Lulu>().SetEnd();
            yield return new WaitForSeconds(1f);
            GameObject.Find("Clear UI").GetComponentInChildren<ClearAnimation>().start = true;
            yield return new WaitForSeconds(5f);
            Time.timeScale = 1f;

            Flowchart flowchart = FindObjectOfType<Flowchart>();
            GameManager.Instance.SetCurrentState(GameState.Event);
            if (!blockName.Equals(null))
                flowchart.ExecuteBlock(blockName);
        }

    }
}
