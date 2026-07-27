using UnityEngine;
using System.Collections; // Cần thiết cho Coroutine đếm thời gian làm chậm

/// <summary>
/// Gắn lên Batman.
/// - Mỗi frame quét phía trên đầu (nghiêng theo hướng mặt) tìm điểm bám hoặc Player.
/// - Space: có điểm bám → bắn móc, bắt đầu bám dây. (Hoặc bắn trúng Player để làm chậm)
/// - Phím Z: thu ngắn dây | Phím X: thả dài dây ra.
/// - Space (lần 2 khi đang bám): phóng Batman ra theo lực lấy đà rồi thả móc.
/// - Chạm đất: tự thả móc.
/// </summary>
public class GrabblingHook : MonoBehaviour
{
    [Header("Móc câu")]
    public Transform hookOrigin;               // điểm xuất phát móc (tay Batman)
    public float maxDistance = 12f;
    public float minDistance = 2f;
    public LayerMask hookLayer;                // layer tường/platform có thể bám

    [Header("Góc quét")]
    [Tooltip("Góc lệch so với thẳng đứng khi quét tìm điểm bám (độ)")]
    public float scanAngle = 40f;

    [Header("Điều khiển dây (Giống Spider-Man)")]
    public float climbSpeed = 5f;              // Tốc độ thu ngắn / thả dài dây khi giữ phím Z / X

    [Header("Lực lấy đà (phóng khi nhấn Space lần 2)")]
    public float launchForceX = 10f;           // lực ngang khi phóng
    public float launchForceY = 12f;           // lực dọc khi phóng

    [Header("Prefab hiệu ứng")]
    public GameObject prefab_HookEffect;     // hiệu ứng khi móc bám tường
    public GameObject prefab_HookDart;         // thân móc hiển thị

    [Header("=== TƯƠNG TÁC TẤN CÔNG PLAYER ===")]
    public LayerMask playerLayer;              // Layer của Player đối thủ
    public float slowPercentage = 0.5f;        // Tỷ lệ làm chậm tốc độ (0.5 = giảm 50% tốc độ chạy)
    public float slowDuration = 30f;           // Thời gian làm chậm (30 giây)

    // ---- nội bộ ----
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private GameObject activeDart;
    private Vector2 hookPoint;
    private DistanceJoint2D hookJoint;         // Sử dụng DistanceJoint2D để giữ dây co giãn theo ý muốn giống Spiderman
    private bool hasTarget = false;
    private bool isPulling = false;   // đang bám móc/đu dây
    private float facingX = 1f;       // hướng mặt lúc bắn

    private PlayerInputController inputController;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        inputController = GetComponent<PlayerInputController>();
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
        // Kiểm tra xem nhân vật này có đang gắn AI của Bot hay không
        bool isBotControlled = GetComponent<BatmanBotAI>() != null && GameManager.Instance != null && GameManager.Instance.isPlayer2Bot;

        if (!isPulling)
            ScanForTarget();

        // CHỈ ĐỌC PHÍM TỪ BÀN PHÍM KHI ĐÓ LÀ NGƯỜI CHƠI THẬT
        if (!isBotControlled && inputController != null && inputController.IsSkillPressed)
        {
            if (isPulling)
                Launch();          // đang bám → phóng ra lấy đà
            else if (hasTarget)
                ShootHook();       // có target → bắn móc
        }

        // Chỉ gọi điều chỉnh dây bằng phím Z/X nếu đang bám và KHÔNG phải là Bot (tránh lỗi NullReference)
        if (isPulling && !isBotControlled)
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

        // Ưu tiên quét trúng Player trước nếu có layer playerLayer
        if (playerLayer != 0)
        {
            RaycastHit2D hitPlayer = Physics2D.Raycast(origin, scanDir, maxDistance, playerLayer);
            if (hitPlayer.collider != null && hitPlayer.collider.gameObject != gameObject)
            {
                hasTarget = true;
                hookPoint = hitPlayer.point;
                return;
            }
        }

