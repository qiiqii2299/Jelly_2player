using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebSwing : MonoBehaviour
{
    public LayerMask AttachableLayers;
    public float maxWebDistance = 5f;
    public float minWebDistance = 2f;
    public float swingForce = 1f;
    public float normalJumpForce = 5f;
    public LineRenderer webLine;
    public Transform webOrigin;
    private DistanceJoint2D webJoint;
    private Vector2 webAttachPoint;
    private Rigidbody2D rb;
    public Double_Jump jumpScript;
    public bool isSwinging = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleWebShooting();
        HandleSwinging();
        HandleWebRelease();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSwinging)
        {
            Destroy(webJoint);
            ClearWebLine();
            isSwinging = false;
            rb.linearDamping = 0f;
        }
    }

    void HandleWebShooting()
    {
        if (jumpScript.Ground || isSwinging)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 aimDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - webOrigin.position;
            RaycastHit2D hit = Physics2D.Raycast(webOrigin.position, aimDirection.normalized, maxWebDistance, AttachableLayers);

            if (hit.collider != null)
            {
                float distanceToTarget = Vector2.Distance(webOrigin.position, hit.point);

                if (distanceToTarget < minWebDistance)
                {
                    return;
                }

                webAttachPoint = hit.point;
                AttachWeb(webAttachPoint);
                DrawWebLine();
                isSwinging = true;
                rb.linearDamping = 0.5f;
            }
        }
    }

    void AttachWeb(Vector2 attachPoint)
    {
        webJoint = gameObject.AddComponent<DistanceJoint2D>();
        webJoint.connectedAnchor = attachPoint;
        webJoint.autoConfigureDistance = false;
        webJoint.distance = Vector2.Distance(webOrigin.position, attachPoint);
        webJoint.enableCollision = true;
    }

    void HandleSwinging()
    {
        if (webJoint != null)
        {
            Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
            rb.AddForce(forceDirection * swingForce);
        }
    }

    void HandleWebRelease()
    {
        if (Input.GetKeyDown(KeyCode.Space) && webJoint != null)
        {
            Destroy(webJoint);
            ClearWebLine();
            isSwinging = false;
            rb.linearDamping = 0f;
            ApplyReleaseJump();
        }
    }

    void ApplyReleaseJump()
    {
        Vector2 jumpForce = new Vector2(rb.linearVelocity.x, normalJumpForce);
        rb.AddForce(jumpForce, ForceMode2D.Impulse);
        jumpScript.ResetJumpCount();
    }

    void DrawWebLine()
    {
        webLine.positionCount = 2;
        webLine.SetPosition(0, webOrigin.position);
        webLine.SetPosition(1, webAttachPoint);
    }

    void ClearWebLine()
    {
        webLine.positionCount = 0;
    }

    void LateUpdate()
    {
        if (webJoint != null)
        {
            DrawWebLine();
        }
    }
}
