using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WarpControl : MonoBehaviour
{
    //ワープ機能を管理するスクリプト

    private bool privateInvinceFlag;    //無敵フラグ

    bool dashWarp = false;      //ワープ方法判定
    Rigidbody2D rb;
    private Animator anim;
    float timer = -1;   //ワープ演出タイマー
    public bool cool = false;    //クールタイムフラグ
    public int ban = 0; //ワープ不可フラグ

    float intervalTimer = 0f;       //MP回復開始タイマー
    float warpInterval = 0.15f;     //通常回復
    float recoverInterval = 0.7f;   //ダメージ回復

    //MPコスト
    public static int dashCost = 10;
    public static int warpCost = 15;

    public static float nowMagic = 200; //MP残量
    public static float maxMagic = 200; //MP最大値
    public static float defaultMagic = 200; //MP初期値
    public GameObject MPGage;
    public GameObject underMPGage;
    bool active = true;                     //ゲージアクティブ
    public static bool overHeat = false;    //オーバーヒートフラグ

    Vector3 warpTarget; //ターゲット
    Vector3 errorTarget; //エラー用ターゲット（現在地）

    Vector2 pos;
    GameObject warpEffect;
    GameObject emergencyEffect;
    RaycastHit2D hit;
    RaycastHit2D hit1;
    RaycastHit2D hit2;
    RaycastHit2D hit3;
    bool exitKey;
    int warpMask;

    bool enemyAttackFlag;   //攻撃フラグ

    //ダッシュフラグ
    float Rightdashtimer = 0;   int Rightdash = 0;
    float Leftdashtimer = 0;    int Leftdash = 0;
    float Updashtimer = 0;      int Updash = 0;
    float Downdashtimer = 0;    int Downdash = 0;
    bool effectSwitch;
    bool stopFlag;

    //デバッグフラグ
    [SerializeField]
    bool UnlimitedMP = false;
    [SerializeField]
    bool counterDebug;
    [SerializeField]
    bool DebugCool;
    [SerializeField]
    bool DebugAttack;
    [SerializeField]
    bool targetRayCheck = false;

    //レイヤーマスク
    int groundMask = 1 << 9 | 1 << 17 | 1 << 20;
    int EnemyMask = 1 << 16;
    int coinMask = 1 << 12 | 1 << 16;
    int targetMask = 1 << 18 | 1 << 12;

    GameObject Concentrate;
    Lulu lulu;

    void Start()
    {
        //読み込み
        warpEffect = (GameObject)Resources.Load("Warp Animation");
        emergencyEffect = (GameObject)Resources.Load("Emergency Animation");
        lulu = GetComponent<Lulu>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        Concentrate = GameObject.Find("Concentrated Line");
    }

    //ダッシュ状態管理
    public int DashFlug()
    {
        if (dashWarp)
        {
            return 2;
        }
        else if (Rightdash == 1 || Leftdash == 1 || Updash == 1 || Downdash == 1)
        {
            return 1;
        }
        else
            return 0;
    }

   //ワープ不可セット
    public void SetBan(bool set) 
    {
        if (set)
            ban++;
        else
            ban--;
    }
    //不可リセット
    public void ZeroBan()
    {
        ban = 0;
    }

    //MP上限初期化
    public static void ResetMax()
    {
        maxMagic = defaultMagic;
    }

    //タイマーセット
    public void SetTimer(float time)
    {
        timer = time;
    }
    public float GetTimer() //タイマー取得
    {
        return timer;
    }

    public void SetIntervalTimer(float value)   //回復タイマーセット
    {
        intervalTimer = value;
    }

    IEnumerator ChainWait() //コイン連鎖移動
    {
        yield return null;
        Warp(0.18f);    //タイマーを上げると連鎖移動が加速
        lulu.Update();
    }

    //MP回復
    public void PlusMagic(float set)
    {
        nowMagic = Mathf.Clamp(nowMagic + set, 0, maxMagic);
    }

    public void WarpEffectStart()   //ワープエフェクト
    {
        Instantiate(warpEffect, transform.position + transform.up, Quaternion.identity);
    }
    public void EmergencyEffectStart()  //緊急回避エフェクト
    {
        StartCoroutine("AvoidanceEffect");
        Instantiate(warpEffect, transform.position + transform.up, Quaternion.identity, GameObject.Find("Player").transform);
        GameObject emergencyGenerate = Instantiate(emergencyEffect, transform.position + transform.up, Quaternion.identity);
        emergencyGenerate.GetComponent<Animator>().speed = 0.5f;
    }
     //緊急回避中
    IEnumerator AvoidanceEffect()
    {
        lulu.avoidance = true;
        yield return new WaitForSeconds(0.2f);
        if (lulu.avoidance)
        {
            GameObject effect = Instantiate(warpEffect, transform.position + transform.up, Quaternion.identity, GameObject.Find("Player").transform);
            effect.transform.localScale *= 2.5f;
        }
        yield return new WaitForSeconds(0.6f);
        lulu.avoidance = false;
    }

    // Update is called once per frame
    void Update()
    {
        DebugMessage();

        //ワープ実行タイマー
        if (timer != -1)
        {
            //MP着地回復
            if (lulu.IsGround() && nowMagic < maxMagic)
            {
                lulu.GenerateSmoke(2); lulu.GenerateSmoke(2); //着地エフェクト
                PlusMagic(maxMagic);
            }

            //ワープ実行
            WarpRun();
        }

        //ワープ入力
        WarpInput();

        //MP回復
        MPrecovery();
    }

    void WarpRun()
    {
        if (timer >= 0.5f)//タイマーが0.5以上の時(移動完了)
        {
            //硬直解除
            rb.constraints = RigidbodyConstraints2D.None;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<Lulu>().SetStop(false);
            stopFlag = false;
            timer = -1;
            lulu.SetYplus(0);

            //集中線解除
            iTween.ScaleTo(Concentrate, iTween.Hash("x", 0f, "time", 2f));
        }
        else if (timer >= 0.25f && effectSwitch)//タイマーが0.2以上の時(移動開始)
        {
            effectSwitch = false;

            GetComponent<PlayerInput>().SetDash(true);
            //ワープ位置調整
            TargetCheck(0);

            //氷攻撃
            if (Friends.Nord)
            {
                Instantiate((GameObject)Resources.Load("Slash1"), new Vector2(transform.position.x + (warpTarget.x - transform.position.x) / 2,
                    transform.position.y + (warpTarget.y - transform.position.y) / 2), Quaternion.identity);
            }

            //ワープ実行
            transform.position = new Vector3(warpTarget.x, warpTarget.y, 0);
            anim.SetBool("Warp", false);
            SE.playnum = 10;
            //ダッシュ入力・ワープ可能状態 初期化
            dashWarp = false;
            cool = false;
            //硬直解除
            rb.constraints = RigidbodyConstraints2D.None;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            //無敵解除
            if (privateInvinceFlag)
            {
                GetComponent<HPManager>().setInvince(false);
                privateInvinceFlag = false;
            }

            //集中線実行
            iTween.ScaleTo(Concentrate, iTween.Hash("x", 0.7f, "time", 0.1f));

            //コイン連鎖
            if (!enemyAttackFlag && nowMagic > 0)
                if (CoinJamp())
                {
                    //近くにコインが見つかった場合、一瞬インターバルを入れて再ワープ
                    StartCoroutine("ChainWait");
                    return;
                }
        }

        //ワープタイマー
        if (timer >= 0 && timer <= 1.0f)
        {
            timer += Time.deltaTime;
        }

        //強制離地 OFF
        if (!cool)
        {
            lulu.SetExitisGround(false);
        }
    }

    void WarpInput()
    {
        if (GameManager.currentGameState != GameState.Playing) //プレイ可能状態判定
            return;

        pos = gameObject.transform.position; //現在地セーブ (障害物チェックの基準点)
        MPManager(); //MPゲージ動作管理

        //オーバーヒート中
        if (overHeat == true)
        {
            nowMagic += 1f;
            return;
        }
        //ワープ不能中
        else if (ban > 0)
        {
            return;
        }
        //ワープインターバル中
        if (intervalTimer < warpInterval)
        {
            return;
        }

        //ワープ入力チェック
        Right();
        Left();
        Up();
        Down();
    }

    void MPrecovery()
    {
        if (intervalTimer < recoverInterval     //インターバル中
            || GetComponent<HPManager>().GetInvince()   //無敵中
            || nowMagic >= maxMagic)            //MP全快
        {
            return;
        }

        if (lulu.IsGround() == false) //空中（遅い）
        {
            PlusMagic(0.2f);
        }
        if (lulu.IsGround() == true) // 地上（早い）
        {
            PlusMagic(2f);
        }
    }

    //ワープ位置調整
    void TargetCheck(int Error)
    {
        //無限ループ防止
        Error++;
        if (Error > 100)
        {
            Debug.Log("WarpError " + transform.position);
            warpTarget = errorTarget;    //ワープキャンセル
            return;
        }

        //地形チェック
        if (lulu.GetPluspower() < lulu.GetReadypower()) //チャージ不足
        {
            warpMask = groundMask + EnemyMask; //敵を避ける
        }
        else //チャージ完了
        {
            warpMask = groundMask; //敵に突っ込む
        }

        //ワープ地点の上下左右をチェック
        hit = HitCheck(new Vector2(warpTarget.x - 0.1f, warpTarget.y), new Vector2(0.2f, 0));
        hit1 = HitCheck(new Vector2(warpTarget.x - 0.1f, warpTarget.y + transform.localScale.y * 4), new Vector3(0.2f, 0));
        hit2 = HitCheck(new Vector2(warpTarget.x - transform.localScale.x, warpTarget.y + transform.localScale.y * 0.2f - 0.1f), new Vector2(0, 2));
        hit3 = HitCheck(new Vector2(warpTarget.x + transform.localScale.x, warpTarget.y + transform.localScale.y * 0.2f - 0.1f), new Vector2(0, 2));

        if (hit && hit1 && hit2 && hit3)
        {
            //完全に地形に埋まっていた場合、地形を脱出するまで主人公側に大きく寄る
            warpTarget += new Vector3((transform.position.x - warpTarget.x) / 10, (transform.position.y - warpTarget.y) / 10, 0);
        }
        //ワープ先微調整
        if (hit || (!hit && !hit1 && hit2 && hit3)) warpTarget += new Vector3(0, 1, 0);
        if (hit1) warpTarget += new Vector3(0, -1, 0);
        if (hit2 || (hit && hit1 && !hit2 && !hit3)) warpTarget += new Vector3(1, 0, 0);
        if (hit3) warpTarget += new Vector3(-1, 0, 0);

        //微調整が無くなるまでループ(100回まで)
        if (hit || hit1 || hit2 || hit3)
            TargetCheck(Error);
    }
    //地形チェック
    RaycastHit2D HitCheck(Vector2 start, Vector2 dir)
    {
        Debug.DrawRay(start, dir, Color.yellow, 3f);
        return Physics2D.Raycast(start, dir, dir.x + dir.y, warpMask);
    }


    public void Cost()  //MP消費
    {
        intervalTimer = 0; //MP回復停止タイマー
        int cost; //実際に減る量

        if (lulu.IsGround() == true) //地上コスト（消費が若干減る）
        {
            cost = dashCost;
        }
        else //空中コスト
        {
            cost = warpCost;
        }

        PlusMagic(-cost); //MP消費実行
    }

    //ワープ処理開始　ワープ中にワープが読み込まれる可能性があるため、フラグ設定は一回のみ
    void Warp(float cooltime)
    {
        lulu.WarpMove(dashWarp); //慣性移動設定
        lulu.SetExitisGround(true); //強制離地設定

        if (!privateInvinceFlag) //通常の無敵判定とは別に、ワープによる無敵フラグ設定
        {
            GetComponent<HPManager>().setInvince(true);
            privateInvinceFlag = true;
        }
        if (!enemyAttackFlag) //攻撃以外でのワープで、MP消費
        {
            Cost();
        }
    
        //すぐ近くに目標があった場合、優先してワープ先に設定
        LongCheck();    

        WarpEffectStart(); 
        rb.constraints = RigidbodyConstraints2D.FreezePosition;
        if (!stopFlag)  //主人公操作不能設定
        {
            GetComponent<Lulu>().SetStop(true);
            stopFlag = true;
        }
        //アニメーション管理
        anim.SetBool("Warp", true);
        anim.SetBool("Fall", true);
        anim.Play("Warp_Enter");

        cool = true; //ワープインターバル設定
        timer = cooltime; //ワープクールタイム設定
        effectSwitch = true; //ワープ実行フラグ設定
        lulu.PowerUp(2); //微チャージ
    }

    void LongCheck()    //ワープ直前のターゲットチェック
    {
        enemyAttackFlag = false; 
        float adjust = 4.5f; //チェック位置調整

        //チェック位置微調整
        if (Input.GetKey(KeyCode.DownArrow)) //少し上
        {
            adjust -= 2.0f;
        }
        else if (Input.GetKey(KeyCode.UpArrow)) //少し下
        {
            adjust += 2.0f;
        }

        for (int i = 1; i <= 45; i++) //Rayチェック実行
        {
            if (hit) //ターゲット発見時点で終了
                break;

            if (GetComponent<SpriteRenderer>().flipX) //主人公の向いている方向へ、横長長方形状のチェック
            {
                hit = Physics2D.Raycast(new Vector2(transform.position.x - 2f, transform.position.y + transform.localScale.y + adjust - ((float)i / 5.0f)), new Vector3(8, 0, 0), 8, coinMask);
                if (targetRayCheck)
                    Debug.DrawRay(new Vector2(transform.position.x - 2f, transform.position.y + transform.localScale.y + adjust - ((float)i / 5.0f)), new Vector3(8, 0, 0), Color.red, 8);
            }
            else
            {
                hit = Physics2D.Raycast(new Vector2(transform.position.x + 2f, transform.position.y + transform.localScale.y + adjust - ((float)i / 5.0f)), new Vector3(-8, 0, 0), 8, coinMask);
                if (targetRayCheck)
                    Debug.DrawRay(new Vector2(transform.position.x + 2f, transform.position.y + transform.localScale.y + adjust - ((float)i / 5.0f)), new Vector3(-8, 0, 0), Color.red, 8);
            }
        }

        if (hit) //ターゲットの正体チェック
        {
            if (hit.collider.gameObject.layer.Equals(16)) //敵の場合
            {
                enemyCheck(hit.collider.gameObject); 
                enemyAttackFlag = true;
            }
            else //コインの場合
            {
                warpTarget = hit.transform.position + new Vector3(0, -2, 0); //位置の微調整をしつつ、ターゲットにワープ位置を設定
            }
        }
    }
    void enemyCheck(GameObject enemy)   //チャージ攻撃
    {
        if (lulu.GetPluspower() > 200 && enemy.GetComponent<Enemy>().GetInvince() == false) //チャージが十分溜まっていて、敵が無敵状態でないとき
        {
            if (GetComponent<SpriteRenderer>().flipX)//主人公右向き
            {
                warpTarget = enemy.transform.position + new Vector3(-enemy.transform.localScale.x / 2, enemy.transform.localScale.y, 0); //敵の左上にワープ位置設定
            }
            else//左向き
            {
                warpTarget = enemy.transform.position + new Vector3(enemy.transform.localScale.x / 2, enemy.transform.localScale.y, 0); //敵の右上にワープ位置設定
            }
        }
    }

    IEnumerator ActiveGage() //MP全快でゲージ非表示
    {
        active = false; 
        underMPGage.GetComponent<Animator>().Play("Magic Recover", 0, 0);
        yield return new WaitForSeconds(1f);
        MPGage.SetActive(false);
    }

    void MPManager()    //MPゲージ管理
    {
        if (intervalTimer <= recoverInterval) //MP回復開始までのインターバル
        {
            intervalTimer += Time.deltaTime; //インターバルタイマー
        }

        //MPゲージアクティブ管理
        if (nowMagic != maxMagic) //MPが減っているとき
        {
            underMPGage.GetComponent<Image>().color = new Color(1, 1, 1, 0); //全回復アニメーション用　常に非表示
            MPGage.SetActive(true);
            active = true;
        }
        else if (active == true) //MP全快時
            StartCoroutine("ActiveGage"); //ゲージ非表示演出

        //オーバーヒート回復
        if(nowMagic >= defaultMagic) 
        {
            overHeat = false;
        }
        //MP上限設定
        if(nowMagic > maxMagic) 
        {
            nowMagic = maxMagic;
        }

        if (nowMagic <= 0 && Life.over == false) //オーバーヒート
        {
            Instantiate(Resources.Load("Flash"));
            overHeat = true;
            PlusMagic(-maxMagic);
        }
    }

    #region 入力チェック

    void AreaCheckClear(string dir)
    {
        warpTarget = GameObject.Find("Warp Default " + dir).transform.position; //ワープ先一時保存
        /*
        if (GameObject.Find("Ride Check " + dir).GetComponent<RideCheck>().inGround == false
            && GameObject.Find("Warp Default " + dir).GetComponent<WarpDefault>().GroundCheck() == true)//ワープ先が地形に引っかかっていて、上が空いているとき
        {
            //ワープ先を上にずらす
            warpTarget = GameObject.Find("Ride Check " + dir).transform.position;
        }
        */

        Warp(0);
    }

    void RightWarp() //右方向への入力チェック
    {
        if (Input.GetKey(KeyCode.UpArrow)) //右上チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から右上に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(7, 7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x, warpTarget.y - transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("UpRight");
        }
        else if (Input.GetKey(KeyCode.DownArrow)) //右下チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から右下に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(7, -7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x, warpTarget.y + transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("DownRight");
        }
        else //右チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から右に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(10, 0, 0), 10, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x, warpTarget.y - transform.localScale.y / 2);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("Right");
        }
    }

    void LeftWarp() //左方向チェック
    {
        if (Input.GetKey(KeyCode.UpArrow)) //左上チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から左上に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(-7, 7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x + transform.localScale.x, warpTarget.y - transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("UpLeft");
        }
        else if (Input.GetKey(KeyCode.DownArrow)) //左下チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から左下に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(-7, -7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x + transform.localScale.x, warpTarget.y + transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("DownLeft");
        }
        else //左チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から左に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(-10, 0, 0), 10, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x + transform.localScale.x, warpTarget.y - transform.localScale.y / 2);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("Left");
        }
    }

    void UpWarp() //上方向チェック
    {
        if (Input.GetKey(KeyCode.RightArrow)) //右上チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から右上に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(7, 7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x, warpTarget.y - transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("UpRight");
        }
        else if (Input.GetKey(KeyCode.LeftArrow)) //左上チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から左上に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(-7, 7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x + transform.localScale.x, warpTarget.y - transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("UpLeft");
        }
        else //上チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から上に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x + 1.0f - (i * 0.5f), pos.y), new Vector3(0, 10, 0), 10, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x / 2, warpTarget.y - transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("Up");
        }
    }
    void DownWarp()
    {
        if (Input.GetKey(KeyCode.RightArrow)) //右下チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から右下に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(7, -7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x, warpTarget.y + transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("DownRight");
        }
        else if (Input.GetKey(KeyCode.LeftArrow)) //左下チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から左下に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x, pos.y + 1.0f - (i * 0.5f)), new Vector3(-7, -7, 0), 7 * 1.414f, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x + transform.localScale.x, warpTarget.y + transform.localScale.y);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("DownLeft");
        }
        else //下方向チェック
        {
            //コイン・ワープ禁止エリアなどを探す　主人公から下に向けてRay
            RaycastHit2D targetcheck;
            for (int i = 0; i <= 3; i++)
            {
                targetcheck = Physics2D.Raycast(new Vector2(pos.x + 1.0f - (i * 0.5f), pos.y), new Vector3(0, -9, 0), 9, targetMask);

                if (targetcheck) //ワープ先との間に遮蔽物があった場合　その地点(+微調整)をワープ先に強制設定
                {
                    warpTarget = targetcheck.point + new Vector2(0, -2);
                    warpTarget = new Vector2(warpTarget.x - transform.localScale.x / 2, warpTarget.y + transform.localScale.y * 1.8f);
                    Warp(0);
                    return;
                }
            }

            //遮蔽物が無い場合
            AreaCheckClear("Down");
        }
    }

    void Right() //右方向ワープ入力チェック
    {
        if (lulu.IsGround() == false)
        {
            if (!Input.GetKey(KeyCode.Z))
            {
                exitKey = true;
            }
            if (exitKey == true && cool == false)
            {
                if ((Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.RightArrow)) || (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.RightArrow)))
                {
                    //空中で一度[Z]を離し、そのまま[→]と[Z]を同時押しした状態
                    exitKey = false;

                    //Zキー判定を初期化してワープ判定
                    errorTarget = transform.position;
                    RightWarp();
                }
            }
        }
        else //着地したとき、Z判定を初期化
        {
            if (exitKey == true)
                exitKey = false;
        }

        //ダッシュ
        if (Rightdashtimer < PlayerInput.dashtime) //ダッシュ入力猶予
        {
            Rightdashtimer += Time.deltaTime;
            if (Rightdash >= 2 && Input.GetKeyDown(KeyCode.RightArrow) && cool == false) 
            {
                //ワープ可能な時に、素早く[→]キーを2回押した状態

                dashWarp = true;

                //ダッシュワープフラグを設定してワープ判定
                errorTarget = transform.position;
                RightWarp();
            }
        }
        else //ダッシュ段階初期化
        {
            Rightdash = 0;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) && Rightdash == 0) //1回目の[→]入力
        {
            Rightdash = 1; //ダッシュ準備
            Rightdashtimer = 0; //入力猶予初期化
        }
        if (Rightdash >= 1 && Input.GetKeyUp(KeyCode.RightArrow)) //すぐに[→]を離す
        {
            Rightdash = 2; //ダッシュ準備完了
            Rightdashtimer = 0; //入力猶予初期化
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)) 
        {
            //[→]以外が押されたらダッシュ段階初期化
            Rightdash = 0;
        }
    }
    void Left()//左方向入力チェック
    {
        if (lulu.IsGround() == false)
        {
            if (!Input.GetKey(KeyCode.Z))
            {
                exitKey = true;
            }
            if (exitKey == true && cool == false)
                if ((Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftArrow)) || (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.LeftArrow)))
                {
                    //空中で一度[Z]を離し、そのまま[←]と[Z]を同時押しした状態
                    exitKey = false;

                    //Zキー判定を初期化してワープ判定
                    errorTarget = transform.position;
                    LeftWarp();
                }
        }
        else //着地したとき、Z判定を初期化
        {
            if (exitKey == true)
                exitKey = false;
        }

        //ダッシュ
        if (Leftdashtimer < PlayerInput.dashtime) //ダッシュ入力猶予
        {
            Leftdashtimer += Time.deltaTime;
            if (Leftdash >= 2 && Input.GetKeyDown(KeyCode.LeftArrow) && cool == false)
            {
                //ワープ可能な時に、素早く[←]キーを2回押した状態

                dashWarp = true;

                //ダッシュワープフラグを設定してワープ判定
                errorTarget = transform.position;
                LeftWarp();
            }
        }
        else //ダッシュ段階初期化
        {
            Leftdash = 0;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) && Leftdash == 0) //1回目の[←]入力
        {
            Leftdash = 1; //ダッシュ準備
            Leftdashtimer = 0; //入力猶予初期化
        }
        if (Leftdash >= 1 && Input.GetKeyUp(KeyCode.LeftArrow)) //すぐに[←]を離す
        {
            Leftdash = 2; //ダッシュ準備完了
            Leftdashtimer = 0; //入力猶予初期化
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            //[←]以外が押されたらダッシュ段階初期化
            Leftdash = 0;
        }
    }

    void Up()//上方向入力チェック
    {
        if (lulu.IsGround() == false)
        {
            if (!Input.GetKey(KeyCode.Z))
            {
                exitKey = true;
            }
            if (exitKey == true && cool == false)
                if ((Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.UpArrow)) || (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.UpArrow)))
                {
                    //空中で一度[Z]を離し、そのまま[↑]と[Z]を同時押しした状態
                    exitKey = false;

                    //Zキー判定を初期化してワープ判定
                    errorTarget = transform.position;
                    UpWarp();
                }
        }
        else //着地したとき、Z判定を初期化
        {
            if (exitKey == true)
                exitKey = false;
        }

        //ダッシュ
        if (Updashtimer < PlayerInput.dashtime) //ダッシュ入力猶予
        {
            Updashtimer += Time.deltaTime;
            if (Updash >= 2 && Input.GetKeyDown(KeyCode.UpArrow) && cool == false)
            {
                //ワープ可能な時に、素早く[↑]キーを2回押した状態

                dashWarp = true;

                //ダッシュワープフラグを設定してワープ判定
                errorTarget = transform.position;
                UpWarp();
            }
        }
        else //ダッシュ段階初期化
        {
            Updash = 0;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && Updash == 0) //1回目の[↑]入力
        {
            Updash = 1; //ダッシュ準備
            Updashtimer = 0; //入力猶予初期化
        }
        if (Updash >= 1 && Input.GetKeyUp(KeyCode.UpArrow)) //すぐに[↑]を離す
        {
            Updash = 2; //ダッシュ準備完了
            Updashtimer = 0; //入力猶予初期化
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            //[↑]以外が押されたらダッシュ段階初期化
            Updash = 0;
        }
    }

    void Down()//下方向入力チェック
    {
        if (lulu.IsGround() == false)
        {
            if (!Input.GetKey(KeyCode.Z))
            {
                exitKey = true;
            }
            if (exitKey == true && cool == false)
                if ((Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.DownArrow)) || (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.DownArrow)))
                {
                    //空中で一度[Z]を離し、そのまま[↓]と[Z]を同時押しした状態
                    exitKey = false;

                    //Zキー判定を初期化してワープ判定
                    errorTarget = transform.position;
                    DownWarp();
                }
        }
        else //着地したとき、Z判定を初期化
        {
            if (exitKey == true)
                exitKey = false;
        }

        //ダッシュ
        if (Downdashtimer < PlayerInput.dashtime) //ダッシュ入力猶予
        {
            Downdashtimer += Time.deltaTime;
            if (Downdash >= 2 && Input.GetKeyDown(KeyCode.DownArrow) && cool == false)
            {
                //ワープ可能な時に、素早く[↓]キーを2回押した状態

                dashWarp = true;

                //ダッシュワープフラグを設定してワープ判定
                errorTarget = transform.position;
                DownWarp();
            }
        }
        else //ダッシュ段階初期化
        {
            Downdash = 0;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && Downdash == 0) //1回目の[↓]入力
        {
            Downdash = 1; //ダッシュ準備
            Downdashtimer = 0; //入力猶予初期化
        }
        if (Downdash >= 1 && Input.GetKeyUp(KeyCode.DownArrow)) //すぐに[↓]を離す
        {
            Downdash = 2; //ダッシュ準備完了
            Downdashtimer = 0; //入力猶予初期化
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            //[↓]以外が押されたらダッシュ段階初期化
            Downdash = 0;
        }
    }
    #endregion


    bool CoinJamp() //連鎖移動コインチェック
    {
        if (overHeat) //オーバーヒートの場合中断
            return false;

        //主人公の前方チェック
        if (GetComponent<SpriteRenderer>().flipX) //右向き
        {
            if (TargetcheckLoop(new Vector2(transform.position.x + transform.localScale.x, transform.position.y + transform.localScale.y + 5.5f), new Vector3(4, 0, 0), true, Color.red))
                return true;
        }
        else //左向き
        {
            if (TargetcheckLoop(new Vector2(transform.position.x - transform.localScale.x, transform.position.y + transform.localScale.y + 5.5f), new Vector3(-4, 0, 0), true, Color.red))
                return true;
        }

        //後方チェック
        if (GetComponent<SpriteRenderer>().flipX) //右向き
        {
            if (TargetcheckLoop(new Vector2(transform.position.x - transform.localScale.x, transform.position.y + transform.localScale.y + 5f), new Vector3(-4, 0, 0), true, Color.green))
                return true;
        }
        else //左向き
        {
            if (TargetcheckLoop(new Vector2(transform.position.x + transform.localScale.x, transform.position.y + transform.localScale.y + 5f), new Vector3(4, 0, 0), true, Color.green))
                return true;
        }

        //下方チェック
        if (TargetcheckLoop(new Vector2(transform.position.x + transform.localScale.x + 5f, transform.position.y - transform.localScale.y), new Vector3(0, -4, 0), false, Color.yellow))
            return true;

        //上方チェック
        if (TargetcheckLoop(new Vector2(transform.position.x + transform.localScale.x + 5f, transform.position.y + transform.localScale.y + 5f), new Vector3(0, 4, 0), false, Color.blue))
            return true;

        return false;
    }
    bool TargetcheckLoop(Vector2 startPos, Vector3 distance, bool goDown, Color color)
    {
        for (int i = 1; i <= 50; i++) //長方形ターゲットチェック
        {
            if (goDown) //Ray縦移動
            {
                hit = Physics2D.Raycast(new Vector2(startPos.x, startPos.y - ((float)i / 5.0f)), distance, 4, coinMask); //ターゲットチェック
                if (targetRayCheck)
                    Debug.DrawRay(new Vector2(startPos.x, startPos.y - ((float)i / 5.0f)), distance, color, 4); //チェック可視化
            }
            else //Ray横移動
            {
                hit = Physics2D.Raycast(new Vector2(startPos.x - ((float)i / 5.0f), startPos.y), distance, 4, coinMask); //ターゲットチェック
                if (targetRayCheck)
                    Debug.DrawRay(new Vector2(startPos.x - ((float)i / 5.0f), startPos.y), distance, color, 4); //チェック可視化
            }

            if (hit) //目標を見つけたとき
            {
                if (hit.collider.gameObject.layer.Equals(16)) //敵の場合
                {
                    enemyCheck(hit.collider.gameObject); //チャージ攻撃チェック
                    enemyAttackFlag = true; //攻撃状態設定
                }
                else //コインの場合
                {
                    warpTarget = hit.transform.position + new Vector3(0, -2, 0); //ワープ位置設定
                    enemyAttackFlag = false; //攻撃状態解除
                }
                return true;
            }
        }
        return false;
    }

    public void EventWarp(Vector3 target)   //イベントシーン用
    {
        StartCoroutine("warpWait"); //ワープアニメ再生
        transform.position = target; //目標地点へ移動
        StartCoroutine("warpWait2"); //ワープ終了
    }

    IEnumerator warpWait()
    {
        WarpEffectStart(); //ワープアニメ再生
        rb.constraints = RigidbodyConstraints2D.FreezePosition; //物理運動停止

        anim.SetBool("Warp", true); //ワープアニメフラグ
        anim.SetBool("Fall", true); //空中アニメフラグ
        anim.Play("Warp_Enter"); //ワープアニメ再生

        yield return new WaitForSeconds(0.3f); //一瞬待機
    }

    IEnumerator warpWait2()
    {
        anim.SetBool("Warp", false); //ワープアニメフラグ停止
        yield return new WaitForSeconds(0.2f); //一瞬待機
        rb.constraints = RigidbodyConstraints2D.None; //物理運動開始
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; //回転禁止（バグ防止）
    }
    //画面上にフラグの表示
    void DebugMessage()
    {
        if(transform.rotation.z != 0)
        {
            transform.localRotation = new Quaternion(0, 0, 0, 0);
        }
        if (DebugAttack)
        {
            GameObject.Find("AttackFlag").GetComponent<Text>().text =
                "Attack = " + enemyAttackFlag;

            GameObject.Find("DashFlag").GetComponent<Text>().text =
                "Dash = " + GetComponent<PlayerInput>().getDash();
        }
    }
}