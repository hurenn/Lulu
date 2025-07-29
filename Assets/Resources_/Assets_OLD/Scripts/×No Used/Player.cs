
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    float slow;
    public static bool grabB = false;
    public static bool throwB = false;
    public static bool stop = false;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;
    public float moveSpeed = 6;
    public float dashSpeed = 10;
    public float finalFallSpeed = -40;
    float time = 0;
    public static float invinceTime = 10f;
    public static float maxInvince = 1.5f;
    public static bool avoidAnim = false;
    bool invinceAnim = false;
    public static int crushDamage = 100;
    public static bool crushble = false;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    public static Vector3 velocity;
    float velocityXSmoothing;

    Controller2D controller;

    Vector2 directionalInput;

    void Start()
    {
        controller = GetComponent<Controller2D>();
        invinceTime = 10f;

        maxJumpHeight = 5;
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update()
    {
        if (Friends.Marlica == true && maxJumpHeight != 3.7f)
        {
            maxJumpHeight = 3.7f;
            gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        }
        if(Friends.Marlica == false && maxJumpHeight != 5)
        {
            maxJumpHeight = 5;
            gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        }

        if (velocity.y < finalFallSpeed)
        {
            if (!crushble)
                crushble = true;
        }
        else
        {
            if (crushble)
                crushble = false;
        }

        if (invinceTime < maxInvince)//無敵時間
        {
            invinceTime += Time.deltaTime;
            if (invinceAnim == false)
            {
                if (StoneUI.use == true)
                {
                    this.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 0.7f, 0.3f);
                }
                else
                {
                    this.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.3f);
                }
                invinceAnim = true;
            }
            else
            {
                if (StoneUI.use == true)
                {
                    this.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 0.7f, 1f);
                }
                else
                {
                    this.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);
                }
                invinceAnim = false;
            }

        }
        else
        {
            avoidAnim = false;
            if (invinceAnim == true)
            {
                if (StoneUI.use == true)
                {
                    this.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 0.7f, 1f);
                }
                else
                {
                    this.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1f);
                }
                invinceAnim = false;
            }
        }

        //Debug.Log("warpcool = " + WarpControl.cool + ", stop = " + stop);
        if (GetComponent<WarpControl>().cool == false && stop == false && Life.over == false)//どちらかtrueならその場で停止
        {
            CalculateVelocity();
            //HandleWallSliding(); //壁キック
            /*
            if (GameObject.Find("WarpPad").GetComponent<WarpPad>().next == false && grabB == false)
            controller.Move(velocity * Time.deltaTime, directionalInput);//移動（重力、ジャンプも兼ねてる）
            */
            if (controller.collisions.above || controller.collisions.below)//重力のリセットと滑り落ちの一部？
            {
                if (controller.collisions.slidingDownMaxSlope)
                {
                    velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
                }
                else
                {
                    velocity.y = 0;
                }
            }
        }
        else
        {
            velocity.y = 0;
            velocity.x = 0;
        }
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()//ジャンプボタン押したとき
    {

        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                { // not jumping against max slope
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    public void OnJumpInputUp()//ジャンプボタン離したとき
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        if (GetComponent<PlayerInput>().GetDash())
            targetVelocityX = directionalInput.x * dashSpeed;
        /*
        if (Hang.hanging == true)
        {
            rb = GameObject.FindWithTag("Hanged").GetComponent<Rigidbody2D>();
            slow = GameObject.FindWithTag("Hanged").GetComponent<HangedObject>().heavy;
            if(slow > 14)
            {
                slow = 14;
            }
            targetVelocityX *= (15 - slow) / 15;
        }*/

        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);

        if (velocity.y > finalFallSpeed)
        {
            /*
            if (Hang.hanging == true)
            {

                float h = GameObject.FindWithTag("Hanged").GetComponent<HangedObject>().heavy;
                if (h > 3)
                    h = 3;
                velocity.y += gravity * h * Time.deltaTime ;
            }
            else
            {*/
                velocity.y += gravity * Time.deltaTime * 0.8f;
            //}
        }

    }
}