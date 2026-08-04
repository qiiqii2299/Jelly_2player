using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn kèm với PlayerBase lên nhân vật có khả năng bay (Superman & Ironman).
/// Khóa cứng Front Sprite khi bay và tự động trả lại quyền quản lý hình ảnh khi hạ cánh.
/// </summary>
[RequireComponent(typeof(PlayerBase))]
public class FlyCharacter : MonoBehaviour
{
    [Header("Thông số Bay")]
    public float flySpeed = 6f;
    public float flyVerticalSpeed = 5f;
    public float flyDuration = 10f;
    public float fallDuration = 3f;
    public float liftForce = 8f;
    public float gentleFallSpeed = 2f;
    public float flyCooldown = 5f;

    [Header("Hiệu ứng Bay & Hình ảnh (Dành riêng cho Ironman)")]
    [Tooltip("Kéo Prefab hiệu ứng phản lực/cánh (đang làm con của Ironman) vào đây.")]
    public GameObject jetpackEffect;

    [Tooltip("Ảnh hiển thị cố định (Front Sprite) khi bay lên.")]
    public Sprite flyingFrontSprite;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Sprite originalSprite;

    // ---- trạng thái ----
    public bool IsFlying { get; private set; } = false;
    public bool IsFalling { get; private set; } = false;

    /// <summary>Thời gian hồi chiêu còn lại. 0 = sẵn sàng.</summary>
    public float CooldownRemaining { get; private set; } = 0f;

    private float flyTimer = 0f;
    private float fallTimer = 0f;

    private PlayerBase playerBase;
    private Rigidbody2D rb;
    private PlayerInputController inputController; // Giữ lại cho Superman

    void Start()
    {
        playerBase = GetComponent<PlayerBase>();
        rb = GetComponent<Rigidbody2D>();
        inputController = GetComponent<PlayerInputController>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }

        // Đảm bảo lúc mới bắt đầu game thì hiệu ứng bay được tắt đi
        if (jetpackEffect != null)
        {
            jetpackEffect.SetActive(false);
        }
    }

    void Update()
    {
        if (playerBase.IsFrozen) return;

        if (!IsFlying && !IsFalling)
        {
            // Đếm hồi chiêu
            if (CooldownRemaining > 0f)
            {
                CooldownRemaining -= Time.deltaTime;
                if (CooldownRemaining <= 0f)
                {
                    CooldownRemaining = 0f;
                    Debug.Log("[FlyCharacter] Skill bay sẵn sàng!");
                }
                return;
            }

            // Lắng nghe skill phụ từ hệ thống Input chung
            bool secondaryPressed = inputController != null
                ? inputController.IsSecondarySkillPressed
                : (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X));

            if (secondaryPressed)
                StartFlying();

            return;
        }

        if (IsFlying)
        {
            HandleFlyMovement();
            TickFlyTimer();

            // Bấm lại skill phụ → chuyển sang rơi ngay
            bool secondaryPressed = inputController != null
                ? inputController.IsSecondarySkillPressed
                : (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X));

            if (secondaryPressed)
                BeginGentleFall();
        }
        else if (IsFalling)
        {
            HandleGentleFall();
        }
    }

    void StartFlying()
    {
        IsFlying = true;
        IsFalling = false;
        flyTimer = 0f;

        playerBase.IsFlying = true;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, liftForce);

        // 1. Bật object tên lửa/phản lực trên lưng lên
        if (jetpackEffect != null)
        {
            jetpackEffect.SetActive(true);
        }

        // 2. Lấy Front Sprite từ script MissileSkill để hiển thị lúc bay
        if (spriteRenderer != null)
        {
            if (animator != null) animator.enabled = false;

            // Tự động tìm component MissileSkill trên nhân vật và lấy ảnh Front của nó
            MissileSkill missileSkill = GetComponent<MissileSkill>();
            if (missileSkill != null && missileSkill.frontSprite != null)
            {
                spriteRenderer.sprite = missileSkill.frontSprite;
            }

            spriteRenderer.color = Color.white;
        }

        Debug.Log("[FlyCharacter] Bắt đầu bay, bật tên lửa và khóa Front Sprite!");
    }
    /// <summary>
    /// Cho phép các Controller bên ngoài (như SupermanController) gọi kích hoạt bay trực tiếp.
    /// </summary>
    public void TriggerFlightExternal()
    {
        if (!IsFlying && !IsFalling && CooldownRemaining <= 0f)
        {
            StartFlying();
        }
    }

    void HandleFlyMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (inputController != null)
        {
            if (inputController.IsRightHeld) horizontal = 1f;
            else if (inputController.IsLeftHeld) horizontal = -1f;

            if (inputController.IsUpHeld) vertical = 1f;
            else if (inputController.IsDownHeld) vertical = -1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) horizontal = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) horizontal = -1f;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) vertical = 1f;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) vertical = -1f;
        }

        rb.linearVelocity = new Vector2(horizontal * flySpeed, vertical * flyVerticalSpeed);
    }

    void TickFlyTimer()
    {
        flyTimer += Time.deltaTime;

        if (flyTimer >= flyDuration)
            BeginGentleFall();
    }

    void BeginGentleFall()
    {
        IsFlying = false;
        IsFalling = true;
        fallTimer = 0f;
        rb.gravityScale = 0f;
        Debug.Log("[FlyCharacter] Chuyển sang rơi từ từ...");
    }

    void HandleGentleFall()
    {
        fallTimer += Time.deltaTime;

        float horizontal = 0f;
        if (inputController != null)
        {
            if (inputController.IsRightHeld) horizontal = 1f;
            else if (inputController.IsLeftHeld) horizontal = -1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) horizontal = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) horizontal = -1f;
        }

        rb.linearVelocity = new Vector2(horizontal * flySpeed, -gentleFallSpeed);

        bool hitGround = playerBase.IsGrounded();
        if (fallTimer >= fallDuration || hitGround)
            StopFlying();
    }

    void StopFlying()
    {
        IsFlying = false;
        IsFalling = false;
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        playerBase.IsFlying = false;

        // 1. Tắt object tên lửa/phản lực khi hạ cánh
        if (jetpackEffect != null)
        {
            jetpackEffect.SetActive(false);
        }

        // 2. Trả lại quyền hoạt hoạ và ảnh gốc
        if (spriteRenderer != null)
        {
            if (animator != null) animator.enabled = true;
            if (originalSprite != null)
            {
                spriteRenderer.sprite = originalSprite;
            }
        }

        CooldownRemaining = flyCooldown;
        Debug.Log($"[FlyCharacter] Đã hạ cánh — ẩn tên lửa và trả lại trạng thái cũ.");
    }
    public void SetFrozen(bool frozen)
    {
        if (!frozen && (IsFlying || IsFalling))
            StopFlying();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsFlying) return;
        if (col.gameObject.CompareTag("Ground"))
            BeginGentleFall();
    }
}