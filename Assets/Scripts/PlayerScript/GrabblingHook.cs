using UnityEngine;

/// <summary>
/// Gắn lên Batman.
/// - Nhận phím bấm trực tiếp (Space, Z, X).
/// - Tích hợp quản lý hình ảnh 3 góc (Trái, Phải, Chính).
/// - Quét phía trên đầu (nghiêng theo hướng mặt) tìm điểm bám móc câu.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class GrabblingHook : MonoBehaviour
{
    [Header("Cài Đặt Phím Bấm")]
    public KeyCode hookKey = KeyCode.Space;
    public KeyCode ropeInKey = KeyCode.Z;
    public KeyCode ropeOutKey = KeyCode.X;

    [Header("Hình Ảnh (Sprites)")]
    [Tooltip("Ảnh mặt chính (đứng im / lúc rơi)")]
    public Sprite frontSprite;
    [Tooltip("Ảnh khi di chuyển/đu sang TRÁI")]
    public Sprite leftSprite;
    [Tooltip("Ảnh khi di chuyển/đu sang PHẢI")]
    public Sprite rightSprite;

    [Header("Móc câu")]
    public Transform hookOrigin;               // điểm xuất phát móc (tay Batman)
    public float maxDistance = 12f;
    public float minDistance = 2f;
    public LayerMask hookLayer;                // layer tường/platform có thể bám

    [Header("Góc quét")]
    [Tooltip("Góc lệch so với thẳng đứng khi quét tìm điểm bám (độ)")]
    public float scanAngle = 40f;

    [Header("Điều khiển dây")]
    public float climbSpeed = 5f;              // Tốc độ thu ngắn / thả dài dây khi giữ phím Z / X

    [Header("Lực lấy đà (phóng khi nhấn Space lần 2)")]
    public float launchForceX = 10f;           // lực ngang khi phóng
    public float launchForceY = 12f;           // lực dọc khi phóng

    [Header("Prefab hiệu ứng")]
    public GameObject prefab_HookEffect;       // hiệu ứng khi móc bám tường
    public GameObject prefab_HookDart;         // thân móc hiển thị

    // ---- nội bộ ----
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private SpriteRenderer spriteRenderer;
    private GameObject activeDart;
    private Vector2 hookPoint;
    private DistanceJoint2D hookJoint;
    private bool hasTarget = false;
    private bool isPulling = false;            // đang bám móc/đu dây
    private float facingX = 1f;                // hướng mặt lúc bắn (1 = Phải, -1 = Trái)
    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (frontSprite != null) spriteRenderer.sprite = frontSprite;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.enabled = false;

        if (lineRenderer.material == null || lineRenderer.material.name.Contains("Default"))
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.grey;
            lineRenderer.endColor = Color.grey;
        }
    }

    void Update()
    {
        // 1. Cập nhật đổi hình ảnh 3 góc (Trái / Phải / Chính)
        UpdateSpriteAppearance();

        // 2. Quét tìm mục tiêu nếu chưa bám móc
        if (!isPulling)
            ScanForTarget();

        // 3. Xử lý phím chính (Space): Bắn móc hoặc Phóng đi
        if (Input.GetKeyDown(hookKey))
        {
            if (isPulling)
                Launch();          // đang bám → phóng ra lấy đà
            else if (hasTarget)
                ShootHook();       // có target → bắn móc
        }

        // 4. Điều chỉnh độ dài dây khi đang bám
        if (isPulling)
            HandleRopeAdjustment();
    }

    // -------------------------------------------------------
    // QUẢN LÝ HÌNH ẢNH (ĐỔI MẶT TRÁI / PHẢI / CHÍNH)
    // -------------------------------------------------------
    void UpdateSpriteAppearance()
    {
        spriteRenderer.flipX = false;
        Vector3 fixedScale = transform.localScale;
        fixedScale.x = Mathf.Abs(originalScale.x);
        transform.localScale = fixedScale;

        float speedX = rb != null ? rb.linearVelocity.x : 0f;

        if (speedX < -0.1f)
        {
            if (leftSprite != null) spriteRenderer.sprite = leftSprite;
            facingX = -1f;
        }
        else if (speedX > 0.1f)
        {
            if (rightSprite != null) spriteRenderer.sprite = rightSprite;
            facingX = 1f;
        }
        else
        {
            if (frontSprite != null) spriteRenderer.sprite = frontSprite;

            // Cho phép đổi hướng nhìn qua phím A/D hoặc mũi tên ngay cả khi đứng im
            float inputX = Input.GetAxisRaw("Horizontal");
            if (inputX < 0) facingX = -1f;
            else if (inputX > 0) facingX = 1f;
        }
    }

    // -------------------------------------------------------
    // Quét phía trên đầu nghiêng theo hướng mặt
    // -------------------------------------------------------
    void ScanForTarget()
    {
        Vector2 origin = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;

        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * facingX, Mathf.Cos(rad)).normalized;

        RaycastHit2D hit = hookLayer != 0
            ? Physics2D.Raycast(origin, scanDir, maxDistance, hookLayer)
            : Physics2D.Raycast(origin, scanDir, maxDistance);

        if (hit.collider != null && hit.collider.gameObject != gameObject
            && Vector2.Distance(origin, hit.point) >= minDistance)
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
    // Bắn móc
    // -------------------------------------------------------
    void ShootHook()
    {
        isPulling = true;
        rb.gravityScale = 0.3f;   // giảm gravity khi đang bám móc
        lineRenderer.enabled = true;

        hookJoint = gameObject.AddComponent<DistanceJoint2D>();
        hookJoint.connectedAnchor = hookPoint;
        hookJoint.autoConfigureDistance = false;

        Vector2 origin = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;
        hookJoint.distance = Vector2.Distance(origin, hookPoint);
        hookJoint.enableCollision = true;

        if (prefab_HookEffect != null)
        {
            GameObject fx = Instantiate(prefab_HookEffect, hookPoint, Quaternion.identity);
            Destroy(fx, 0.5f);
        }

        if (prefab_HookDart != null)
        {
            if (activeDart == null)
                activeDart = Instantiate(prefab_HookDart, hookPoint, Quaternion.identity);
            else
            {
                activeDart.transform.position = hookPoint;
                activeDart.SetActive(true);
            }
            SetSortingOrder(activeDart, 20);
        }
    }

    // -------------------------------------------------------
    // Điều chỉnh độ dài dây bằng phím Z và X
    // -------------------------------------------------------
    void HandleRopeAdjustment()
    {
        if (hookJoint != null)
        {
            if (Input.GetKey(ropeInKey))
            {
                hookJoint.distance -= climbSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(ropeOutKey))
            {
                hookJoint.distance += climbSpeed * Time.deltaTime;
            }

            hookJoint.distance = Mathf.Clamp(hookJoint.distance, minDistance, maxDistance);
        }

        UpdateLine();
    }

    // -------------------------------------------------------
    // Space lần 2 khi đang bám → phóng ra lấy đà
    // -------------------------------------------------------
    void Launch()
    {
        ReleaseHook(true);
        rb.linearVelocity = new Vector2(facingX * launchForceX, launchForceY);
    }

    // -------------------------------------------------------
    void ReleaseHook(bool keepVelocity)
    {
        isPulling = false;
        hasTarget = false;
        rb.gravityScale = 1f;
        lineRenderer.enabled = false;

        if (hookJoint != null)
        {
            Destroy(hookJoint);
            hookJoint = null;
        }

        if (!keepVelocity)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (activeDart != null)
            activeDart.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isPulling && collision.contacts[0].normal.y > 0.5f)
            ReleaseHook(false);
    }

    void UpdateLine()
    {
        if (!lineRenderer.enabled) return;
        Vector2 start = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, hookPoint);
    }

    void SetSortingOrder(GameObject obj, int order)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = order;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origin = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;
        float fx = Application.isPlaying ? facingX : (transform.right.x > 0 ? 1f : -1f);
        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * fx, Mathf.Cos(rad)).normalized;

        Gizmos.color = hasTarget ? Color.green : Color.yellow;
        Gizmos.DrawRay(origin, scanDir * maxDistance);
        if (hasTarget) Gizmos.DrawWireSphere(hookPoint, 0.2f);
    }
}