//using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;
using Fungus;

public class Enemy : MonoBehaviour
{
    public Vector3 efectPos = new Vector3(0, 2f, 0);
    public int gem = 10;
    public Slider slider;
    public GameObject HPGage;
    bool active = true;
    public Vector3 effectOffset;
    Color objectColor;

    Animator anim;
    public int HP = 100;
    bool dead = false;
    public bool Boss;

    Rigidbody2D rb;
    public bool grab = false;
    public bool end = false;
    bool SuperDamage = false;
    GameObject player;

    public float invinceTime = 5;
    [SerializeField]
    bool invince = false;
    float maxInvince = 1.5f;
    bool invinceAnim = false;

    public float EnemyBlastSize = 2;

    GameObject Hit;
    GameObject Explosion;
    GameObject ExplosionLight;
    GameObject BurstGage;
    GameObject EnemyExplosion;
    GameObject Gems;
    GameObject Concentrate;

    GameObject fireBall;

    private GameObject Blast;
    bool engage;
    bool pursuit;
    public bool battle = true;
    public string blockName;
    Quaternion qua;

    bool isGround;
    int groundLayer = 1 << 9; //地面レイヤーマスク

    public float GetMaxInvince()
    {
        return maxInvince;
    }

    // Start is called before the first frame update
    void Start()
    {
        objectColor = GetComponent<SpriteRenderer>().color;
        BurstGage = (GameObject)Resources.Load("Burst ChargeGage");
        Hit = (GameObject)Resources.Load("HitEffect1");
        Explosion = (GameObject)Resources.Load("Explosion_blue");
        ExplosionLight = (GameObject)Resources.Load("Explosion Flash");
        EnemyExplosion = (GameObject)Resources.Load("Enemy Explosion");
        Gems = (GameObject)Resources.Load("GemScatter");
        //slider = GameObject.Find("Slider").GetComponent<Slider>();
        slider.maxValue = HP;
        rb = this.gameObject.GetComponent<Rigidbody2D>();
        player = GameObject.Find("Player");
        anim = GetComponent<Animator>();
    }

    public bool GetIsground()
    {
        return isGround;
    }

    // Update is called once per frame
    void Update()
    {
        //ゲーム用接地判定
        isGround = Physics2D.Linecast(transform.position + transform.up / 2, transform.position, groundLayer);
        Debug.DrawLine(transform.position + transform.up / 2, transform.position, Color.green);

        if (pursuit == true && HP > 0) //追撃中
        {
            if (GameManager.currentGameState == GameState.Pause)
                return;

            if (Input.GetKeyDown(KeyCode.Z))
            {
                SE.playnum = 10;
                SE.playnum = 9;
                var ran = new Random();
                fireBall = (GameObject)Instantiate(Resources.Load("FireBall"), transform.position + new Vector3((float)ran.NextDouble() * 20 - 10.0f, (float)ran.NextDouble() * 10 + 5.0f), Quaternion.identity);
                fireBall.GetComponent<FireBall>().target = gameObject;
            }
        }

        if (!Boss) //雑魚敵の場合
        {
            InvisibleTime();//ボスにも必要なら上へ上げる。

            if (HP == slider.maxValue && active == true)//HPMAXならゲージ非表示
            {
                HPGage.SetActive(false);
                active = false;
            }
            if (HP != slider.maxValue && active == false)//HPが減っているならゲージ表示
            {
                HPGage.SetActive(true);
                active = true;
            }
        }

        slider.value = HP;

        //Debug.Log("over = " + over + ", step = " + step + ", end = " + end + ", throwing = " + throwing);
        if (HP <= 0 && dead == false) //HPが0になったとき
        {
            StartCoroutine("Break"); //死亡処理
        }

    }

    public bool GetDead() //生か死か
    {
        return dead;
    }

    public bool GetDown() //ダウン状態取得
    {
        return SuperDamage;
    }

    IEnumerator SetDown() //ダウン状態
    {
        SuperDamage = true;
        yield return new WaitForSeconds(0.3f);
        SuperDamage = false;
    }

