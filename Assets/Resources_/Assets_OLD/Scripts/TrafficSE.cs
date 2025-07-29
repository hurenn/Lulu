using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSE : MonoBehaviour
{
    AudioSource audioSource;
    public List<AudioClip> audioclip = new List<AudioClip>();
    // Start is called before the first frame update
    void Start()
    {
    }

    private void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine("wait");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator wait()
    {
        yield return new WaitForSeconds(0.6f);
        audioSource.PlayOneShot(audioclip[0]);
        yield return new WaitForSeconds(0.6f);
        audioSource.PlayOneShot(audioclip[0]);
        yield return new WaitForSeconds(0.6f);
        audioSource.PlayOneShot(audioclip[0]);
        yield return new WaitForSeconds(0.6f);
        audioSource.PlayOneShot(audioclip[1]);
    }
}
