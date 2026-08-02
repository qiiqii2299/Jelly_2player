using UnityEngine;

/// <summary>
/// Gắn lên Spiderman.
/// - Nhận phím trực tiếp.
/// - Đã gộp cơ chế 3 Sprites (Trái, Phải, Chính) giống Hulk.
/// - Đã điều chỉnh hướng quét tơ (Radar) đi theo hướng Sprite thực tế.
/// </summary>
public class WebSwing : MonoBehaviour
{
    [Header("Cài Đặt Phím Bấm")]
    public KeyCode webKey = KeyCode.Space;
    public KeyCode climbUpKey = KeyCode.W;
    public KeyCode climbDownKey = KeyCode.S;

    [Header("Hình Ảnh (Sprites)")]
    [Tooltip("Ảnh mặt chính (đứng im hoặc ở điểm rơi cao nhất)")]
    public Sprite frontSprite;
    [Tooltip("Ảnh khi đi/đu sang TRÁI")]
    public Sprite leftSprite;
    [Tooltip("Ảnh khi đi/đu sang PHẢI")]
    public Sprite rightSprite;

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
    public float climbSpeed = 5f;           // Tốc độ leo lên/xuống dây

    // ---- nội bộ ----
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private DistanceJoint2D webJoint;
    private Vector2 attachPoint;
    private bool hasTarget = false;
    private bool isSwinging = false;

    // Các biến phục vụ hiển thị hình ảnh
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private float facingDir = 1f; // 1 = Phải, -1 = Trái (Lưu hướng để quét tơ chính xác)

    public bool IsSwinging => isSwinging;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        // Cài đặt mặt mặc định
        if (frontSprite != null) spriteRenderer.sprite = frontSprite;

        // Thiết lập LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
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
        // Liên tục cập nhật hình ảnh Trái/Phải/Chính
        UpdateSpriteAppearance();

        if (!isSwinging)
            ScanForTarget();

        // Xử lý Input Bắn / Thả Tơ
        if (Input.GetKeyDown(webKey))
        {
            if (isSwinging) ReleaseWeb();
            else if (hasTarget) ShootWeb();
        }

        if (isSwinging)
            HandleSwingMovement();
    }

    // -------------------------------------------------------
    // QUẢN LÝ HÌNH ẢNH (ĐỔI MẶT TƯƠNG TỰ HULK)
    // -------------------------------------------------------
    void UpdateSpriteAppearance()
    {
        // 1. Khóa các tác động lật ngược nhân vật từ hệ thống khác
        spriteRenderer.flipX = false;
        Vector3 fixedScale = transform.localScale;
        fixedScale.x = Mathf.Abs(originalScale.x);
        transform.localScale = fixedScale;

        // 2. Lấy vận tốc thực tế để đổi ảnh cho mượt (kể cả khi đang bị văng đi trên tơ)
        float speedX = rb.linearVelocity.x;

        if (speedX < -0.1f)
        {
            if (leftSprite != null) spriteRenderer.sprite = leftSprite;
            facingDir = -1f; // Nhớ hướng này để quét tơ
        }
        else if (speedX > 0.1f)
        {
            if (rightSprite != null) spriteRenderer.sprite = rightSprite;
            facingDir = 1f;  // Nhớ hướng này để quét tơ
        }
        else
        {
            // Vận tốc = 0 (Đứng im) -> Trở về mặt chính
            if (frontSprite != null) spriteRenderer.sprite = frontSprite;

            // Xử lý thêm: Nếu đứng im nhưng bấm phím, vẫn cho phép đổi hướng Radar
            float inputX = Input.GetAxisRaw("Horizontal");
            if (inputX < 0) facingDir = -1f;
            else if (inputX > 0) facingDir = 1f;
        }
    }

    // -------------------------------------------------------
    // Quét phía trên đầu để tìm điểm bám
    // -------------------------------------------------------
    void ScanForTarget()
    {
        Vector2 origin = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;

        // CẬP NHẬT: Sử dụng facingDir (do UpdateSpriteAppearance tính toán) thay vì transform.right
        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * facingDir, Mathf.Cos(rad)).normalized;

        RaycastHit2D hit = attachableLayers != 0
            ? Physics2D.Raycast(origin, scanDir, maxWebDistance, attachableLayers)
            : Physics2D.Raycast(origin, scanDir, maxWebDistance);

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
        webJoint.autoConfigureDistance = false;

        webJoint.distance = Vector2.Distance(origin, attachPoint);

        webJoint.enableCollision = true;

        lineRenderer.enabled = true;
        isSwinging = true;
        rb.linearDamping = 0.5f;
    }

    void HandleSwingMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        if (inputX != 0)
            rb.AddForce(new Vector2(inputX * swingForce, 0f));

        if (webJoint != null)
        {
            bool pullingUp = Input.GetKey(climbUpKey) || Input.GetKey(KeyCode.UpArrow);
            bool pullingDown = Input.GetKey(climbDownKey) || Input.GetKey(KeyCode.DownArrow);

            if (pullingUp)
                webJoint.distance -= climbSpeed * Time.deltaTime;
            else if (pullingDown)
                webJoint.distance += climbSpeed * Time.deltaTime;

            webJoint.distance = Mathf.Clamp(webJoint.distance, minWebDistance, maxWebDistance);
        }
    }

    void ReleaseWeb()
    {
        isSwinging = false;
        hasTarget = false;
        rb.linearDamping = 0f;

        if (webJoint != null)
        {
            Destroy(webJoint);
            webJoint = null;
        }

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

        Vector2 start = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, attachPoint);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origin = webOrigin != null ? (Vector2)webOrigin.position : (Vector2)transform.position;

        // Sử dụng facingDir để vẽ tia đúng hướng trong Editor
        float currentFacingDir = facingDir != 0 ? facingDir : 1f;
        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * currentFacingDir, Mathf.Cos(rad)).normalized;

        Gizmos.color = hasTarget ? Color.green : Color.yellow;
        Gizmos.DrawRay(origin, scanDir * maxWebDistance);

        if (hasTarget)
            Gizmos.DrawWireSphere(attachPoint, 0.2f);
    }
}