    IEnumerator BurstAttack() //チャージ攻撃
    {
        GameObject.Find("Player").GetComponent<HPManager>().setInvince(true); //主人公無敵化
        FireBall.ShootFlag(false); //チャージ終了

        if(HP > 0)
            StartCoroutine("SetDown"); //ダウン状態処理

        //読み込み・振動
        GameObject burst = (GameObject)Instantiate(BurstGage, transform.position, Quaternion.identity, GameObject.Find("Blue Gage").transform);
        burst.GetComponent<RectTransform>().localPosition = new Vector3(0, -10, 0);
        CameraImpulse.StartImpulse();
        GameObject explosionEffect2;
        if(GameObject.Find("CM Zoom"))
        {
//            GameObject.Find("CM Zoom").GetComponent<CinemachineVirtualCamera>().enabled = true;
        }

        //大爆発
        yield return new WaitForSeconds(0.1f);
        explosionEffect2 = (GameObject)Instantiate(ExplosionLight, transform.position + effectOffset, Quaternion.identity);
        explosionEffect2.transform.localScale *= 0.5f + 200 * 0.01f;
        SE.playnum = 27;
        yield return new WaitForSeconds(0.1f);

        //追撃
        Concentrate = GameObject.Find("Concentrated Line");
        iTween.ScaleTo(Concentrate, iTween.Hash("x", 0.5f, "time", 0.1f));
        GameObject button = (GameObject)Instantiate(Resources.Load("Xbutton"), GameObject.Find("ScreenColor").transform);

        Time.timeScale = 0.2f;
        pursuit = true;
        yield return new WaitForSeconds(0.4f);
        Destroy(button);

        if (fireBall)
        {
            gem = (int)(gem * 1.5f);
            FireBall.ShootFlag(true);
            Time.timeScale = 0.4f;
            pursuit = false;
            SE.playnum = 27;

            //爆発
            yield return new WaitForSeconds(0.005f);

            Time.timeScale = 0.001f;
            yield return new WaitForSeconds(0.001f);

            SE.playnum = 28;//ズドーン
            Time.timeScale = 0.9f;
            yield return new WaitForSeconds(0.2f);
            Blast = Instantiate(EnemyExplosion, transform.position + effectOffset, Quaternion.identity);
            Blast.transform.localScale *= EnemyBlastSize * 1.5f;

            Instantiate(Resources.Load("Flash")); //フラッシュ
        }

        //片づけ
        endPursuit();

        yield return new WaitForSeconds(0.3f);
        GameObject.Find("Player").GetComponent<HPManager>().setInvince(false);
    }

    public void endPursuit()
    {
        pursuit = false;
        Time.timeScale = 1f;
        iTween.ScaleTo(Concentrate, iTween.Hash("x", 0f, "time", 2f));
        if (HP <= 1)
        {
            HP = 0;
        }
    }

    public void SlashEffect(Quaternion set)
    {
        qua = set;
        StartCoroutine("SlashAnimation");
    }

    IEnumerator SlashAnimation()
    {
        GameObject slash = Instantiate((GameObject)Resources.Load("Slash2"), transform.position + efectPos, qua, transform);
        yield return new WaitForSeconds(0.1f);
        GameObject wave = Instantiate((GameObject)Resources.Load("ShockWave"), transform.position + efectPos, Quaternion.identity);
        GameObject spark = Instantiate((GameObject)Resources.Load("Spark"), transform.position + efectPos, Quaternion.identity);
        
        yield return new WaitForSeconds(0.1f);
        anim.speed = 0f;
        slash.GetComponent<Animator>().speed = 0f;
        wave.GetComponent<Animator>().speed = 0f;

        SE.playnum = 13;
        Damage(20, 0);
        GameObject.Find("Player").GetComponent<Lulu>().PowerUp(20);

        yield return new WaitForSeconds(0.1f);
        anim.speed = 1f;
        slash.GetComponent<Animator>().speed = 1f;
        wave.GetComponent<Animator>().speed = 1f;
    }

    public void Damage(int damage, int plus)
    {
        if(!fireBall)
            SE.playnum = 29;
        if (invince)
        {
            return;
        }

        Instantiate(Hit, transform.position + effectOffset, Quaternion.identity);

        //if (plus > 0)
        if (plus >= player.GetComponent<Lulu>().GetReadypower())
        {
            //HP = (int)Mathf.Clamp(HP - (damage + (int)Mathf.Clamp(plus * plus / 100, 0, 1000)), 1, int.MaxValue);
            HP = (int)Mathf.Clamp(HP - damage, 1, int.MaxValue);
            StartCoroutine("BurstAttack");
            CollectCoin.Collected += (int)Mathf.Clamp(plus * plus / 100, 0, 1000);
            player.GetComponent<Lulu>().PowerUp(-300);
            GameObject.Find("Blue Gage").GetComponent<ChargeGage>().ColorChange("yellow");
        }
        else
        {
            HP -= damage;
        }
    }

    public void SetInvince(bool set)
    {
        invince = set;
    }
    public bool GetInvince()
    {
        return invince;
    }
    void InvisibleTime()
    {
        if (invinceTime < maxInvince)//無敵時間
        {
            invinceTime += Time.deltaTime;
            if (invinceAnim == false)
            {
                this.GetComponent<SpriteRenderer>().color = objectColor - new Color(0, 0, 0, 0.7f);
                invinceAnim = true;
            }
            else
            {
                this.GetComponent<SpriteRenderer>().color = objectColor;
                invinceAnim = false;
            }

        }
        else
        {
            if (invinceAnim == true)
            {
                this.GetComponent<SpriteRenderer>().color = objectColor;
                invinceAnim = false;
            }
        }
    }

