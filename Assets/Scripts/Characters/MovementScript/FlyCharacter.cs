using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn kèm với PlayerBase lên nhân vật có khả năng bay.
/// Kích hoạt khi người chơi bấm skill phụ (IsSecondarySkillPressed).
///
/// Luồng:
///   1. Bấm skill phụ → bắt đầu bay (tối đa flyDuration giây).
///   2. Trong khi bay: điều khiển tự do 4 hướng (Trái/Phải/Lên/Xuống).
///   3. Hết thời gian bay, trúng Ground, hoặc bấm lại skill phụ → rơi từ từ 3s.
///   4. PlayerBase.IsFlying = false → PlayerBase tiếp quản trở lại.
/// </summary>
[RequireComponent(typeof(PlayerBase))]
public class FlyCharacter : MonoBehaviour
{
    [Header("Thông số Bay")]
    [Tooltip("Tốc độ di chuyển ngang khi đang bay")]
    public float flySpeed = 6f;

    [Tooltip("Tốc độ di chuyển dọc (lên/xuống) khi đang bay")]
    public float flyVerticalSpeed = 5f;

    [Tooltip("Thời gian bay tối đa (giây)")]
    public float flyDuration = 10f;

    [Tooltip("Thời gian rơi từ từ sau khi hết thời gian bay (giây)")]
    public float fallDuration = 3f;

    [Tooltip("Lực đẩy lên khi bắt đầu bay")]
    public float liftForce = 8f;

    [Tooltip("Tốc độ rơi từ từ khi hết thời gian bay")]
    public float gentleFallSpeed = 2f;

    [Tooltip("Thời gian hồi chiêu sau khi hạ cánh (giây)")]
    public float flyCooldown = 5f;

    // ---- trạng thái ----
    public bool IsFlying   { get; private set; } = false;
    public bool IsFalling  { get; private set; } = false;

    /// <summary>Thời gian hồi chiêu còn lại. 0 = sẵn sàng.</summary>
    public float CooldownRemaining { get; private set; } = 0f;

    private float flyTimer  = 0f;
    private float fallTimer = 0f;

    private PlayerBase            playerBase;
    private Rigidbody2D           rb;
    private PlayerInputController inputController;

    // -------------------------------------------------------
    void Start()
    {
        playerBase      = GetComponent<PlayerBase>();
        rb              = GetComponent<Rigidbody2D>();
        inputController = GetComponent<PlayerInputController>();
    }

    // -------------------------------------------------------
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

            // Lắng nghe skill phụ để kích hoạt bay
            bool secondaryPressed = inputController != null
                ? inputController.IsSecondarySkillPressed
                : Input.GetKeyDown(KeyCode.LeftShift);

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
                : Input.GetKeyDown(KeyCode.LeftShift);

            if (secondaryPressed)
                BeginGentleFall();
        }
        else if (IsFalling)
        {
            HandleGentleFall();
        }
    }

    // -------------------------------------------------------
    void StartFlying()
    {
        IsFlying  = true;
        IsFalling = false;
        flyTimer  = 0f;

        // Báo PlayerBase nhường quyền di chuyển ngang
        playerBase.IsFlying = true;

        // Tắt trọng lực — FlyCharacter tự quản lý vật lý
        rb.gravityScale   = 0f;
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation;

        // Đẩy lên khi bắt đầu bay
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, liftForce);

        Debug.Log("[FlyCharacter] Bắt đầu bay!");
    }

    // -------------------------------------------------------
    void HandleFlyMovement()
    {
        // Người chơi điều hướng tự do 4 chiều khi đang bay
        float horizontal = 0f;
        float vertical   = 0f;

        if (inputController != null)
        {
            if      (inputController.IsRightHeld) horizontal =  1f;
            else if (inputController.IsLeftHeld)  horizontal = -1f;

            if      (inputController.IsUpHeld)   vertical =  1f;
            else if (inputController.IsDownHeld) vertical = -1f;
        }
        else
        {
            if      (Input.GetKey(KeyCode.RightArrow)) horizontal =  1f;
            else if (Input.GetKey(KeyCode.LeftArrow))  horizontal = -1f;

            if      (Input.GetKey(KeyCode.UpArrow))   vertical =  1f;
            else if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;
        }

        rb.linearVelocity = new Vector2(horizontal * flySpeed, vertical * flyVerticalSpeed);

        // Flip sprite theo hướng ngang
        if      (horizontal > 0f) transform.rotation = Quaternion.Euler(0f,   0f, 0f);
        else if (horizontal < 0f) transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // -------------------------------------------------------
    void TickFlyTimer()
    {
        flyTimer += Time.deltaTime;

        if (flyTimer >= flyDuration)
            BeginGentleFall();
    }

    // -------------------------------------------------------
    /// <summary>Chuyển sang trạng thái rơi từ từ — gọi từ hết giờ, trúng Ground, hoặc bấm lại skill phụ.</summary>
    void BeginGentleFall()
    {
        IsFlying  = false;
        IsFalling = true;
        fallTimer = 0f;
        rb.gravityScale = 0f; // tự quản lý trong HandleGentleFall
        Debug.Log("[FlyCharacter] Chuyển sang rơi từ từ...");
    }

    // -------------------------------------------------------
    void HandleGentleFall()
    {
        fallTimer += Time.deltaTime;

        // Vẫn cho phép điều hướng ngang trong lúc rơi
        float horizontal = 0f;
        if (inputController != null)
        {
            if      (inputController.IsRightHeld) horizontal =  1f;
            else if (inputController.IsLeftHeld)  horizontal = -1f;
        }
        else
        {
            if      (Input.GetKey(KeyCode.RightArrow)) horizontal =  1f;
            else if (Input.GetKey(KeyCode.LeftArrow))  horizontal = -1f;
        }

        // Rơi từ từ xuống
        rb.linearVelocity = new Vector2(horizontal * flySpeed, -gentleFallSpeed);

        if      (horizontal > 0f) transform.rotation = Quaternion.Euler(0f,   0f, 0f);
        else if (horizontal < 0f) transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Hết thời gian rơi hoặc đã chạm đất
        bool hitGround = playerBase.IsGrounded();
        if (fallTimer >= fallDuration || hitGround)
            StopFlying();
    }

    // -------------------------------------------------------
    void StopFlying()
    {
        IsFlying  = false;
        IsFalling = false;

        // Trả lại trọng lực và quyền điều khiển cho PlayerBase
        rb.gravityScale    = 1f;
        rb.constraints     = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity  = new Vector2(rb.linearVelocity.x, 0f);

        playerBase.IsFlying = false;

        // Bắt đầu hồi chiêu
        CooldownRemaining = flyCooldown;

        Debug.Log($"[FlyCharacter] Đã hạ cánh — cooldown {flyCooldown}s.");
    }

    // -------------------------------------------------------
    /// <summary>Được gọi từ FreezeEffect để bật/tắt trạng thái bất động.</summary>
    public void SetFrozen(bool frozen)
    {
        if (!frozen && (IsFlying || IsFalling))
            StopFlying();
    }

    // -------------------------------------------------------
    // Trúng Ground trong lúc bay → rơi từ từ ngay lập tức
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsFlying) return;
        if (col.gameObject.CompareTag("Ground"))
            BeginGentleFall();
    }

    // -------------------------------------------------------
    // Gizmo debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
