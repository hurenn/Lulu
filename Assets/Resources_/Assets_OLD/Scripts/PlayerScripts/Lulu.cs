//using Cinemachine;
using System.Collections;
using UnityEngine;

public class Lulu : MonoBehaviour
{
    //主人公を操作・動作させるためのスクリプト

    float x = 0;//操作移動
    //慣性移動
    private float xPlus = 0;
    private float yPlus = 0;

    public float warpMovePower = 5; //ワープ慣性移動力
    public float speed = 4f;    //移動速度
    public float jumpPower = 700;   //ジャンプ力
    public float downPower = 360;   //ジャンプキャンセル力

    private int stop;               //操作不可フラグ
    private float stopTimer = 0;            //操作不可タイマー

    private bool isGround;          //接地判定
    private bool isGroundFlag;      //アニメーション用接地判定
    public bool ExitisGround = false;   //強制離地判定
    public int atackPower = 10;         //素の攻撃力
    private int plusPower = 0;          //チャージ
    private readonly int readyPower = 200;       //チャージ充填ライン
    private bool smokeFlag = false;     //着地アニメーションフラグ
    public bool avoidance = false;      //回避中透明化

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer rend;
    GameObject smoke;       //着地アニメーション読み込み
    GameObject generate;    //着地アニメーション発生

    int groundLayer = 1 << 9; //地面レイヤーマスク
    //壁衝突判定
    private bool rightWall1;
    private bool leftWall1;

    //チャージ設定
    public void PowerUp(int power)
    {
        if (!Friends.Marlica)
            return;

        //ゲージ色管理
        if (power >= 0)
            GameObject.Find("Blue Gage").GetComponent<ChargeGage>().ColorChange("blue");
        else
            GameObject.Find("Blue Gage").GetComponent<ChargeGage>().ColorChange("red");

        //実際のチャージ処理
        plusPower = Mathf.Clamp(plusPower + power, 0, 300);
    }
    
    //現チャージ取得
    public int GetPluspower()   
    {
        return plusPower;
    }

    //チャージ充填ライン取得
    public int GetReadypower()  
    {
        return readyPower;
    }
    
    //接地判定取得
    public bool IsGround()  
    {
        return isGround;
    }
    
    //操作不可フラグセット
    public void SetStop(bool b) 
    {
        if (b == true)
            stop++;
        else
            stop--;
    }

    //慣性移動セットx
    public void SetXplus(int set)   
    {
        xPlus = set;
    }
    
    //慣性移動セットy
    public void SetYplus(int set)   
    {
        yPlus = set;
    }
    
     //移動不可リセット
     //Fungusイベントの一つに使用中
    public void zeroStop() 
    {
        stop = 0;
    }

    //着地アニメーション発生
    public void GenerateSmoke() 
    {
        generate = (GameObject)Instantiate(smoke, transform.position, Quaternion.identity);
    }
    
    //着地アニメーション発生（大きさ変更）
    public void GenerateSmoke(float set)    
    {
        generate = (GameObject)Instantiate(smoke, transform.position, Quaternion.identity);
        generate.transform.localScale *= set;
    }   
    
    //操作不可フラグ取得
    public bool GetStop()
    {
        if (stop > 0)
            return true;
        else
            return false;
    }
    
    //操作不可フラグカウント取得
    public int GetStopCounter() 
    {
        return stop;
    }

    //主人公完全停止
    public void SetEnd()
    {
        GetComponent<HPManager>().setInvince(true);
        xPlus = 0;
        yPlus = 0;
        GetComponent<WarpControl>().SetBan(true);
        stop = 100;
    }

    //強制離地セット
    public void SetExitisGround(bool set)
    {
        ExitisGround = set;
    }

    void Start()
    {
        //読み込み
        rend = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        smoke = (GameObject)Resources.Load("Smoke");
    }

    public void Update()
    {
        //アニメーション指定
        AnimationControl();

        //デバッグコマンド
        ChargeDebug();

        //y方向への慣性移動処理
        WarpJump();

        //操作可能状態判定
        PlayableTimer();

        //着地・接地関係
        Ground();

        //ジャンプ
        Jump();

        //左右移動
        Move();
    }

    void PlayableTimer()
    {
        if (stopTimer > 0)  //操作不可 タイマー
        {
            stopTimer -= Time.deltaTime;
        }
        else if (stopTimer < 0) //操作不可解除 タイマー
        {
            stop--;
            stopTimer = 0;
        }
    }

