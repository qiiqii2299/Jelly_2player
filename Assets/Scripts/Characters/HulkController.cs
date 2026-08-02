using UnityEngine;

/// <summary>
/// Gắn lên Hulk cùng với PlayerBase (PlayerBase quản lý di chuyển ngang/nhảy cơ bản).
/// Đã loại bỏ hoàn toàn Bot Mode, chỉ giữ lại điều khiển và kỹ năng cho người chơi.
/// Đã sửa lỗi: Giữ nguyên vị trí hiện tại khi hết trạng thái khổng lồ, không bị giật lùi về chỗ cũ.
/// Đã cập nhật: Phân chia rõ 3 ảnh (Trái, Phải, Chính) dựa theo tốc độ thực tế, không bị lỗi khi tự động di chuyển.
/// </summary>
[RequireComponent(typeof(PlayerBase))]
[RequireComponent(typeof(SpriteRenderer))]
public class HulkController : MonoBehaviour
{
    [Header("Cài Đặt Phím Bấm")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode punchKey = KeyCode.J;
    public KeyCode giantKey = KeyCode.Z;

    [Header("Hình Ảnh (Sprites)")]
    [Tooltip("Ảnh mặt chính (kéo image_27ef14.png vào đây)")]
    public Sprite frontSprite;
    [Tooltip("Ảnh khi đi sang TRÁI")]
    public Sprite leftSprite;
    [Tooltip("Ảnh khi đi sang PHẢI")]
    public Sprite rightSprite;

    [Header("1. Nhảy Cao")]
    public float jumpMultiplier = 2.5f;
    public float highJumpCooldown = 4f;
    public GameObject jumpEffectPrefab;

    [Header("2. Đấm")]
    public float punchRange = 1.5f;
    public float freezeDuration = 3f;
    public float punchCooldown = 5f;
    public GameObject hitEffectPrefab;

    [Header("3. Hóa Khổng Lồ")]
    public float giantScaleMultiplier = 1.3f;
    public float giantSpeedMultiplier = 2f;
    public float giantDuration = 3f;
    public float giantCooldown = 10f;
    public Sprite giantFormSprite;

    // --- Biến nội bộ (Cooldown & Trạng thái) ---
    private float highJumpTimer, punchTimer, giantTimer, giantActiveTimer;
    private bool isHighJumpReady = true;
    private bool isPunchReady = true;
    private bool isGiantReady = true;
    private bool isGiantActive = false;

    private Vector3 originalScale;
    private float appliedYOffset;
    private float originalMoveSpeed;
    private Sprite originalSprite; // Lưu tạm ảnh trước khi khổng lồ

    private PlayerBase playerBase;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        playerBase = GetComponent<PlayerBase>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;

        // Bắt đầu game với mặt chính
        if (frontSprite != null) spriteRenderer.sprite = frontSprite;
    }

    void Update()
    {
        TickCooldowns();

        // Xử lý Input kỹ năng trực tiếp
        if (Input.GetKeyDown(jumpKey) && isHighJumpReady) HighJump();
        if (Input.GetKeyDown(punchKey) && isPunchReady) Punch();
        if (Input.GetKeyDown(giantKey) && isGiantReady && !isGiantActive) ActivateGiant();

        // Cập nhật đổi mặt (Trái/Phải/Chính)
        UpdateSpriteAppearance();
    }

    void TickCooldowns()
    {
        if (!isHighJumpReady) { highJumpTimer -= Time.deltaTime; if (highJumpTimer <= 0) isHighJumpReady = true; }
        if (!isPunchReady) { punchTimer -= Time.deltaTime; if (punchTimer <= 0) isPunchReady = true; }
        if (!isGiantReady) { giantTimer -= Time.deltaTime; if (giantTimer <= 0) isGiantReady = true; }

        if (isGiantActive)
        {
            giantActiveTimer -= Time.deltaTime;
            if (giantActiveTimer <= 0f) DeactivateGiant();
        }
    }

