using UnityEngine;

/// <summary>
/// Gắn lên Spiderman.
/// - Mỗi frame quét hình quạt phía trên đầu để tìm điểm bám.
/// - Space: nếu tìm thấy điểm bám → bắn tơ lên đó. Đang đu → thả tơ.
/// - Hướng quét nghiêng theo hướng mặt nhân vật (trái/phải) + lên trên.
/// </summary>
public class WebSwing : MonoBehaviour
{
    [Header("Tơ")]
    public Transform webOrigin;             // điểm xuất phát tơ (Spiderman_Throw_Point)
    public float maxWebDistance = 15f;
    public float minWebDistance = 2f;
    public LayerMask attachableLayers;      // layer tường/platform có thể bám

    [Header("Góc quét")]
    [Tooltip("Góc lệch so với thẳng đứng khi quét tìm điểm bám (độ)")]
    public float scanAngle = 45f;           // quét tối đa 45° so với thẳng đứng

    [Header("Vật lý đu")]
    public float swingForce = 20f;

    // ---- nội bộ ----
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private DistanceJoint2D webJoint;
    private Vector2 attachPoint;
    private bool hasTarget = false;     // có điểm bám hợp lệ phía trên không
    private bool isSwinging = false;
    public bool IsSwinging => isSwinging;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;

        // Điều chỉnh độ dày sợi tơ vừa phải
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        lineRenderer.enabled = false;

        if (lineRenderer.material == null || lineRenderer.material.name.Contains("Default"))
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }
    }

    void Update()
    {
        if (!isSwinging)
            ScanForTarget();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isSwinging)
                ReleaseWeb();
            else if (hasTarget)
                ShootWeb();
        }

        if (isSwinging)
            HandleSwingMovement();
    }

    // -------------------------------------------------------
    // Quét phía trên đầu để tìm điểm bám
    // Hướng quét: thẳng lên + nghiêng theo hướng mặt nhân vật
    // -------------------------------------------------------
    void ScanForTarget()
    {
        Vector2 origin = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;

        // Hướng mặt: 1 = phải, -1 = trái
        float facingX = transform.right.x > 0 ? 1f : -1f;

        // Hướng bắn: lên trên nghiêng theo hướng mặt (angle độ so với trục Y)
        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * facingX, Mathf.Cos(rad)).normalized;

        RaycastHit2D hit = attachableLayers != 0
            ? Physics2D.Raycast(origin, scanDir, maxWebDistance, attachableLayers)
            : Physics2D.Raycast(origin, scanDir, maxWebDistance);

        // Loại bỏ nếu hit chính mình
        if (hit.collider != null && hit.collider.gameObject != gameObject
            && Vector2.Distance(origin, hit.point) >= minWebDistance)
        {
            hasTarget = true;
            attachPoint = hit.point;
        }
        else
        {
            hasTarget = false;
        }
    }

    void ShootWeb()
    {
        Vector2 origin = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;

        webJoint = gameObject.AddComponent<DistanceJoint2D>();
        webJoint.connectedAnchor = attachPoint;

        // ĐÃ THÊM ĐIỀU CHỈNH: Cho phép DistanceJoint2D tự động cập nhật hoặc co giãn độ dài linh hoạt theo thực tế
        webJoint.autoConfigureDistance = false;
        webJoint.enableCollision = true;

        lineRenderer.enabled = true;
        isSwinging = true;
        rb.linearDamping = 0.5f;
    }

    void HandleSwingMovement()
    {
        float input = Input.GetAxisRaw("Horizontal");
        if (input != 0)
            rb.AddForce(new Vector2(input * swingForce, 0f));

        // ĐỔI PHÍM: Dùng phím Z (hoặc mũi tên đi lên tùy bạn đổi) để thu ngắn dây, X để thả dài dây ra
        // Bạn có thể thay KeyCode.Z / KeyCode.X bằng bất kỳ phím nào bạn muốn (ví dụ: KeyCode.I, KeyCode.K)
        if (webJoint != null)
        {
            if (Input.GetKey(KeyCode.Z))
            {
                webJoint.distance -= 5f * Time.deltaTime; // Kéo người lại gần điểm bám
            }
            else if (Input.GetKey(KeyCode.X))
            {
                webJoint.distance += 5f * Time.deltaTime; // Thả dây dài ra
            }

            // Giới hạn khoảng cách không bị vượt quá min/max
            webJoint.distance = Mathf.Clamp(webJoint.distance, minWebDistance, maxWebDistance);
        }
    }

    void ReleaseWeb()
    {
        isSwinging = false;
        hasTarget = false;
        rb.linearDamping = 0f;

        if (webJoint != null) { Destroy(webJoint); webJoint = null; }

        lineRenderer.enabled = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSwinging && collision.contacts[0].normal.y > 0.5f)
            ReleaseWeb();
    }

    void LateUpdate()
    {
        if (!isSwinging || webJoint == null) return;

        // Cập nhật điểm đầu của tơ tại vị trí tay nhân vật (webOrigin) và điểm cuối tại điểm bám (attachPoint)
        Vector2 start = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, attachPoint);
    }

    // Vẽ hướng quét trong Scene view để dễ debug
    void OnDrawGizmosSelected()
    {
        Vector2 origin = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;
        float facingX = transform.right.x > 0 ? 1f : -1f;
        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * facingX, Mathf.Cos(rad)).normalized;

        Gizmos.color = hasTarget ? Color.green : Color.yellow;
        Gizmos.DrawRay(origin, scanDir * maxWebDistance);

        if (hasTarget)
            Gizmos.DrawWireSphere(attachPoint, 0.2f);
    }
}