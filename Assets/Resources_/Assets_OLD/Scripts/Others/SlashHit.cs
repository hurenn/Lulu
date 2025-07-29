using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashHit : MonoBehaviour
{
    GameObject Slash;
    float attackSwitch = 1.5f;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (attackSwitch > 0)
        {
            attackSwitch = Mathf.Clamp(attackSwitch - Time.deltaTime, 0, 2);
        }
        if (attackSwitch == 0)
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer.Equals(16))    //レイヤーネーム「エネミー」
        {
            attackSwitch = -1;
            StartCoroutine("Attack");
        }
    }

    IEnumerator Attack()
    {
        yield return new WaitForSeconds(0.15f);
        Instantiate((GameObject)Resources.Load("slashBack"), transform.position, Quaternion.identity, this.transform);
        yield return new WaitForSeconds(0.5f);
        GetComponent<DamageZone>().Stay = false;
        Destroy(gameObject,0.5f);
    }
}
