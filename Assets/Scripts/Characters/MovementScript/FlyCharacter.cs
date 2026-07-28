using UnityEngine;

/// <summary>
/// Script di chuyển dành riêng cho nhân vật có cơ chế bay tự do.
/// Không kế thừa PlayerBase — hoạt động hoàn toàn độc lập.
/// Yêu cầu: Rigidbody2D (Gravity Scale = 0), PlayerInputController trên cùng GameObject.
/// </summary>
public class FlyCharacter : MonoBehaviour
{
    // -------------------------------------------------------
    [Header("Thông số bay")]
    public float flySpeed    = 6f;    // tốc độ di chuyển tối đa
    public float acceleration = 12f;  // gia tốc (units/s²)
    public float deceleration = 10f;  // độ giảm tốc khi không nhấn phím

    // -------------------------------------------------------
    // Flag bất động — FreezeEffect gọi SetFrozen() để kích hoạt
    public bool IsFrozen { get; private set; } = false;

    // -------------------------------------------------------
    // Tham chiếu nội bộ
    private Rigidbody2D   rb;
    private Animator      anim;
    private PlayerInputController inputController;

    private float currentDirection = 1f; // 1 = mặt phải, -1 = mặt trái

    // -------------------------------------------------------
    protected virtual void Start()
    {
        rb              = GetComponent<Rigidbody2D>();
        anim            = GetComponent<Animator>();
        inputController = GetComponent<PlayerInputController>();

        // Đảm bảo không bị trọng lực kéo và không bị xoay lật vật lý
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    // -------------------------------------------------------
    protected virtual void Update()
    {
        if (IsFrozen) return;

        HandleMovement();
        HandleSkillInput();
    }

    // -------------------------------------------------------
    /// <summary>
    /// Di chuyển 4 hướng tự do. Dùng acceleration/deceleration để cảm giác mượt.
    /// Flip sprite theo trục Y — không bao giờ thay đổi trục Z, tránh lật ngược.
    /// </summary>
    private void HandleMovement()
    {
        // --- Đọc input ---
        float horizontal = 0f;
        float vertical   = 0f;

        if (inputController != null)
        {
            if (inputController.IsRightHeld) horizontal =  1f;
            else if (inputController.IsLeftHeld) horizontal = -1f;

            if (inputController.IsUpHeld)   vertical =  1f;
            else if (inputController.IsDownHeld) vertical = -1f;
        }

        // --- Tính vector hướng (normalize để bay chéo không nhanh hơn) ---
        Vector2 inputDir = new Vector2(horizontal, vertical);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        // --- Áp dụng acceleration / deceleration ---
        Vector2 targetVelocity = inputDir * flySpeed;

        float lerpRate = inputDir.sqrMagnitude > 0f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, lerpRate * Time.deltaTime);

        // --- Flip sprite theo hướng ngang (chỉ trục Y, giữ nguyên Z = 0) ---
        if (horizontal > 0f)       currentDirection =  1f;
        else if (horizontal < 0f)  currentDirection = -1f;

        transform.rotation = currentDirection > 0f
            ? Quaternion.Euler(0f,   0f, 0f)
            : Quaternion.Euler(0f, 180f, 0f);
    }

    // -------------------------------------------------------
    /// <summary>
    /// Override trong subclass để xử lý kĩ năng riêng của từng nhân vật bay.
    /// </summary>
    protected virtual void HandleSkillInput()
    {
        // Subclass override, ví dụ:
        // if (inputController != null && inputController.IsSkillPressed) UseSkill();
    }

    // -------------------------------------------------------
    /// <summary>
    /// Được gọi từ FreezeEffect để bật/tắt trạng thái bất động.
    /// Logic giống PlayerBase.SetFrozen() nhưng gravityScale luôn = 0 khi unfreeze.
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;

        if (rb == null) return;

        if (frozen)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale   = 0f;
            rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            // Nhân vật bay → gravityScale vẫn = 0 sau khi thoát freeze
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    // -------------------------------------------------------
    // Vẽ gizmo debug để dễ quan sát trong Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
