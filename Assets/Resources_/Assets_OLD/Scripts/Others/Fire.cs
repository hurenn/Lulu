using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
    SpriteRenderer rend;
    float timer;
    Animator anim;
    bool fire = false;
    public int EnemyDamage = 400;
    public int PlayerDamage = 100;

    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        timer = 0;
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (rend.isVisible)
            timer += Time.deltaTime;

        if (timer > 6f)
        {
            timer = 0;
        }
        else if (timer > 4f)
        {
            fire = false;
            anim.Play("Fire Out");
        }
        else if (timer > 0.2f)
        {
            anim.Play("Fire Stay");
        }
        else
        {
            fire = true;
            anim.Play("Fire Start");
        }
    }
    void OnTriggerStay2D(Collider2D Col)
    {
        if (Col.gameObject.tag == "Player")
        {
            if (fire == true)
                Needle.PlayerHit(PlayerDamage);
        }
    }

}
