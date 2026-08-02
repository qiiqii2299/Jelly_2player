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

    [Header("Bám tường (Đã tối ưu)")]
    public float wallCheckDistance = 0.6f;
    public float wallSlideSpeed = 1.5f; // Tốc độ trượt xuống
    // Đã bỏ wallGrabDuration vì không cần bắt người chơi chờ nữa
    public float wallJumpForceX = 7f;
    public float wallJumpForceY = 10f;

    // ---- trạng thái nội bộ ----
    protected bool isGrounded;
    protected bool isGrappling = false;
    private bool canJump = true;

    // Được FlyCharacter set để PlayerBase nhường quyền điều khiển
    public bool IsFlying { get; set; } = false;

    // Flag bất động — FreezeEffect set để chặn toàn bộ input/movement
    public bool IsFrozen { get; private set; } = false;

    private bool isOnWall = false;
    private bool isWallSliding = false;
    private float wallSide = 0f;   // 1 = tường phải, -1 = tường trái

    protected Rigidbody2D rb;
    protected Animator anim;
    protected PlayerInputController inputController;

    // -------------------------------------------------------
    virtual protected void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
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

        HandleWallSlide();
        HandleMovement();
        HandleJump();
    }

    // -------------------------------------------------------
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

            isWallSliding = false;
        }
    }

    // -------------------------------------------------------
    void CheckWall()
    {
        if (isGrounded) { isOnWall = false; return; }

        LayerMask mask = groundLayer != 0 ? groundLayer : Physics2D.AllLayers;
        bool wallRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, mask);
        bool wallLeft = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, mask);

        if (wallRight) { isOnWall = true; wallSide = 1f; }
        else if (wallLeft) { isOnWall = true; wallSide = -1f; }
        else { isOnWall = false; }
    }

    // -------------------------------------------------------
    // THAY THẾ: Cơ chế Trượt Tường mượt mà hơn
    // -------------------------------------------------------
    void HandleWallSlide()
    {
        // Kích hoạt trượt tường khi chạm tường, không chạm đất và đang có xu hướng rơi
        if (isOnWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;

            // Hãm tốc độ rơi xuống bằng wallSlideSpeed (Mathf.Max giúp nhân vật không rơi quá nhanh)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
        else
        {
            isWallSliding = false;
        }
    }

    // -------------------------------------------------------
    protected void HandleMovement()
    {
        // 1. Nhận input thực tế
        float inputX = 0f;
        if (inputController != null)
        {
            if (inputController.IsRightHeld) inputX = 1f;
            else if (inputController.IsLeftHeld) inputX = -1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.RightArrow)) inputX = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow)) inputX = -1f;
        }

        // Cập nhật hướng nếu có phím bấm
        if (inputX != 0) currentDirection = inputX;

        // 2. Xử lý logic bám tường và thoát tường
        if (isWallSliding)
        {
            // Nếu người chơi bấm hướng NGƯỢC LẠI với tường -> Thả tay rớt xuống
            if (inputX == -wallSide)
            {
                isWallSliding = false;
            }
            else
            {
                // Đang trượt tường -> Khóa trục X để bám sát tường, tránh bị vấp
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return; // Dừng logic di chuyển ngang ở đây
            }
        }

        // 3. Di chuyển ngang bình thường
        rb.linearVelocity = new Vector2(currentDirection * moveSpeed, rb.linearVelocity.y);
    }

    // -------------------------------------------------------
    protected void HandleJump()
    {
        bool jumpPressed = inputController != null
            ? inputController.IsJumpPressed
            : Input.GetKeyDown(KeyCode.UpArrow);

        if (!jumpPressed) return;

        // 1. Nếu đang trượt tường (hoặc vừa ôm tường) -> Wall Jump
        if (isWallSliding || isOnWall)
        {
            isWallSliding = false;

            // Nảy theo hướng ngược lại với bức tường
            rb.linearVelocity = new Vector2(-wallSide * wallJumpForceX, wallJumpForceY);

            // Cập nhật lại hướng di chuyển hiện tại để nhân vật chạy tiếp ra xa tường
            currentDirection = -wallSide;
            canJump = false;
            return;
        }

        // 2. Nhảy bình thường từ mặt đất
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
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else if (!frozen && rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
}