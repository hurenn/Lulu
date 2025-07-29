using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class GemScatter : MonoBehaviour
{
    public int size = 5;
    public bool autoFollow = true;
    GameObject gem;
    GameObject Scatter;
    // Start is called before the first frame update
    void Start()
    {
        gem = (GameObject)Resources.Load("PowerGem");
        var rand = new Random();
        for (int i = 0; i < size; i++)
        {
            //Debug.Log("gem");
            Scatter = Instantiate(gem, transform.position, Quaternion.identity);
            Destroy(Scatter, 8f);
            Scatter.GetComponent<Coin1>().Scatter(new Vector2((float)rand.NextDouble()-0.5f, (float)rand.NextDouble()-0.5f), transform.position, autoFollow);
        }
        Destroy(gameObject, 1f);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
