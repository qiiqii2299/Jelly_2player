using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("Di chuyển cơ bản")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public bool isAutoRun = true; // Bật/tắt chế độ tự động chạy

    // BIẾN MỚI: Lưu giữ hướng chạy cuối cùng (1 = Phải, -1 = Trái)
    protected float currentDirection = 1f;

    [Header("Kiểm tra chạm đất")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    protected bool isGrounded;
    protected bool isGrappling = false;

    protected Rigidbody2D rb;
    protected Animator anim;

    virtual protected void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    virtual protected void Update()
    {
        CheckGrounded();

        // Ưu tiên nhận nút kỹ năng (chuột) trước
        HandleSkillInput();

        // NẾU ĐANG ĐU DÂY: Ngừng chạy và nhảy, nhường 100% cho vật lý
        if (isGrappling) return;

        HandleMovement();
        HandleJump();
    }

    protected void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        // Kiểm tra xem nhân vật có đang đứng trên đất không
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // VỪA CHẠM ĐẤT: Triệt tiêu ngay lập tức lực rơi (y) để ngăn triệt để hiện tượng nảy/tưng
        if (!wasGrounded && isGrounded)
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    protected void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 1. CẬP NHẬT HƯỚNG ĐI DỰA TRÊN PHÍM BẤM
        if (moveInput > 0)
        {
            currentDirection = 1f;
        }
        else if (moveInput < 0)
        {
            currentDirection = -1f;
        }

        // 2. TÍNH TOÁN TỐC ĐỘ 
        float currentSpeed = 0f;
        if (isAutoRun)
        {
            currentSpeed = currentDirection * moveSpeed;
        }
        else
        {
            currentSpeed = moveInput * moveSpeed;
        }

        // 3. ÁP DỤNG LỰC DI CHUYỂN
        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        // 4. LẬT MẶT BẰNG CÁCH XOAY TRỤC Y (Tránh lỗi Animator đè Scale)
        if (currentDirection > 0)
        {
            // Quay mặt sang phải (Mặc định)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (currentDirection < 0)
        {
            // Quay mặt sang trái (Xoay 180 độ)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
    protected void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    virtual protected void HandleSkillInput() { }
}