    void Ground()
    {
        if (ExitisGround)   //強制離地
        {
            isGround = false;
            isGroundFlag = false;
        }
        else
        {
            //ゲーム用接地判定
            isGround = Physics2D.Linecast(transform.position + transform.up / 2, transform.position, groundLayer);
            Debug.DrawLine(transform.position + transform.up / 2, transform.position, Color.green);

            //エフェクト用接地判定 少し長め
            isGroundFlag = Physics2D.Linecast(transform.position + transform.up / 2, transform.position - transform.up / 3, groundLayer);
            Debug.DrawLine(transform.position + transform.up / 2, transform.position - transform.up / 3, Color.blue);
        }

        if (smokeFlag != isGroundFlag)   //着地でエフェクト
        {
            if (smokeFlag == false) //着地した瞬間
                GenerateSmoke();  //煙発生

            smokeFlag = isGroundFlag; //無限ループ防止
        }

        anim.SetBool("IsGround", isGroundFlag); //接地フラグ
        if (anim.GetBool("Attack"))
            anim.SetBool("IsGround", false);    //攻撃直後は接地判定なし
    }

    void Jump()
    {
        //ジャンプ判定
        if (GameManager.currentGameState == GameState.Playing && stop <= 0 //行動可能状態
            && Input.GetKeyDown(KeyCode.Z) && isGround)//地上でZキー
        {
            anim.SetBool("Jump", true);
            anim.Play("Jump");
            isGround = false;

            if (!GetComponent<PlayerInput>().GetDash())
                rb.AddForce(Vector2.up * jumpPower);    //ジャンプ実行
            else
                rb.AddForce(Vector2.up * jumpPower * 1.1f);  //ダッシュジャンプ(高め)
        }
        //上昇中 or 落下中 判定
        float velY = rb.linearVelocity.y;
        bool isJumping = velY > 0.1f ? true : false;
        bool isFalling = velY < -0.1f ? true : false;

        //状態に応じたフラグ管理
        anim.SetBool("Jump", isJumping);
        anim.SetBool("Fall", isFalling);

        //ジャンプキャンセル(上昇相殺)
        if (anim.GetBool("Jump") && velY > 1.5f //ジャンプ中
            && Input.GetKeyUp(KeyCode.Z))  //Zを離した
        {
            rb.AddForce(Vector2.down * downPower);
        }
    }

    void Move()
    {
        Vector2 start = transform.position + transform.right * 0.6f + transform.up * 3.8f;
        Vector2 end = transform.position + transform.right * 0.6f + transform.up * 0.4f;
        //壁衝突判定 右
        rightWall1 = Physics2D.Linecast(start, end, groundLayer);
        Debug.DrawLine(start, end, Color.green);

        start = transform.position - transform.right * 0.6f + transform.up * 3.8f;
        end = transform.position - transform.right * 0.6f + transform.up * 0.4f;
        //壁衝突判定 左
        leftWall1 = Physics2D.Linecast(start, end, groundLayer);
        Debug.DrawLine(start, end, Color.green);

        x = 0;  //操作移動力リセット

        //移動入力
        if (stop <= 0 && GetComponent<WarpControl>().GetTimer() == -1)
        {
            //慣性移動力
            if (xPlus > 0 || xPlus < 0)
            {
                xPlus /= 3f;
            }
            if (xPlus <= 0.2f && xPlus >= -0.2f)
            {
                xPlus = 0;
            }

            //操作移動力
            if (GameManager.currentGameState == GameState.Playing)
                x = Input.GetAxisRaw("Horizontal");

            //壁衝突による移動力リセット
            if (rightWall1 && x > 0)
            {
                x = 0;
                xPlus = 0;
            }
            if (leftWall1 && x < 0)
            {
                x = 0;
                xPlus = 0;
            }
        }

        if (Life.nowLife > 0)
        {
            //左右移動実行
            if (!GetComponent<PlayerInput>().GetDash())
            {
                rb.linearVelocity = new Vector2(x * speed + xPlus * 1.5f, rb.linearVelocity.y); //細かな移動が出来るように、加速を直接変更
            }
            else
            {
                rb.linearVelocity = new Vector2(x * speed * 2.0f + xPlus * 1.5f, rb.linearVelocity.y); //ダッシュ移動
            }
        }
    }

    void AnimationControl()
    {
        if (GetComponent<HPManager>().GetInvince())
        {
            //緊急回避透明化
            if (avoidance)
                rend.color = new Color(1f, 1f, 1f, 0.1f);
        }
        else
        {
            //透明解除
            rend.color = new Color(1f, 1f, 1f, 1f);
        }

        if (x != 0) //移動中
        {
            //振り向き
            bool flip = x < 0 ? false : true;
            GetComponent<SpriteRenderer>().flipX = flip;

            //移動アニメーション
            if (GetComponent<PlayerInput>().GetDash())
                anim.SetBool("Dash", true);
            else
                anim.SetBool("Walk", true);
        }
        else  //停止中
        {
            //移動アニメーション停止
            anim.SetBool("Walk", false);
            anim.SetBool("Dash", false);
        }
    }
 
