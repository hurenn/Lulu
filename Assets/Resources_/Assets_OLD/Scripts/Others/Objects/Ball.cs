using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    Rigidbody2D rb;
    private bool up;
    private bool down;
    private bool right;
    private bool left;
    int groundLayer = 1 << 9;// | 1 << 16;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            rb.AddForce(new Vector2(-20, 7), ForceMode2D.Impulse);
        }

        right = Physics2D.Linecast(transform.position, transform.position + transform.right / 1.5f, groundLayer);
        Debug.DrawLine(transform.position, transform.position + transform.right / 1.5f, Color.green);
        left = Physics2D.Linecast(transform.position - transform.right / 1.5f, transform.position, groundLayer);
        Debug.DrawLine(transform.position - transform.right / 1.5f, transform.position, Color.yellow);

        up = Physics2D.Linecast(transform.position, transform.position + transform.up / 1.5f, groundLayer);
        Debug.DrawLine(transform.position, transform.position + transform.up / 1.5f, Color.red);
        down = Physics2D.Linecast(transform.position - transform.up / 1.5f, transform.position, groundLayer);
        Debug.DrawLine(transform.position - transform.up / 1.5f, transform.position, Color.blue);

        if (right && rb.linearVelocity.x > 0)
        {
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y);
        }
        else if (left && rb.linearVelocity.x < 0)
        {
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y);
        }

        if (up && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y);
        }
        else if (down && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y);
        }
    }
}
