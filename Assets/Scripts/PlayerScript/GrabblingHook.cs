using System.Collections;
using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    private LineRenderer line;
    public float maxDistance = 10f;
    public float grappleSpeed = 5f;
    private float grappleShootSpeed = 10f;
    private bool isGrappling = false;
    public bool retracting = false;

    public LayerMask grappleLayerMask;

    private Vector2 target;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.positionCount = 2;
            line.enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isGrappling || retracting)
            {
                ReleaseGrapple();
            }
            else
            {
                StartGrapple();
            }
        }

        if (retracting)
        {
            Vector2 grapplePos = Vector2.Lerp(transform.position, target, grappleSpeed * Time.deltaTime);
            transform.position = grapplePos;

            if (line != null)
            {
                line.SetPosition(0, transform.position);
            }

            if (Vector2.Distance(transform.position, target) < 0.5f)
            {
                isGrappling = false;
                retracting = false;
                if (line != null)
                {
                    line.enabled = false;
                }
            }
        }
    }

    private void StartGrapple()
    {
        if (Camera.main == null)
            return;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePosition - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxDistance, grappleLayerMask);

        if (hit.collider != null)
        {
            target = hit.point;
            isGrappling = true;
            if (line != null)
            {
                line.enabled = true;
                line.positionCount = 2;
            }
            StartCoroutine(Grapple());
        }
    }

    private IEnumerator Grapple()
    {
        float t = 0f;
        float time = 10f;

        if (line != null)
        {
            line.SetPosition(0, transform.position);
            line.SetPosition(1, transform.position);
        }

        Vector2 newPosition;
        for (; t < time; t += grappleShootSpeed * Time.deltaTime)
        {
            if (!isGrappling)
                yield break;

            newPosition = Vector2.Lerp(transform.position, target, t / time);
            transform.position = newPosition;

            if (line != null)
            {
                line.SetPosition(0, transform.position);
                line.SetPosition(1, target);
            }

            yield return null;
        }

        if (line != null)
        {
            line.SetPosition(1, target);
        }

        retracting = true;
    }

    private void ReleaseGrapple()
    {
        isGrappling = false;
        retracting = false;

        if (line != null)
        {
            line.enabled = false;
        }
    }
}
