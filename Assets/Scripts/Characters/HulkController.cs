using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn lên Hulk cùng với PlayerBase.
/// Di chuyển do PlayerBase đảm nhiệm.
///
/// SKILL CHÍNH  — Nhảy Cao (High Jump)
///   Phím: Space (P1) / Keypad0 (P2) — skillKey
///   Hulk bật lên với lực = jumpForce * jumpMultiplier
///   Cooldown: highJumpCooldown giây
///
/// SKILL PHỤ — Đấm (Punch)
///   Phím: J (P1) / Keypad4 (P2) — secondarySkillKey
///   Quét OverlapCircle phía trước → bất động đối thủ freezeDuration giây
///   Cooldown: punchCooldown giây
///
/// SKILL HÓA KHỔNG LỒ — Giant Form
///   Phím: Z (P1) / Keypad1 (P2) — ropeInKey
///   Hulk phóng to scale, tăng tốc chạy trong giantDuration giây
///   Cooldown: giantCooldown giây
/// </summary>
[RequireComponent(typeof(PlayerBase))]
public class HulkController : MonoBehaviour
{
    // -------------------------------------------------------
    // SKILL CHÍNH: Nhảy Cao
    // -------------------------------------------------------
    [Header("Nhảy Cao (Skill Chính — Space/LeftShift)")]
    [Tooltip("Hệ số nhân lên trên jumpForce của PlayerBase")]
    public float jumpMultiplier = 2.5f;
    [Tooltip("Cooldown giữa 2 lần nhảy cao (giây)")]
    public float highJumpCooldown = 4f;

    [Header("Hiệu ứng Nhảy Cao")]
    [Tooltip("Prefab hiệu ứng khi nhảy — để trống dùng fallback màu xanh lá")]
    public GameObject jumpEffectPrefab;

    // -------------------------------------------------------
    // SKILL PHỤ: Đấm
    // -------------------------------------------------------
    [Header("Đấm (Skill Phụ — J/K)")]
    [Tooltip("Tầm đấm — bán kính OverlapCircle phía trước mặt")]
    public float punchRange = 1.5f;
    [Tooltip("Thời gian bất động khi trúng đấm (giây)")]
    public float freezeDuration = 3f;
    [Tooltip("Cooldown giữa 2 lần đấm (giây)")]
    public float punchCooldown = 5f;

    [Header("Hiệu ứng Đấm")]
    [Tooltip("Prefab hiệu ứng đấm — để trống dùng fallback hình tròn xanh lá")]
    public GameObject hitEffectPrefab;

    // -------------------------------------------------------
    // SKILL HÓA KHỔNG LỒ: Giant Form
    // -------------------------------------------------------
    [Header("Hóa Khổng Lồ (Skill 3 — ropeInKey Z/Keypad1)")]
    [Tooltip("Hệ số phóng to scale khi hóa khổng lồ")]
    public float giantScaleMultiplier = 2f;
    [Tooltip("Hệ số nhân tốc độ chạy khi hóa khổng lồ")]
    public float giantSpeedMultiplier = 2f;
    [Tooltip("Thời gian duy trì hình thức khổng lồ (giây)")]
    public float giantDuration = 3f;
    [Tooltip("Cooldown giữa 2 lần hóa khổng lồ (giây)")]
    public float giantCooldown = 10f;

    // -------------------------------------------------------
    // Bot Mode
    // -------------------------------------------------------
    [Header("Bot Mode")]
    [Tooltip("Tick nếu là bot — tắt input bàn phím")]
    public bool isBot = false;

    /// Bot đọc để biết cooldown đấm
    public bool IsPunchReady => isPunchReady;
    /// Bot đọc để biết cooldown nhảy
    public bool IsHighJumpReady => isHighJumpReady;
    /// Bot đọc để biết cooldown hóa khổng lồ
    public bool IsGiantReady => isGiantReady;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private float highJumpTimer = 0f;
    private bool  isHighJumpReady = true;

    private float punchTimer = 0f;
    private bool  isPunchReady = true;

    private float giantTimer     = 0f;
    private bool  isGiantReady   = true;
    private bool  isGiantActive  = false;
    private float giantActiveTimer = 0f;
    private Vector3 originalScale;
    private float   originalMoveSpeed;

