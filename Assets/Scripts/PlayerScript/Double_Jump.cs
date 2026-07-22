using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class Double_Jump : MonoBehaviour
{

    public float speed = 5f;
    public float JumpForce = 5f;
    private Rigidbody2D rb;
    private int maxJump = 2;
    public int currentJump = 0;
    //private Animator animator;
    public bool Ground = false;
    public WebSwing webSwingScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            currentJump = 0;
            //animator.SetBool("IsGrounded", true);
            Ground = true;
            //animator.SetBool("Jumping?", false);
        }
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            //animator.SetBool("IsGrounded", false);
            Ground = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!webSwingScript.isSwinging)
        {
            float move = 0f;

            if (Input.GetKey(KeyCode.D))
            {
                move = 1f;
                //animator.SetBool("Running?", true);
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (Input.GetKey(KeyCode.A))
            {
                move = -1f;
                //animator.SetBool("Running?", true);
                transform.localScale = new Vector3(-1, 1, 1);
            }

            rb.linearVelocity = new Vector2(move * speed, rb.linearVelocity.y);
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentJump < maxJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, JumpForce);
            currentJump++;
        }
    }

    public void ResetJumpCount()
    {
        currentJump = 1;
    }
}
