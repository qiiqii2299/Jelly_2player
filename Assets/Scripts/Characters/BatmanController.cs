using UnityEngine;

public class BatmanController : PlayerBase
{
    [Header("Cấu hình Móc câu")]
    public Transform  firePoint;
    public GameObject sideEffectPrefab;
    public GameObject dartPrefab;

    [Header("Tầm bắn & Góc quét")]
    public float maxGrappleDistance = 15f;
    public float minGrappleDistance = 2f;
    [Tooltip("Góc lệch so với thẳng đứng, nghiêng về phía mặt nhân vật (độ)")]
    public float scanAngle  = 40f;
    public LayerMask grappleLayer;

    [Header("Kéo về điểm bám")]
    public float pullSpeed        = 15f;
    public float arrivalThreshold = 0.4f;

    // ---- nội bộ ----
    private LineRenderer lr;
    private GameObject   activeDart;
    private Vector2      hookPoint;
    private bool         hasTarget = false;
    private float        facingX   = 1f;

    protected override void Start()
    {
        base.Start();
        SetupLineRenderer();
    }

    // -------------------------------------------------------
    void SetupLineRenderer()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.startWidth    = 0.06f;
        lr.endWidth      = 0.06f;
        lr.sortingOrder  = 10;

        if (lr.sharedMaterial == null || lr.sharedMaterial.name.Contains("Default-Line"))
        {
            lr.material    = new Material(Shader.Find("Sprites/Default"));
            lr.startColor  = new Color(0.7f, 0.7f, 0.7f);
            lr.endColor    = new Color(0.4f, 0.4f, 0.4f);
        }

        lr.enabled = false;
    }

    // -------------------------------------------------------
    protected override void HandleSkillInput()
    {
        bool skillPressed = inputController != null
            ? inputController.IsSkillPressed
            : Input.GetKeyDown(KeyCode.Space);

        // ---- Đang kéo về điểm bám ----
        if (isGrappling)
        {
            rb.bodyType    = RigidbodyType2D.Dynamic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            PullTowardHook();

            // Bấm skill trong lúc kéo → thả móc ngay
            if (skillPressed)
                StopGrapple(true);

            return;
        }

        // ---- Chưa bắn: scan mỗi frame tìm điểm bám ----
        ScanForTarget();

        // Bấm skill + có target → bắn
        if (skillPressed && hasTarget)
            ShootHook();
    }

    // -------------------------------------------------------
    // Scan hướng lên trên nghiêng theo hướng mặt — giống GrabblingHook
    // -------------------------------------------------------
    void ScanForTarget()
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;

        facingX = transform.right.x > 0f ? 1f : -1f;

        float   rad     = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * facingX, Mathf.Cos(rad)).normalized;

        RaycastHit2D hit = grappleLayer != 0
            ? Physics2D.Raycast(origin, scanDir, maxGrappleDistance, grappleLayer)
            : Physics2D.Raycast(origin, scanDir, maxGrappleDistance);

        if (hit.collider != null
            && hit.collider.gameObject != gameObject
            && Vector2.Distance(origin, hit.point) >= minGrappleDistance)
        {
            hasTarget = true;
            hookPoint = hit.point;
        }
        else
        {
            hasTarget = false;
        }
    }

    // -------------------------------------------------------
    void ShootHook()
    {
        isGrappling = true;

        rb.bodyType    = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Hiệu ứng tại điểm bám
        if (sideEffectPrefab != null)
        {
            GameObject fx = Instantiate(sideEffectPrefab, hookPoint, Quaternion.identity);
            Destroy(fx, 0.5f);
        }

        // Dart tại điểm bám
        if (dartPrefab != null)
        {
            if (activeDart == null)
                activeDart = Instantiate(dartPrefab, hookPoint, Quaternion.identity);
            else
            {
                activeDart.transform.position = hookPoint;
                activeDart.SetActive(true);
            }
        }

        lr.enabled = true;
        UpdateLine();
    }

    // -------------------------------------------------------
    // Kéo Batman thẳng về hookPoint
    // -------------------------------------------------------
    void PullTowardHook()
    {
        Vector2 pos  = transform.position;
        Vector2 dir  = (hookPoint - pos).normalized;
        float   dist = Vector2.Distance(pos, hookPoint);

        rb.gravityScale   = 0f;
        rb.linearVelocity = dir * pullSpeed;

        UpdateLine();

        if (dist <= arrivalThreshold)
            StopGrapple(false);
    }

    // -------------------------------------------------------
    void StopGrapple(bool keepVelocity)
    {
        isGrappling = false;
        hasTarget   = false;

        rb.gravityScale = 1f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        if (!keepVelocity)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        lr.enabled = false;

        if (activeDart != null)
            activeDart.SetActive(false);
    }

    // -------------------------------------------------------
    void UpdateLine()
    {
        if (!lr.enabled) return;
        Vector2 start = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;
        lr.SetPosition(0, start);
        lr.SetPosition(1, hookPoint);
    }

    protected new void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        if (isGrappling && collision.contacts[0].normal.y > 0.5f)
            StopGrapple(false);
    }

    // Gizmo: vẽ tia scan để thấy tầm bắn trong Scene
    private void OnDrawGizmosSelected()
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;

        float   fx      = Application.isPlaying ? facingX : (transform.right.x > 0f ? 1f : -1f);
        float   rad     = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * fx, Mathf.Cos(rad)).normalized;

        Gizmos.color = hasTarget ? Color.green : Color.yellow;
        Gizmos.DrawRay(origin, scanDir * maxGrappleDistance);

        if (hasTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hookPoint, 0.2f);
        }
    }
}