    public void WarpJump()
    {
        if (yPlus != 0)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, yPlus);
    }
 
    //ワープ後の慣性移動力設定
    public void WarpMove(bool dash)
    {
        //ワープの瞬間のキー入力によって慣性方向を変える
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                xPlus = 5;
                yPlus = 15;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                xPlus = 20;
                yPlus = -10;
            }
            else
            {
                xPlus = 15;
                yPlus = 1;
            }
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                xPlus = -5;
                yPlus = 15;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                xPlus = -20;
                yPlus = -10;
            }
            else
            {
                xPlus = -15;
                yPlus = 1;
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            xPlus = 0;
            yPlus = -25;
        }
        else
        {
            xPlus = 0;
            yPlus = 10;
        }
        //Debug.Log("move" + xPlus + " " + yPlus);
    }

    //左上に向けた慣性移動
    void JumptoLeft()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); 
        xPlus = -3;
        yPlus = 14;
    }

    //右上に向けた慣性移動
    void JumptoRight()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        xPlus = 3;
        yPlus = 14;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        //攻撃・ダメージ判定
        if (collision.gameObject.layer.Equals(16)       //敵にぶつかった
            && !collision.gameObject.GetComponent<Enemy>().GetDead()  //敵が生きてる
            && !Life.over)  //自分も生きてる
        {
            //ダッシュ中なら攻撃
            if (GetComponent<PlayerInput>().GetDash())
            {
                EnemyAttack(collision.gameObject);
            }
            else
            {
                if (!collision.gameObject.GetComponent<DamageZone>())
                    return;
                //ダメージ
                GetComponent<HPManager>().Damage(collision.gameObject.GetComponent<DamageZone>().PlayerDamage);
            }
        }
    }
    
    //ダメージアニメーション
    public void DamageAnimation()
    {
        anim.Play("Damage to Land");
        SetStop(true);
        StartCoroutine("Knockback");
    }
     //ダメージノックバック
    IEnumerator Knockback()
    {
        rb.AddForce(Vector2.up * jumpPower / 2);

        if (GetComponent<SpriteRenderer>().flipX)
            xPlus = -3;
        else
            xPlus = 3;

        yield return new WaitForSeconds(0.5f);
        SetStop(false);
    }
  
    //攻撃処理
    public void EnemyAttack(GameObject enemy) 
    {
        enemy.GetComponent<Enemy>().Damage(atackPower, plusPower);

        StartCoroutine("AttackStop");
    }
    //攻撃中 操作禁止
    IEnumerator AttackStop()
    {
        //操作禁止 ダッシュ解除 無敵
        SetStop(true);
        GetComponent<WarpControl>().SetBan(true);
        GetComponent<PlayerInput>().SetDash(false);
        GetComponent<HPManager>().setInvince(true);

        //攻撃アニメーション
        anim.SetTrigger("Attack");
        anim.Play("Attack");
        if (Input.GetKey(KeyCode.RightArrow))
        {
            JumptoLeft();
        }
        else
        {
            JumptoRight();
        }
        yield return new WaitForSeconds(0.4f);
 
        //操作状態初期化
        SetStop(false);
        GetComponent<WarpControl>().SetBan(false);
        yPlus = 0;
        anim.ResetTrigger("Attack");
        yield return new WaitForSeconds(0.3f); 
        GetComponent<HPManager>().setInvince(false);

        //追撃片づけ
        Time.timeScale = 1f;
        //if (GameObject.Find("CM Zoom"))
        //    GameObject.Find("CM Zoom").GetComponent<CinemachineVirtualCamera>().enabled = false;
    }

    void ChargeDebug()
    {
        //デバッグコマンド  Yを押しながら数字を押してチャージ
        if (Input.GetKey(KeyCode.Y))
        {
            CollectCoin.Collected = 0;
            if (Input.GetKeyDown(KeyCode.Alpha5))
                PowerUp(-50);
            if (Input.GetKeyDown(KeyCode.Alpha6))
                PowerUp(50);
            if (Input.GetKeyDown(KeyCode.Alpha7))
                PowerUp(300);
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                SE.playnum = 31;
                Instantiate(Resources.Load("Flash"));
                Instantiate(Resources.Load("Get Gem"), transform.position, Quaternion.identity);
                WarpControl.maxMagic += 20;
            }
        }
    }

}
