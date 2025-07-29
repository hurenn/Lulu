using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fungus;

public class NextScene : MonoBehaviour
{

    public string LoadScene;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void nextScene()
    {
        
        FadeManager.Instance.LoadScene(LoadScene, 0.15f);
    }
}
