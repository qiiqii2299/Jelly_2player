using UnityEngine;

/// <summary>
/// Xử lý di chuyển và nhảy cho Bot AI.
/// Tương đương Double_Jump của người chơi nhưng hoàn toàn độc lập —
/// không đọc Input bàn phím, chỉ nhận lệnh từ AIBrain.
/// 
/// Gắn component này lên prefab bot thay cho Double_Jump.
/// </summary>
public class BotMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 5f;

    [Header("Jump")]
    public int maxJump = 2;

    [Header("Ground Detection")]
    [Tooltip("Layer mặt đất — phải khớp với tag Ground")]
    public LayerMask groundLayer;
    [Tooltip("Độ dài ray kiểm tra đất, chỉnh tăng nếu bot cao")]
    public float groundCheckLength = 0.65f;

    // Đọc bởi AIBrain và SpiderManBotSkill để biết bot đang đứng đất không
    // Dùng Raycast thay vì chỉ dựa vào collision event để tránh false negative
    public bool isGrounded { get; private set; } = false;
    [HideInInspector] public int currentJump = 0;

    // SpiderManBotSkill set true khi đang swing để BotMovement không override velocity
    [HideInInspector] public bool isSwinging = false;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Raycast kiểm tra đất mỗi frame — đáng tin hơn OnCollisionEnter/Exit
        CheckGroundRaycast();
    }

    void CheckGroundRaycast()
    {
        // Bắn ray từ chân bot xuống dưới
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            groundCheckLength,
            groundLayer);

        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        // Reset jump count khi vừa chạm đất
        if (!wasGrounded && isGrounded)
            currentJump = 0;
    }

    // -------------------------------------------------------
    // API cho AIBrain gọi
    // -------------------------------------------------------

    /// Luôn chạy về phía trước (hướng +X)
    public void MoveForward()
    {
        if (isSwinging) return; // swing tự có momentum
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
        transform.localScale = new Vector3(1, 1, 1);
    }

    /// Nhảy nếu còn lượt nhảy
    public void Jump()
    {
        if (currentJump >= maxJump) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        currentJump++;
    }

    /// Nhảy sau khi thả tơ (gọi bởi SpiderManBotSkill)
    public void ReleaseJump(float force)
    {
        rb.AddForce(new Vector2(rb.linearVelocity.x, force), ForceMode2D.Impulse);
        currentJump = 1; // giống ResetJumpCount() của Double_Jump gốc
    }

    public Rigidbody2D GetRb() => rb;
}
