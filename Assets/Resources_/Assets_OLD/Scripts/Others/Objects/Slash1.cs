using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slash1 : MonoBehaviour
{
    public float wait = 0.1f;
    float timer = 0;
    bool flag = false;
    GameObject target;
    Quaternion qua;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= wait && flag)
        {
            target.GetComponent<Enemy>().SlashEffect(qua);
            Destroy(gameObject);
        }
        if (timer >= wait && !flag)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer.Equals(16))    //レイヤーネーム「エネミー」
        {
            if (collision.gameObject.GetComponent<Enemy>().HP <= 0)
                return;
            timer = 0;
            flag = true;
            target = collision.gameObject;
            transform.position = target.transform.position;
            transform.Rotate(new Vector3(0, 0, Random.Range(0, 360)));
            qua = transform.rotation;
            GetComponent<SpriteRenderer>().enabled = true;
        }
    }
}
