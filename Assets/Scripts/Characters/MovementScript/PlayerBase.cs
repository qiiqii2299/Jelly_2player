using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("Di chuyển cơ bản")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    // Hướng di chuyển hiện tại: 1 = phải, -1 = trái
    protected float currentDirection = 1f;

    [Header("Kiểm tra chạm đất")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Bám tường")]
<<<<<<< HEAD
    public float wallCheckDistance = 0.6f;    // khoảng cách raycast sang 2 bên để phát hiện tường
    public float wallSlideSpeed = 5.0f;    // tốc độ trượt xuống khi hết thời gian bám (đã cập nhật)
    public float wallGrabDuration = 0.2f;      // thời gian bám tường tối đa (giây) (đã cập nhật)
    public float wallJumpForceX = 7f;      // lực ngang khi bật khỏi tường
    public float wallJumpForceY = 10f;     // lực dọc khi bật khỏi tường
=======
    public float wallCheckDistance = 0.6f;
    public float wallSlideSpeed    = 1.5f;
    public float wallGrabDuration  = 1f;
    public float wallJumpForceX    = 7f;
    public float wallJumpForceY    = 10f;
>>>>>>> b2b4037 (reworked movement and flying ability)

    // ---- trạng thái nội bộ ----
    protected bool isGrounded;
    protected bool isGrappling = false;
    private   bool canJump     = true;

    // Được FlyCharacter set để PlayerBase nhường quyền điều khiển
    public bool IsFlying { get; set; } = false;

    // Flag bất động — FreezeEffect set để chặn toàn bộ input/movement
    public bool IsFrozen { get; private set; } = false;

<<<<<<< HEAD
    private bool isOnWall = false;   // đang chạm tường
    private bool isWallGrabbing = false;  // đang bám cứng (chưa trượt)
    private bool isWallSliding = false;  // đang trượt xuống từ từ
    private float wallGrabTimer = 0f;
    private float wallSide = 0f;      // 1 = tường bên phải, -1 = tường bên trái

    protected Rigidbody2D rb;
    protected Animator anim;
=======
    private bool  isOnWall       = false;
    private bool  isWallGrabbing = false;
    private bool  isWallSliding  = false;
    private float wallGrabTimer  = 0f;
    private float wallSide       = 0f;   // 1 = tường phải, -1 = tường trái
>>>>>>> b2b4037 (reworked movement and flying ability)

    protected Rigidbody2D           rb;
    protected Animator              anim;
    protected PlayerInputController inputController;

<<<<<<< HEAD
    // --- HÀM KIỂM TRA NHANH XEM NHÂN VẬT NÀY CÓ PHẢI LÀ BOT KHÔNG ---
    protected bool IsBotControlled()
    {
        return GetComponent<BatmanBotAI>() != null && GameManager.Instance != null && GameManager.Instance.isPlayer2Bot;
    }

=======
    // -------------------------------------------------------
>>>>>>> b2b4037 (reworked movement and flying ability)
    virtual protected void Start()
    {
        rb              = GetComponent<Rigidbody2D>();
        anim            = GetComponent<Animator>();
        inputController = GetComponent<PlayerInputController>();
    }

    // -------------------------------------------------------
    virtual protected void Update()
    {
        if (IsFrozen) return;

        CheckGrounded();
        CheckWall();

        HandleSkillInput();

        // FlyCharacter đang bay — nhường quyền hoàn toàn
        if (IsFlying) return;

        if (isGrappling) return;

        // Nếu là Bot thì bỏ qua các hàm đọc phím điều khiển thủ công của người chơi
        if (IsBotControlled()) return;

        HandleWallGrab();
        HandleMovement();
        HandleJump();
    }

    // -------------------------------------------------------
    /// <summary>Trả về trạng thái chạm đất — FlyCharacter dùng để biết khi nào hạ cánh.</summary>
    public bool IsGrounded() => isGrounded;

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

            ExitWallGrab();
        }
    }

    // -------------------------------------------------------
    void CheckWall()
    {
        if (isGrounded) { isOnWall = false; return; }

        LayerMask mask = groundLayer != 0 ? groundLayer : Physics2D.AllLayers;
        bool wallRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, mask);
        bool wallLeft  = Physics2D.Raycast(transform.position, Vector2.left,  wallCheckDistance, mask);

        if      (wallRight) { isOnWall = true; wallSide =  1f; }
        else if (wallLeft)  { isOnWall = true; wallSide = -1f; }
        else                { isOnWall = false; }

        if (!isOnWall) ExitWallGrab();
    }

    // -------------------------------------------------------
    void HandleWallGrab()
    {
<<<<<<< HEAD
        // Bắt đầu bám tường khi chạm tường và đang rơi/bay xuống
=======
        // Bắt đầu bám tường khi chạm tường và đang rơi
>>>>>>> b2b4037 (reworked movement and flying ability)
        if (isOnWall && !isGrounded && rb.linearVelocity.y <= 0)
        {
            if (!isWallGrabbing && !isWallSliding)
            {
                isWallGrabbing = true;
                wallGrabTimer  = 0f;
            }
        }

        if (isWallGrabbing)
        {
            wallGrabTimer += Time.deltaTime;

<<<<<<< HEAD
            // Cho phép trượt nhẹ từ từ ngay từ đầu để tạo cảm giác bám ma sát
            rb.linearVelocity = new Vector2(0f, -wallSlideSpeed * 0.5f);
            rb.gravityScale = 0.1f;

            // Hết thời gian bám ngắn -> chuyển sang trượt nhanh hơn
=======
            // Khóa di chuyển, giữ cố định trên tường
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale   = 0f;

>>>>>>> b2b4037 (reworked movement and flying ability)
            if (wallGrabTimer >= wallGrabDuration)
            {
                isWallGrabbing = false;
                isWallSliding  = true;
                rb.gravityScale = 1f;
            }
        }

        if (isWallSliding)
        {
<<<<<<< HEAD
            // Trượt xuống với tốc độ trượt chuẩn
=======
            // Trượt xuống từ từ, không cho di chuyển ngang
>>>>>>> b2b4037 (reworked movement and flying ability)
            rb.linearVelocity = new Vector2(0f, -wallSlideSpeed);
        }
    }
    // -------------------------------------------------------
    void ExitWallGrab()
    {
        if (isWallGrabbing || isWallSliding)
        {
            isWallGrabbing  = false;
            isWallSliding   = false;
            rb.gravityScale = 1f;
            canJump         = true;
        }
    }

    // -------------------------------------------------------
    protected void HandleMovement()
    {
<<<<<<< HEAD
        if (IsBotControlled()) return; // Chặn tuyệt đối nếu là Bot
=======
        // Khi đang bám/trượt tường — khóa hoàn toàn di chuyển ngang
>>>>>>> b2b4037 (reworked movement and flying ability)
        if (isWallGrabbing || isWallSliding) return;

        // Kiểm tra input trái/phải để đổi hướng
        if (inputController != null)
        {
            if      (inputController.IsRightHeld) currentDirection =  1f;
            else if (inputController.IsLeftHeld)  currentDirection = -1f;
        }
        else
        {
            if      (Input.GetKey(KeyCode.RightArrow)) currentDirection =  1f;
            else if (Input.GetKey(KeyCode.LeftArrow))  currentDirection = -1f;
        }

        // Tự động di chuyển theo hướng hiện tại
        rb.linearVelocity = new Vector2(currentDirection * moveSpeed, rb.linearVelocity.y);

        // Flip sprite
        if      (currentDirection > 0f) transform.rotation = Quaternion.Euler(0f,   0f, 0f);
        else if (currentDirection < 0f) transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // -------------------------------------------------------
    protected void HandleJump()
    {
<<<<<<< HEAD
        if (IsBotControlled()) return; // Chặn tuyệt đối nếu là Bot

        bool jumpPressed = inputController != null ? inputController.IsJumpPressed : Input.GetKeyDown(KeyCode.UpArrow);
=======
        bool jumpPressed = inputController != null
            ? inputController.IsJumpPressed
            : Input.GetKeyDown(KeyCode.UpArrow);
>>>>>>> b2b4037 (reworked movement and flying ability)

        if (!jumpPressed) return;

        // Nhảy khỏi tường — input nhảy vẫn hoạt động dù đang bám
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

    // -------------------------------------------------------
    public void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;

        if (frozen && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale   = 0f;
            rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }
        else if (!frozen && rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}
