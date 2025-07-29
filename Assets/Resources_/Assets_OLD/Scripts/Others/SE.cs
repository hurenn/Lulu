using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SE : MonoBehaviour {

    AudioSource audioSource;
    public List<AudioClip> audioclip = new List<AudioClip>();
    //AudioClip clip;
    public static int playnum = -1;

	// Use this for initialization
	void Start () {
        audioSource = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
        if(playnum != -1)
        {
            audioSource.PlayOneShot(audioclip[playnum]);
            playnum = -1;
        }
	}
}
