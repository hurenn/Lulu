using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemybreak_Destroy : MonoBehaviour
{
    public GameObject destroyObject;
    public GameObject generateObject;
    // Start is called before the first frame update
    void Start()
    {

    }

    private void Awake()
    {
        if (destroyObject)
            gameObject.AddOnDestroyCallback(() => eventDestroy());
        if (generateObject)
            gameObject.AddOnDestroyCallback(() => eventGenerate());
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void eventDestroy()
    {
        Instantiate(Resources.Load("Enemy Explosion"), destroyObject.transform.position, Quaternion.identity);
        Destroy(destroyObject);
    }
    private void eventGenerate()
    {
        Instantiate(generateObject, generateObject.transform.position, Quaternion.identity);
    }
}
