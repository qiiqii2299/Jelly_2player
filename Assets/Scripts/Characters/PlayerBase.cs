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
    private bool canJump = true;

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

        if (groundCheck != null && groundLayer != 0)
        {
            // Dùng OverlapCircle nếu đã gán groundCheck và groundLayer
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // Fallback: coi là đang đứng đất khi vận tốc y gần 0 và đang rơi/đứng yên
            isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;
        }

        if (!wasGrounded && isGrounded)
        {
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    protected void HandleMovement()
    {
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;

        // Cập nhật hướng nhìn khi có phím bấm
        if (moveInput > 0) currentDirection = 1f;
        else if (moveInput < 0) currentDirection = -1f;

        float currentSpeed = moveInput * moveSpeed;

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        // Lật mặt
        if (currentDirection > 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (currentDirection < 0)
            transform.rotation = Quaternion.Euler(0, 180, 0);
    }
    protected void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            canJump = false;
        }
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        // Cho phép nhảy lại khi chạm bất kỳ vật thể nào phía dưới
        if (collision.contacts[0].normal.y > 0.5f)
            canJump = true;
    }

    virtual protected void HandleSkillInput() { }
}