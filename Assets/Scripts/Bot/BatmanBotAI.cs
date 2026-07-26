using UnityEngine;

public class BatmanBotAI : MonoBehaviour
{
    private GrabblingHook hookScript;
    private Rigidbody2D rb;

    [Header("Cấu hình hành vi di chuyển của Bot")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f; // Lực nhảy dự phòng khi gặp vật cản

    [Header("Cấu hình thời gian hành vi của Bot")]
    public float timeToReleaseHook = 1.0f; // Thời gian đu dây
    private float hookTimer = 0f;

    [Header("Cấu hình cảm biến thông minh")]
    public float wallDetectDistance = 1.0f; // Khoảng cách phát hiện tường phía trước
    public LayerMask obstacleLayer;         // Layer chướng ngại vật/tường

    private Vector2 lastPosition;
    private float stuckTimer = 0f;

    void Start()
    {
        hookScript = GetComponent<GrabblingHook>();
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Kiểm tra an toàn chế độ Bot
        if (GameManager.Instance == null || !GameManager.Instance.isPlayer2Bot)
        {
            return;
        }

        if (hookScript == null || rb == null) return;

        bool hasTarget = hookScript.HasTarget();
        bool isPulling = hookScript.IsPulling();

        if (!isPulling)
        {
            // --- TỰ ĐỘNG DI CHUYỂN TIẾN VỀ PHÍA TRƯỚC ---
            float moveDir = 1f; // Hướng di chuyển tiến lên trước
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

            // Xoay mặt nhân vật theo hướng chạy
            if (moveDir > 0) transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (moveDir < 0) transform.rotation = Quaternion.Euler(0, 180, 0);

            // --- CẢM BIẾN THÔNG MINH 1: PHÁT HIỆN VẬT CẢN PHÍA TRƯỚC ---
            Vector2 rayOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
            RaycastHit2D frontWall = Physics2D.Raycast(rayOrigin, Vector2.right * moveDir, wallDetectDistance, obstacleLayer);

            if (frontWall.collider != null && !hasTarget)
            {
                // Nếu trước mặt vướng tường mà không có điểm móc, tự động nhảy lên để vượt qua
                if (Mathf.Abs(rb.linearVelocity.y) < 0.05f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                }
            }

            // --- CẢM BIẾN THÔNG MINH 2: CHỐNG KẸT (STUCK DETECTION) ---
            // Kiểm tra xem bot có đang bị đứng im một chỗ trong khi vẫn muốn di chuyển không
            if (Vector2.Distance((Vector2)transform.position, lastPosition) < 0.02f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 0.6f) // Nếu kẹt quá 0.6 giây, tự kích hoạt nhảy/bắn móc để phá thế kẹt
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 1.2f);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;

            // --- TỰ ĐỘNG BẮN MÓC KHI CÓ MỤC TIÊU ---
            if (hasTarget)
            {
                hookScript.BotInput_TriggerSpace();
                hookTimer = 0f;
            }
        }
        else
        {
            // --- KHI ĐANG ĐU DÂY: THÔNG MINH HÓA THỜI GIAN PHÓNG ĐI ---
            hookTimer += Time.deltaTime;

            // Nếu bot đã văng qua nửa quãng đường hoặc hết thời gian cấu hình, tự động phóng đi để lấy đà bay xa
            if (hookTimer >= timeToReleaseHook || rb.linearVelocity.y > 2f)
            {
                hookScript.BotInput_TriggerSpace();
            }
        }
    }
}