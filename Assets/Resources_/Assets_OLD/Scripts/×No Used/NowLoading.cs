using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NowLoading : MonoBehaviour
{
    public AsyncOperation async;

    [SerializeField]
    private GameObject LoadUI;

    [SerializeField]
    private Slider slider;

    public static string SceneName = "Stage 1-1";
    public GameObject Particle;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene()
    {
        LoadUI.SetActive(true);
        if(!Particle.Equals(null))
            Particle.SetActive(true);
        async = SceneManager.LoadSceneAsync(SceneName);
        StartCoroutine("Loading");
    }
    public void LoadEvent()
    {
        LoadUI.SetActive(true);
        if (!Particle.Equals(null))
            Particle.SetActive(true);
        async = SceneManager.LoadSceneAsync("EventScene");
        StartCoroutine("Loading");
    }

    IEnumerator Loading()
    {
        while (!async.isDone)
        {
            var progressVal = Mathf.Clamp01(async.progress / 0.9f);
            slider.value = progressVal;
            yield return null;
        }
    }
}