        // Nếu không trúng Player thì quét tường như bình thường
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
    // Bắn móc (Hỗ trợ cả bám tường lẫn bắn trúng làm chậm Player)
    // -------------------------------------------------------
    void ShootHook()
    {
        Vector2 origin = hookOrigin != null ? (Vector2)hookOrigin.position : (Vector2)transform.position;

        // Đổi tên biến 'fx' thành 'facingDir' để tránh trùng lặp phạm vi (CS0136)
        float facingDir = transform.right.x > 0 ? 1f : -1f;

        float rad = scanAngle * Mathf.Deg2Rad;
        Vector2 scanDir = new Vector2(Mathf.Sin(rad) * facingDir, Mathf.Cos(rad)).normalized;

        // Kiểm tra xem lúc bắn có trúng Player đối phương không
        if (playerLayer != 0)
        {
            RaycastHit2D hitPlayer = Physics2D.Raycast(origin, scanDir, maxDistance, playerLayer);
            if (hitPlayer.collider != null && hitPlayer.collider.gameObject != gameObject)
            {
                // NẾU BẮN TRÚNG PLAYER: Kích hoạt hiệu ứng làm chậm 30s
                PlayerBase enemyController = hitPlayer.collider.GetComponent<PlayerBase>();
                if (enemyController != null)
                {
                    StartCoroutine(ApplySlowEffect(enemyController));
                    Debug.Log("Bắn trúng đối thủ bằng móc câu! Làm chậm trong " + slowDuration + " giây.");
                }
                return; // Thoát hàm, không tạo dây đu tường
            }
        }

        // --- NẾU BẮN TRÚNG TƯỜNG (BÌNH THƯỜNG) ---
        isPulling = true;
        rb.gravityScale = 0.3f;   // giảm gravity khi đang bám móc
        lineRenderer.enabled = true;

        // Tạo DistanceJoint2D để giữ khoảng cách linh hoạt với điểm bám
        hookJoint = gameObject.AddComponent<DistanceJoint2D>();
        hookJoint.connectedAnchor = hookPoint;
        hookJoint.autoConfigureDistance = false;

        hookJoint.distance = Vector2.Distance(origin, hookPoint);
        hookJoint.enableCollision = true;

        // Hiệu ứng tại điểm bám
        if (prefab_HookEffect != null)
        {
            GameObject fxObj = Instantiate(prefab_HookEffect, hookPoint, Quaternion.identity);
            Destroy(fxObj, 0.5f);
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

    // Coroutine xử lý hiệu ứng làm chậm đối thủ trong 30 giây
    IEnumerator ApplySlowEffect(PlayerBase targetPlayer)
    {
        float originalSpeed = targetPlayer.moveSpeed;

        // Giảm tốc độ chạy của đối thủ
        targetPlayer.moveSpeed *= (1f - slowPercentage);

        // Đợi đủ thời gian quy định (30 giây)
        yield return new WaitForSeconds(slowDuration);

        // Hồi phục lại tốc độ ban đầu
        targetPlayer.moveSpeed = originalSpeed;
    }

    // -------------------------------------------------------
    // Điều chỉnh độ dài dây bằng phím Z và X khi đang bám móc
    // -------------------------------------------------------
    void HandleRopeAdjustment()
    {
        if (hookJoint != null)
        {
            // Kiểm tra an toàn inputController trước khi đọc phím
            if (inputController != null)
            {
                // Bấm Z để thu ngắn dây lại, X để thả dài dây ra
                if (inputController.IsRopeInHeld)
                {
                    hookJoint.distance -= climbSpeed * Time.deltaTime;
                }
                else if (inputController.IsRopeOutHeld)
                {
                    hookJoint.distance += climbSpeed * Time.deltaTime;
                }
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

    // ==========================================
    // CÁC HÀM HỖ TRỢ CHO BOT / AI GỌI TRỰC TIẾP
    // ==========================================

    /// <summary>
    /// Cho phép Bot gọi để thực hiện hành động bắn móc hoặc phóng đi (tương đương nhấn Space)
    /// </summary>
    public void BotInput_TriggerSpace()
    {
        if (isPulling)
        {
            Launch(); // Đang bám -> Phóng ra lấy đà bay đi
        }
        else if (hasTarget)
        {
            ShootHook(); // Có mục tiêu -> Bắn móc
        }
    }

    // Các hàm cho phép AI kiểm tra trạng thái hiện tại của móc
    public bool IsPulling()
    {
        return isPulling;
    }

    public bool HasTarget()
    {
        hasTarget = false;
        ScanForTarget(); // Quét lại để cập nhật trạng thái mục tiêu tức thời cho AI
        return hasTarget;
    }
}