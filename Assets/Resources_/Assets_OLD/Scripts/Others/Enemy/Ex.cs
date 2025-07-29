using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class Ex : MonoBehaviour
{
    Animator anim;
    GameObject player;
    Rigidbody2D rb;
    [SerializeField]
    int BattleFaze = 1;

    Vector2 distance;
    public bool debug;
    bool idle = true;
    float idleTimer = 0;

    GameObject FireAura;
    GameObject BurstPlosion;
    GameObject Gems;

    // Start is called before the first frame update
    void Start()
    {
        FireAura = (GameObject)Resources.Load("Ice Aura");
        BurstPlosion = (GameObject)Resources.Load("Burst");
        Gems = (GameObject)Resources.Load("GemScatter");
        anim = GetComponent<Animator>();
        player = GameObject.Find("Player");
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //やられ状態
        if (GetComponent<Enemy>().GetDown() == true)
        {
            Down();
        }

        //行動不能
        if (GetComponent<Enemy>().HP <= 0)
        {
            return;
        }

        DebugText();

        //行動状態確認
        if (!idle)
        {
            return;
        }

        //待機状態継続
        if (idleTimer < 1f)
        {
            idleTimer += Time.deltaTime;
            return;
        }

        //戦闘モード確認
        if (!GetComponent<Enemy>().battle)
        {
            return;
        }

        //行動開始
        idle = false;

        //強化段階管理
        switch (BattleFaze)
        {
            case 1:
                Faze1();
                break;

            case 2:
                Faze2();
                break;
        }
    }

    void Faze1()
    {
        idleTimer = -2;
        StartCoroutine(Shoot(0));
    }

    void Faze2()
    {
        idleTimer = 0.3f;
        var rand = new Random();
        switch (rand.Next(0, 4))
        {
            case 0:
                StartCoroutine("Burst");
                break;
            case 1:
            case 2:
            case 3:
                StartCoroutine(Shoot(rand.Next(1, 4)));
                break;

        }
    }


    IEnumerator Burst()
    {
        anim.SetTrigger("Burst");
        GameObject FireEffect;
        Vector3 Pos = transform.position;
        var rand = new Random();
        int random = rand.Next(0, 3);

        switch (random)
        {
            case 0:
                Pos -= new Vector3(0, 0);
                break;
            case 1:
                Pos -= new Vector3(27, 0);
                break;
        }

        //予備動作
        SE.playnum = 9;
        FireEffect = (GameObject)Instantiate(FireAura, Pos, Quaternion.identity);
        Destroy(FireEffect, 1f);
        yield return new WaitForSeconds(1f);

        //攻撃動作
        SE.playnum = 28;
        FireEffect = (GameObject)Instantiate(BurstPlosion, Pos, Quaternion.identity);
        Destroy(FireEffect, 0.8f);
        CameraImpulse.StartImpulse(transform.up * 2 + transform.right);
        Geminstant(4, Pos);
        yield return new WaitForSeconds(0.3f);

        anim.SetTrigger("Burst");
        idle = true;
    }

    void RainStart(float plusPos)
    {
        GameObject Slash1 = (GameObject)Instantiate(Resources.Load("slashEnemy1"), transform.position + new Vector3(-14f, 0), Quaternion.identity);
        Slash1.transform.position -= new Vector3(plusPos - 14, 5, 0);
        Slash1.transform.Rotate(new Vector3(0, 0, 180));
        Destroy(Slash1, 0.5f);
    }
    void RainAttack(float plusPos)
    {
        GameObject none = Instantiate(new GameObject(), transform.position, Quaternion.identity, transform);
        GameObject SlashAttack = (GameObject)Instantiate(Resources.Load("slashEnemy2"), transform.position, Quaternion.identity, none.transform);
        SlashAttack.GetComponent<Animator>().Play("slashEnemy_Rain");
        none.transform.position -= new Vector3(plusPos, 0, 0);
        Destroy(none, 2f);
    }

    void SlashStart(Vector2 rotates, bool sound)
    {
        if(sound)
            SE.playnum = 25; //ピーン

        GameObject Slash1 = (GameObject)Instantiate(Resources.Load("slashEnemy1"), transform.position + new Vector3(-14f, 0), Quaternion.identity);
        Slash1.transform.position += new Vector3(0, 3f + rotates[0], 0);
        Slash1.transform.Rotate(new Vector3(0, 0, rotates[1]));
        Destroy(Slash1, 0.5f);
    }
    void SlashAttack()
    {
        SE.playnum = 27; //バーン
        GameObject SlashAttack = (GameObject)Instantiate(Resources.Load("slashEnemy2"), transform.position, Quaternion.identity, transform);
        SlashAttack.transform.GetComponentInChildren<Thunder>().Gems = 10;
        SlashAttack.GetComponent<DamageZone>().PlayerDamage = 51;
        Destroy(SlashAttack, 4f);
    }
    void SlashAttack(int number, bool sound)
    {
        if(sound)
            SE.playnum = 27; //バーン

        GameObject SlashAttack = (GameObject)Instantiate(Resources.Load("slashEnemy2"), transform.position, Quaternion.identity, transform);
        SlashAttack.GetComponent<Animator>().Play("slashEnemy2-" + number);
        Destroy(SlashAttack, 4f);
    }

    IEnumerator Shoot(int random)
    {
        anim.SetTrigger("Shoot");
        yield return new WaitForSeconds(1f);
        anim.SetTrigger("Shoot");
        Vector2[] rotates = new Vector2[5];

        switch (random)
        {
            case 0: //One
                //予備動作
                SlashStart(new Vector2(0, -90), true);
                yield return new WaitForSeconds(0.6f);
                //攻撃
                SlashAttack();
                break;

            case 1: //Same
                rotates[0] = new Vector2(0, -90);
                rotates[1] = new Vector2(3, -100);
                rotates[2] = new Vector2(6, -110);

                SE.playnum = 25;
                for (int i = 0; i <= 2; i++)
                {
                    SlashStart(rotates[i], false);
                }

                yield return new WaitForSeconds(0.4f);

                SE.playnum = 27;
                for (int i = 1; i <= 3; i++)
                {
                    SlashAttack(i, false);
                }
                break;

            case 2: //Rain

                SE.playnum = 25;
                int[] pos = {-1, 6, 13, 20, 27};
                for(int i = 0; i < 5; i++)
                    RainStart(pos[i]);

                yield return new WaitForSeconds(0.35f);

                SE.playnum = 27;
                for (int i = 0; i < 5; i++)
                    RainAttack(pos[i]);

                break;

            case 3: //Row
                rotates[0] = new Vector2(0, -100);
                rotates[1] = new Vector2(3, -110);
                rotates[2] = new Vector2(6, -120);
                rotates[3] = new Vector2(9, -130);
                rotates[4] = new Vector2(18, -140);

                for (int i = 4; i >= 0; i--)
                {
                    SlashStart(rotates[i], true);
                    yield return new WaitForSeconds(0.1f);
                }

                for (int i = 5; i >= 1; i--)
                {
                    SlashAttack(i, true);
                    yield return new WaitForSeconds(0.1f);
                }
                break;
        }
        idle = true;
    }

    void Geminstant(int size, Vector3 Pos)
    {
        GameObject GemScatter;
        Gems.GetComponent<GemScatter>().size = size;
        GemScatter = (GameObject)Instantiate(Gems, Pos, Quaternion.identity, this.transform);
        Destroy(GemScatter, 3f);
        GemScatter.transform.parent = null;
    }

    public void Down()
    {
        GetComponent<Enemy>().SetInvince(false);
        anim.speed = 1;
        idle = true;
        idleTimer = -1f;
        anim.Play("Damage");
        anim.ResetTrigger("Burst");
        anim.ResetTrigger("Shoot");
        StopAllCoroutines();
    }

    void DebugText()
    {
        distance = player.transform.position - transform.position;
        if (debug)
        {
            GameObject.Find("Distance").GetComponent<Text>().text = "距離：" + distance.ToString();
            GameObject.Find("IdleTimer").GetComponent<Text>().text = "待機状態：" + idle + ", タイマー：" + idleTimer.ToString();
            GameObject.Find("Gravity").GetComponent<Text>().text = "重力：" + rb.gravityScale;
            GameObject.Find("AttackFlag").GetComponent<Text>().text = "攻撃状態：" + GetComponent<DamageZone>().Attakable;
            GameObject.Find("InvinceFlag").GetComponent<Text>().text = "無敵状態：" + GetComponent<Enemy>().GetInvince();
        }
    }
}