    private PlayerInputController inputController;
    private PlayerBase            playerBase;
    private Rigidbody2D           rb;

    // -------------------------------------------------------
    void Start()
    {
        inputController = GetComponent<PlayerInputController>();
        playerBase      = GetComponent<PlayerBase>();
        rb              = GetComponent<Rigidbody2D>();
        originalScale   = transform.localScale;
    }

    void Update()
    {
        TickCooldowns();

        if (isBot) return;

        HandleHighJumpInput();
        HandlePunchInput();
        HandleGiantInput();
    }

    // -------------------------------------------------------
    // Đếm cooldown
    // -------------------------------------------------------
    void TickCooldowns()
    {
        if (!isHighJumpReady)
        {
            highJumpTimer -= Time.deltaTime;
            if (highJumpTimer <= 0f)
            {
                isHighJumpReady = true;
                Debug.Log("[Hulk] Nhảy cao sẵn sàng!");
            }
        }

        if (!isPunchReady)
        {
            punchTimer -= Time.deltaTime;
            if (punchTimer <= 0f)
            {
                isPunchReady = true;
                Debug.Log("[Hulk] Đấm sẵn sàng!");
            }
        }

        if (!isGiantReady)
        {
            giantTimer -= Time.deltaTime;
            if (giantTimer <= 0f)
            {
                isGiantReady = true;
                Debug.Log("[Hulk] Hóa Khổng Lồ sẵn sàng!");
            }
        }

        // Đếm ngược thời gian hiệu ứng đang chạy
        if (isGiantActive)
        {
            giantActiveTimer -= Time.deltaTime;
            if (giantActiveTimer <= 0f)
                DeactivateGiant();
        }
    }

    // -------------------------------------------------------
    // Input skill chính: Nhảy Cao
    // -------------------------------------------------------
    void HandleHighJumpInput()
    {
        if (!isHighJumpReady) return;

        bool skillPressed = inputController != null
            ? inputController.IsSkillPressed
            : Input.GetKeyDown(KeyCode.Space);

        if (skillPressed)
            HighJump();
    }

    // -------------------------------------------------------
    // Input skill phụ: Đấm
    // -------------------------------------------------------
    void HandlePunchInput()
    {
        if (!isPunchReady) return;

        bool punchPressed = inputController != null
            ? inputController.IsSecondarySkillPressed
            : Input.GetKeyDown(KeyCode.J);

        if (punchPressed)
            Punch();
    }

    // -------------------------------------------------------
    // Input skill 3: Hóa Khổng Lồ
    // -------------------------------------------------------
    void HandleGiantInput()
    {
        if (!isGiantReady || isGiantActive) return;

        bool giantPressed = inputController != null
            ? Input.GetKeyDown(inputController.ropeInKey)
            : Input.GetKeyDown(KeyCode.Z);

        if (giantPressed)
            ActivateGiant();
    }

    // -------------------------------------------------------
    /// <summary>
    /// Nhảy cao — lấy jumpForce từ PlayerBase, nhân hệ số jumpMultiplier.
    /// Bot AI gọi trực tiếp hàm này.
    /// </summary>
    public void HighJump()
    {
        if (!isHighJumpReady) return;
        if (rb == null || playerBase == null) return;

        float baseJump     = playerBase.jumpForce;
        float highJumpForce = baseJump * jumpMultiplier;

        // Áp lực đứt khoát lên trên (giữ vận tốc ngang hiện tại)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, highJumpForce);

        // Hiệu ứng tại chân Hulk
        SpawnJumpEffect(transform.position);

        isHighJumpReady = false;
        highJumpTimer   = highJumpCooldown;
        Debug.Log($"[Hulk] Nhảy cao! Lực = {highJumpForce} (base {baseJump} × {jumpMultiplier}). Cooldown {highJumpCooldown}s.");
    }

