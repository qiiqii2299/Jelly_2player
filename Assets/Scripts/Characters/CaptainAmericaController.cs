using UnityEngine;

/// <summary>
/// Gắn lên Captain America cùng với PlayerBase.
/// - Đã gỡ bỏ PlayerInputController, nhận phím Space trực tiếp.
/// - Tích hợp quản lý hình ảnh 3 góc (Trái, Phải, Chính).
/// - Skill: Ném khiên theo hướng đang nhìn, tự động lật mặt sprite khiên.
/// </summary>
[RequireComponent(typeof(PlayerBase))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class CaptainAmericaController : MonoBehaviour
{
    [Header("Cài Đặt Phím Bấm")]
    public KeyCode throwKey = KeyCode.Space;

    [Header("Hình Ảnh (Sprites)")]
    [Tooltip("Ảnh mặt chính (đứng im)")]
    public Sprite frontSprite;
    [Tooltip("Ảnh khi đi sang TRÁI")]
    public Sprite leftSprite;
    [Tooltip("Ảnh khi đi sang PHẢI")]
    public Sprite rightSprite;

    [Header("Khiên")]
    [Tooltip("Prefab khiên — để trống sẽ tự tạo hình tròn xanh")]
    public GameObject shieldPrefab;

    [Tooltip("Transform điểm xuất phát khiên (tay Captain)")]
    public Transform throwPoint;

    [Header("Thông số khiên")]
    public float throwCooldown = 3f;
    public float shieldSpeed = 10f;
    public float shieldReturnSpeed = 14f;
    public float maxShieldDistance = 6f;
    public float freezeDuration = 1f;
    public float shieldBoostForce = 18f; // lực nhảy tăng cường khi đạp khiên

    [Header("Bot Mode")]
    [Tooltip("Tick nếu đây là bot — tắt input bàn phím")]
    public bool isBot = false;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private float cooldownTimer = 0f;
    private bool isReady = true;
    private bool shieldInFlight = false; // chỉ 1 khiên tồn tại cùng lúc

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private float facingDir = 1f; // 1 = Phải, -1 = Trái

    // Bot đọc để biết cooldown
    public bool IsReady => isReady;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (frontSprite != null) spriteRenderer.sprite = frontSprite;
    }

    void Update()
    {
        // 1. Cập nhật hình ảnh 3 góc (Trái / Phải / Chính)
        UpdateSpriteAppearance();

        // 2. Đếm cooldown
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady = true;
                Debug.Log("[CaptainAmerica] Khiên sẵn sàng!");
            }
        }

        // 3. Người chơi nhấn phím ném khiên
        if (!isBot && isReady && !shieldInFlight)
        {
            if (Input.GetKeyDown(throwKey))
            {
                ThrowShield();
            }
        }
    }

    // -------------------------------------------------------
    // QUẢN LÝ HÌNH ẢNH (ĐỔI MẶT TRÁI / PHẢI / CHÍNH)
    // -------------------------------------------------------
    void UpdateSpriteAppearance()
    {
        spriteRenderer.flipX = false;
        Vector3 fixedScale = transform.localScale;
        fixedScale.x = Mathf.Abs(originalScale.x);
        transform.localScale = fixedScale;

        float speedX = rb != null ? rb.linearVelocity.x : 0f;

        if (speedX < -0.1f)
        {
            if (leftSprite != null) spriteRenderer.sprite = leftSprite;
            facingDir = -1f;
        }
        else if (speedX > 0.1f)
        {
            if (rightSprite != null) spriteRenderer.sprite = rightSprite;
            facingDir = 1f;
        }
        else
        {
            if (frontSprite != null) spriteRenderer.sprite = frontSprite;

            // Cho phép đổi hướng nhìn qua phím A/D hoặc mũi tên ngay cả khi đứng im
            float inputX = Input.GetAxisRaw("Horizontal");
            if (inputX < 0) facingDir = -1f;
            else if (inputX > 0) facingDir = 1f;
        }
    }

    /// <summary>
    /// Ném khiên. Gọi trực tiếp bởi bot AI hoặc phím Space.
    /// </summary>
    public void ThrowShield()
    {
        if (!isReady || shieldInFlight) return;

        // Xác định hướng ném chuẩn dựa theo hướng mặt thực tế
        Vector2 fireDir = new Vector2(facingDir, 0f);

        Vector3 spawnPos = throwPoint != null
            ? throwPoint.position
            : transform.position + new Vector3(facingDir * 0.5f, 0.3f, 0f);

        // Tạo khiên — dùng prefab nếu có, fallback tạo GameObject trống
        GameObject shieldObj = shieldPrefab != null
            ? Instantiate(shieldPrefab, spawnPos, Quaternion.identity)
            : new GameObject("Shield");

        shieldObj.transform.position = spawnPos;

        // Lấy component ShieldProjectile (tự add nếu chưa có)
        ShieldProjectile shield = shieldObj.GetComponent<ShieldProjectile>();
        if (shield == null) shield = shieldObj.AddComponent<ShieldProjectile>();

        // Truyền thông số sang khiên
        shield.speed = shieldSpeed;
        shield.returnSpeed = shieldReturnSpeed;
        shield.maxDistance = maxShieldDistance;
        shield.freezeDuration = freezeDuration;
        shield.shieldBoostForce = shieldBoostForce;

        // Khởi tạo hướng bay kèm theo hướng mặt để khiên lật mặt chuẩn xác
        shield.Init(gameObject, fireDir, facingDir);

        // Theo dõi khiên — khi nó bị Destroy thì cho ném lại
        shieldInFlight = true;
        StartCoroutine(WatchShield(shieldObj));

        // Cooldown
        isReady = false;
        cooldownTimer = throwCooldown;

        Debug.Log("[CaptainAmerica] Ném khiên về hướng " + facingDir + "!");
    }

    System.Collections.IEnumerator WatchShield(GameObject shieldObj)
    {
        // Chờ đến khi khiên bị Destroy
        while (shieldObj != null)
            yield return null;

        shieldInFlight = false;
        Debug.Log("[CaptainAmerica] Khiên đã về tay.");
    }
}