    IEnumerator Break() //撃破演出
    {
        dead = true;
        if (Boss) //ボスの時
        {
            GameObject.Find("Pause").GetComponent<Pause>().setBan(true); //ポーズ禁止
            GameManager.Instance.SetCurrentState(GameState.Event);
            //カメラをボスに向ける

            GameObject.Find("Player").GetComponent<Lulu>().SetEnd(); //操作完全不能
            GameObject.Find("Player").GetComponent<Rigidbody2D>().gravityScale = 0.1f;
            GameObject.Find("Clear UI").GetComponentInChildren<ClearAnimation>().black = true;
            yield return new WaitForSeconds(0.12f);
            Instantiate(Resources.Load("Flash")); //フラッシュ
            yield return new WaitForSeconds(0.08f);
            Time.timeScale = 0.01f; //スロー
            
            SE.playnum = 20;
            anim.Play("Break");

            yield return new WaitForSeconds(0.025f);

            Time.timeScale = 1f; //スロー解除
            GameObject.Find("Player").GetComponent<Lulu>().SetYplus(0);
            GameObject.Find("Player").GetComponent<Rigidbody2D>().gravityScale = 1f;

            yield return new WaitForSeconds(1f);

            yield return new WaitForSeconds(0.5f);
            
            //クリア演出
            GameObject.Find("Clear UI").GetComponentInChildren<ClearAnimation>().start = true;
            yield return new WaitForSeconds(5f);

            GameManager.Instance.SetCurrentState(GameState.Event);
            Flowchart flowchart = FindObjectOfType<Flowchart>();
            if (!blockName.Equals(null))
            {
                flowchart.ExecuteBlock(blockName);
            }
        }
        else //雑魚敵の時
        {
            if (!engage) //爆発に巻き込まれていないとき
            {
                GetComponent<Rigidbody2D>().gravityScale = 0;
                GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

                anim.Play("Break");
                yield return new WaitForSeconds(0.5f);
            }
            else //爆発に巻き込まれたとき
            {
                GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
            }

            SE.playnum = 27;
            Blast = Instantiate(EnemyExplosion, transform.position + effectOffset, Quaternion.identity);
            Blast.transform.localScale *= EnemyBlastSize;
            CameraImpulse.StartImpulse();
            CollectCoin.Collected += gem;
            Geminstant(gem);
            yield return new WaitForSeconds(2f);
            Destroy(this.gameObject);
        }
    }
    void Geminstant(int size)
    {
        //Debug.Log(size);
        Gems.GetComponent<GemScatter>().size = size;
        Instantiate(Gems, transform.position + transform.up * 2, Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D Collider)
    {/*
        if (Collider.gameObject.name.Equals("Foot") && Player.crushble == true)
        {
            HP -= Player.crushDamage;
            if (HP <= 0)
            {
                Destroy(transform.GetChild(3).gameObject);
            }
            //player.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, 7f), ForceMode2D.Impulse);
            //OnGround.jump = false;
            //rb.AddForce(new Vector2(3f, 5f), ForceMode2D.Impulse);
            invinceTime = 0;
        }
        */

        if (Collider.gameObject.tag == "Atack") //バグったら消す
        {
            if (Collider.gameObject.GetComponent<Weapon>().state == 1)
            {
                HP -= Collider.gameObject.GetComponent<Weapon>().PlayersAtack;
                if (HP <= 0)
                {
                    Destroy(transform.GetChild(3).gameObject);
                }
                //player.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, 7f), ForceMode2D.Impulse);
                //OnGround.jump = false;
                //rb.AddForce(new Vector2(3f, 5f), ForceMode2D.Impulse);
                invinceTime = 0;
            }
        }
        if(Collider.gameObject.tag == "EnemyExplosion" && !Collider.gameObject.Equals(Blast))
        {
            HP -= 100;
            if(HP <= 0)
            {
                gem += Collider.gameObject.GetComponent<ExplosionScore>().score / 2;// + (Collider.gameObject.GetComponent<ExplosionScore>().score / 2);
                engage = true;
                //Debug.Log(gameObject.name + " Engagement");
            }
            //Debug.Log(gameObject.name + " " +  score);
        }
    }
    private void OnCollisionStay2D(UnityEngine.Collision2D Collider)
    {
        if (Collider.gameObject.tag.Equals("Object"))
        {
            rb.linearVelocity = new Vector2(0, 0);
        }
    }
}