    // -------------------------------------------------------
    /// <summary>
    /// Đấm — quét OverlapCircle phía trước, bất động mục tiêu.
    /// Bot AI gọi trực tiếp hàm này.
    /// </summary>
    public void Punch()
    {
        if (!isPunchReady) return;

        Vector2 punchCenter = (Vector2)transform.position
                            + (Vector2)transform.right * (punchRange * 0.6f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(punchCenter, punchRange);
        bool hitAnyone = false;

        foreach (var col in hits)
        {
            if (!col.CompareTag("Player"))    continue;
            if (col.gameObject == gameObject) continue;

            FreezeEffect freeze = col.GetComponent<FreezeEffect>();
            if (freeze == null) freeze = col.gameObject.AddComponent<FreezeEffect>();
            freeze.Apply(freezeDuration);

            SpawnHitEffect(col.transform.position);
            hitAnyone = true;
            Debug.Log($"[Hulk] Đấm trúng {col.gameObject.name} — bất động {freezeDuration}s");
        }

        if (!hitAnyone)
            SpawnHitEffect(punchCenter);

        isPunchReady = false;
        punchTimer   = punchCooldown;
        Debug.Log($"[Hulk] Đấm! Cooldown {punchCooldown}s.");
    }

    // -------------------------------------------------------
    /// <summary>
    /// Hóa Khổng Lồ — phóng to scale và tăng tốc chạy trong giantDuration giây.
    /// Bot AI gọi trực tiếp hàm này.
    /// </summary>
    public void ActivateGiant()
    {
        if (!isGiantReady || isGiantActive) return;
        if (playerBase == null) return;

        // Lưu giá trị gốc trước khi thay đổi
        originalScale     = transform.localScale;
        originalMoveSpeed = playerBase.moveSpeed;

        // Phóng to và tăng tốc
        transform.localScale   = originalScale * giantScaleMultiplier;
        playerBase.moveSpeed   = originalMoveSpeed * giantSpeedMultiplier;

        isGiantActive    = true;
        giantActiveTimer = giantDuration;

        isGiantReady = false;
        giantTimer   = giantCooldown;

        Debug.Log($"[Hulk] Hóa Khổng Lồ! Scale x{giantScaleMultiplier}, tốc độ x{giantSpeedMultiplier} trong {giantDuration}s. Cooldown {giantCooldown}s.");
    }

    void DeactivateGiant()
    {
        isGiantActive = false;

        // Khôi phục giá trị gốc
        transform.localScale = originalScale;
        if (playerBase != null)
            playerBase.moveSpeed = originalMoveSpeed;

        Debug.Log("[Hulk] Hết Hóa Khổng Lồ — khôi phục kích thước và tốc độ.");
    }

    // -------------------------------------------------------
    // Hiệu ứng nhảy cao
    // -------------------------------------------------------
    void SpawnJumpEffect(Vector2 position)
    {
        GameObject fx;

        if (jumpEffectPrefab != null)
        {
            fx = Instantiate(jumpEffectPrefab, position, Quaternion.identity);
        }
        else
        {
            fx = new GameObject("HulkJumpFX");
            fx.transform.position = position;

            SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeCircleSprite(32);
            sr.color        = new Color(0.1f, 0.8f, 0.3f, 0.85f); // xanh lá đậm
            sr.sortingOrder = 20;
            fx.transform.localScale = Vector3.one * 0.6f;
        }

        Destroy(fx, 0.3f);
    }

    // -------------------------------------------------------
    // Hiệu ứng đấm
    // -------------------------------------------------------
    void SpawnHitEffect(Vector2 position)
    {
        GameObject fx;

        if (hitEffectPrefab != null)
        {
            fx = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        }
        else
        {
            fx = new GameObject("HulkHitFX");
            fx.transform.position = position;

            SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeCircleSprite(32);
            sr.color        = new Color(0.2f, 1f, 0.2f, 0.9f);
            sr.sortingOrder = 20;
            fx.transform.localScale = Vector3.one * 0.4f;
        }

        Destroy(fx, 0.3f);
    }

    // -------------------------------------------------------
    // Tạo sprite hình tròn runtime
    // -------------------------------------------------------
    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     radius = size / 2f;
        Vector2   center = new Vector2(radius, radius);

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y,
                    Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? Color.white : Color.clear);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // -------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        // Vùng đấm
        Vector2 punchCenter = (Vector2)transform.position
                            + (Vector2)transform.right * (punchRange * 0.6f);
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(punchCenter, punchRange);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, punchCenter);
    }
}
