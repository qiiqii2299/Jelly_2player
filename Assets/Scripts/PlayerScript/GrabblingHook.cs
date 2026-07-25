using UnityEngine;

/// <summary>
/// Gắn lên Batman.
/// - Mỗi frame quét phía trên đầu (nghiêng theo hướng mặt) tìm điểm bám.
/// - Space: có điểm bám → bắn móc, bắt đầu bám dây.
/// - Phím Z: thu ngắn dây | Phím X: thả dài dây ra.
/// - Space (lần 2 khi đang bám): phóng Batman ra theo lực lấy đà rồi thả móc.
/// - Chạm đất: tự thả móc.
/// </summary>
public class GrabblingHook : MonoBehaviour
{
    [Header("Móc câu")]
    public Transform hookOrigin;             // điểm xuất phát móc (tay Batman)
    public float maxDistance = 12f;
    public float minDistance = 2f;
    public LayerMask hookLayer;              // layer tường/platform có thể bám

    [Header("Góc quét")]
    [Tooltip("Góc lệch so với thẳng đứng khi quét tìm điểm bám (độ)")]
    public float scanAngle = 40f;

    [Header("Điều khiển dây (Giống Spider-Man)")]
    public float climbSpeed = 5f;             // Tốc độ thu ngắn / thả dài dây khi giữ phím Z / X

    [Header("Lực lấy đà (phóng khi nhấn Space lần 2)")]
    public float launchForceX = 10f;         // lực ngang khi phóng
    public float launchForceY = 12f;         // lực dọc khi phóng

    [Header("Prefab hiệu ứng")]
    public GameObject prefab_HookEffect;    // hiệu ứng khi móc bám tường
    public GameObject prefab_HookDart;      // thân móc hiển thị

    // ---- nội bộ ----
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private GameObject activeDart;
    private Vector2 hookPoint;
    private DistanceJoint2D hookJoint;      // Sử dụng DistanceJoint2D để giữ dây co giãn theo ý muốn giống Spiderman
    private bool hasTarget = false;
    private bool isPulling = false;   // đang bám móc/đu dây
    private float facingX = 1f;      // hướng mặt lúc bắn

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

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
        if (!isPulling)
            ScanForTarget();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPulling)
                Launch();          // đang bám → phóng ra lấy đà
            else if (hasTarget)
                ShootHook();       // có target → bắn móc
        }

        if (isPulling)
            HandleRopeAdjustment();
    }

    // -------------------------------------------------------
    // Quét phía trên đầu nghiêng theo hướng mặt
    // -------------------------------------------------------
    void ScanForTarget()
    {
        Vector2 origin = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;
        facingX = transform.right.x > 0 ? 1f : -1f;

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

        // Tạo DistanceJoint2D để giữ khoảng cách linh hoạt với điểm bám
        hookJoint = gameObject.AddComponent<DistanceJoint2D>();
        hookJoint.connectedAnchor = hookPoint;
        hookJoint.autoConfigureDistance = false;

        Vector2 origin = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;
        hookJoint.distance = Vector2.Distance(origin, hookPoint);
        hookJoint.enableCollision = true;

        // Hiệu ứng tại điểm bám
        if (prefab_HookEffect != null)
        {
            GameObject fx = Instantiate(prefab_HookEffect, hookPoint, Quaternion.identity);
            Destroy(fx, 0.5f);
        }

        // Dart hiển thị tại điểm bám
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
    // Điều chỉnh độ dài dây bằng phím Z và X khi đang bám móc
    // -------------------------------------------------------
    void HandleRopeAdjustment()
    {
        if (hookJoint != null)
        {
            // Bấm Z để thu ngắn dây lại, X để thả dài dây ra
            if (Input.GetKey(KeyCode.Z))
            {
                hookJoint.distance -= climbSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.X))
            {
                hookJoint.distance += climbSpeed * Time.deltaTime;
            }

            // Giới hạn khoảng cách dây trong khoảng an toàn
            hookJoint.distance = Mathf.Clamp(hookJoint.distance, minDistance, maxDistance);
        }

        // Cập nhật hiển thị sợi dây liên tục theo vị trí tay Batman và điểm bám
        UpdateLine();
    }

    // -------------------------------------------------------
    // Space lần 2 khi đang bám → phóng ra lấy đà
    // -------------------------------------------------------
    void Launch()
    {
        ReleaseHook(true);
        // Phóng theo hướng mặt + lên trên
        rb.linearVelocity = new Vector2(facingX * launchForceX, launchForceY);
    }

    // -------------------------------------------------------
    void ReleaseHook(bool keepVelocity)
    {
        isPulling = false;
        hasTarget = false;
        rb.gravityScale = 1f;
        lineRenderer.enabled = false;

        // Xóa DistanceJoint2D khi thả móc
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

    // Tự thả khi chạm đất
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