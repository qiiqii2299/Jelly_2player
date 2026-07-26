using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("Di chuyển cơ bản")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public bool isAutoRun = false;

    protected float currentDirection = 1f;

    [Header("Kiểm tra chạm đất")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Bám tường")]
    public float wallCheckDistance = 0.6f;    // khoảng cách raycast sang 2 bên để phát hiện tường
    public float wallSlideSpeed = 1.5f;    // tốc độ trượt xuống khi hết thời gian bám
    public float wallGrabDuration = 1f;      // thời gian bám tường tối đa (giây)
    public float wallJumpForceX = 7f;      // lực ngang khi bật khỏi tường
    public float wallJumpForceY = 10f;     // lực dọc khi bật khỏi tường

    // ---- trạng thái nội bộ ----
    protected bool isGrounded;
    protected bool isGrappling = false;
    private bool canJump = true;

    private bool isOnWall = false;   // đang chạm tường
    private bool isWallGrabbing = false;  // đang bám cứng (chưa trượt)
    private bool isWallSliding = false;  // đang trượt xuống từ từ
    private float wallGrabTimer = 0f;
    private float wallSide = 0f;      // 1 = tường bên phải, -1 = tường bên trái

    protected Rigidbody2D rb;
    protected Animator anim;

    protected PlayerInputController inputController;

    // --- HÀM KIỂM TRA NHANH XEM NHÂN VẬT NÀY CÓ PHẢI LÀ BOT KHÔNG ---
    protected bool IsBotControlled()
    {
        return GetComponent<BatmanBotAI>() != null && GameManager.Instance != null && GameManager.Instance.isPlayer2Bot;
    }

    virtual protected void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        inputController = GetComponent<PlayerInputController>();
    }

    virtual protected void Update()
    {
        CheckGrounded();
        CheckWall();

        HandleSkillInput();

        if (isGrappling) return;

        // Nếu là Bot thì bỏ qua các hàm đọc phím điều khiển thủ công của người chơi
        if (IsBotControlled()) return;

        HandleWallGrab();
        HandleMovement();
        HandleJump();
    }

    // -------------------------------------------------------
    protected void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        if (groundCheck != null && groundLayer != 0)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        else
            isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;

        if (!wasGrounded && isGrounded)
        {
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // Reset wall grab khi chạm đất
            ExitWallGrab();
        }
    }

    // -------------------------------------------------------
    void CheckWall()
    {
        if (isGrounded) { isOnWall = false; return; }

        // Raycast sang 2 bên để phát hiện tường
        LayerMask mask = groundLayer != 0 ? groundLayer : Physics2D.AllLayers;
        bool wallRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, mask);
        bool wallLeft = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, mask);

        if (wallRight) { isOnWall = true; wallSide = 1f; }
        else if (wallLeft) { isOnWall = true; wallSide = -1f; }
        else { isOnWall = false; }

        // Rời tường → thoát trạng thái
        if (!isOnWall) ExitWallGrab();
    }

    // -------------------------------------------------------
    void HandleWallGrab()
    {
        // Bắt đầu bám tường khi chạm tường và đang rơi/bay
        if (isOnWall && !isGrounded && rb.linearVelocity.y <= 0)
        {
            if (!isWallGrabbing && !isWallSliding)
            {
                isWallGrabbing = true;
                wallGrabTimer = 0f;
            }
        }

        if (isWallGrabbing)
        {
            wallGrabTimer += Time.deltaTime;

            // Giữ nhân vật cố định trên tường
            rb.linearVelocity = new Vector2(0f, 0f);
            rb.gravityScale = 0f;

            // Hết thời gian bám → bắt đầu trượt
            if (wallGrabTimer >= wallGrabDuration)
            {
                isWallGrabbing = false;
                isWallSliding = true;
                rb.gravityScale = 1f;
            }
        }

        if (isWallSliding)
        {
            // Trượt xuống từ từ
            rb.linearVelocity = new Vector2(0f, -wallSlideSpeed);
        }
    }

    // -------------------------------------------------------
    void ExitWallGrab()
    {
        if (isWallGrabbing || isWallSliding)
        {
            isWallGrabbing = false;
            isWallSliding = false;
            rb.gravityScale = 1f;
            canJump = true;   // cho phép nhảy lại khi chạm đất
        }
    }

    // -------------------------------------------------------
    protected void HandleMovement()
    {
        if (IsBotControlled()) return; // Chặn tuyệt đối nếu là Bot
        if (isWallGrabbing || isWallSliding) return;

        float moveInput = 0f;

        // Đọc phím trái/phải thông qua inputController thay vì check cứng KeyCode
        if (inputController != null)
        {
            if (inputController.IsRightHeld) moveInput = 1f;
            else if (inputController.IsLeftHeld) moveInput = -1f;
        }
        else
        {
            // Fallback nếu quên gắn controller
            if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;
        }

        if (isAutoRun && moveInput == 0)
        {
            moveInput = currentDirection;
        }

        if (moveInput > 0) currentDirection = 1f;
        else if (moveInput < 0) currentDirection = -1f;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (currentDirection > 0) transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (currentDirection < 0) transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    // -------------------------------------------------------
    protected void HandleJump()
    {
        if (IsBotControlled()) return; // Chặn tuyệt đối nếu là Bot

        bool jumpPressed = inputController != null ? inputController.IsJumpPressed : Input.GetKeyDown(KeyCode.UpArrow);

        if (!jumpPressed) return;

        if (isWallGrabbing || isWallSliding)
        {
            ExitWallGrab();
            rb.linearVelocity = new Vector2(-wallSide * wallJumpForceX, wallJumpForceY);
            canJump = false;
            return;
        }

        if (canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            canJump = false;
        }
    }

    // -------------------------------------------------------
    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            canJump = true;
    }

    virtual protected void HandleSkillInput() { }
}