    // -------------------------------------------------------
    // QUẢN LÝ HÌNH ẢNH (ĐỔI MẶT)
    // -------------------------------------------------------
    void UpdateSpriteAppearance()
    {
        // Đang là người khổng lồ thì khóa ảnh tĩnh, không tự đổi
        if (isGiantActive) return;

        // 1. KHÓA LẬT 1: Tắt flipX mặc định của SpriteRenderer
        spriteRenderer.flipX = false;

        // 2. KHÓA LẬT 2: Ép scale X luôn dương (đề phòng script PlayerBase tự động đổi chiều scale)
        Vector3 fixedScale = transform.localScale;
        fixedScale.x = Mathf.Abs(originalScale.x);
        transform.localScale = fixedScale;

        // 3. Lấy vận tốc thực tế (thay vì bắt phím bấm)
        float speedX = rb.linearVelocity.x;

        // 4. Xử lý đổi ảnh theo hướng đang lướt đi
        if (speedX < -0.1f)
        {
            // Thực tế đang trượt sang trái
            if (leftSprite != null) spriteRenderer.sprite = leftSprite;
        }
        else if (speedX > 0.1f)
        {
            // Thực tế đang trượt sang phải
            if (rightSprite != null) spriteRenderer.sprite = rightSprite;
        }
        else
        {
            // Vận tốc = 0 (Đứng im hoàn toàn) -> Trở về mặt chính
            if (frontSprite != null) spriteRenderer.sprite = frontSprite;
        }
    }

    // -------------------------------------------------------
    // CÁC KỸ NĂNG
    // -------------------------------------------------------
    public void HighJump()
    {
        float highJumpForce = playerBase.jumpForce * jumpMultiplier;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, highJumpForce);

        SpawnJumpEffect(transform.position);

        isHighJumpReady = false;
        highJumpTimer = highJumpCooldown;
    }

    public void Punch()
    {
        Vector2 punchCenter = (Vector2)transform.position + (Vector2)transform.right * (punchRange * 0.6f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(punchCenter, punchRange);
        bool hitAnyone = false;

        foreach (var col in hits)
        {
            if (!col.CompareTag("Player") || col.gameObject == gameObject) continue;

            FreezeEffect freeze = col.GetComponent<FreezeEffect>();
            if (freeze == null) freeze = col.gameObject.AddComponent<FreezeEffect>();
            freeze.Apply(freezeDuration);

            SpawnHitEffect(col.transform.position);
            hitAnyone = true;
        }

        if (!hitAnyone) SpawnHitEffect(punchCenter);

        isPunchReady = false;
        punchTimer = punchCooldown;
    }

    public void ActivateGiant()
    {
        originalScale = transform.localScale;
        originalMoveSpeed = playerBase.moveSpeed;
        originalSprite = spriteRenderer.sprite;

        float originalHeight = spriteRenderer.bounds.size.y;

        transform.localScale = originalScale * giantScaleMultiplier;
        playerBase.moveSpeed = originalMoveSpeed * giantSpeedMultiplier;

        if (giantFormSprite != null) spriteRenderer.sprite = giantFormSprite;

        appliedYOffset = (originalHeight / 2f) * (giantScaleMultiplier - 1f);
        transform.position += new Vector3(0, appliedYOffset, 0);

        isGiantActive = true;
        giantActiveTimer = giantDuration;
        isGiantReady = false;
        giantTimer = giantCooldown;
    }

    void DeactivateGiant()
    {
        isGiantActive = false;

        // Phục hồi
        if (originalSprite != null) spriteRenderer.sprite = originalSprite;
        transform.localScale = originalScale;
        playerBase.moveSpeed = originalMoveSpeed;
        transform.position -= new Vector3(0, appliedYOffset, 0);
    }

    // -------------------------------------------------------
    // HIỆU ỨNG & GIZMOS
    // -------------------------------------------------------
    void SpawnJumpEffect(Vector2 position)
    {
        if (jumpEffectPrefab != null) Instantiate(jumpEffectPrefab, position, Quaternion.identity);
        else CreateFallbackFX("HulkJumpFX", position, new Color(0.1f, 0.8f, 0.3f, 0.85f), 0.6f);
    }

    void SpawnHitEffect(Vector2 position)
    {
        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, position, Quaternion.identity);
        else CreateFallbackFX("HulkHitFX", position, new Color(0.2f, 1f, 0.2f, 0.9f), 0.4f);
    }

    void CreateFallbackFX(string fxName, Vector2 position, Color color, float scale)
    {
        GameObject fx = new GameObject(fxName);
        fx.transform.position = position;
        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite(32);
        sr.color = color;
        sr.sortingOrder = 20;
        fx.transform.localScale = Vector3.one * scale;
        Destroy(fx, 0.3f);
    }

    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 punchCenter = (Vector2)transform.position + (Vector2)transform.right * (punchRange * 0.6f);
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(punchCenter, punchRange);
    }
}