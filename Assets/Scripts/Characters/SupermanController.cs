using UnityEngine;

/// <summary>
/// Gắn lên Superman cùng với PlayerBase và FlyCharacter.
/// - Skill CHÍNH (Space): Bắn laser đỏ từ mắt.
/// - Skill PHỤ (Phím Z hoặc X): Kích hoạt bay lên (phối hợp với FlyCharacter).
/// - Tích hợp quản lý hình ảnh 3 góc (Trái, Phải, Chính).
/// </summary>
[RequireComponent(typeof(PlayerBase))]
[RequireComponent(typeof(FlyCharacter))]
[RequireComponent(typeof(SpriteRenderer))]
public class SupermanController : MonoBehaviour
{
    [Header("Cài Đặt Phím Bấm")]
    public KeyCode laserKey = KeyCode.Space;
    public KeyCode flyKeyZ = KeyCode.Z; // Phím phụ bay lựa chọn 1
    public KeyCode flyKeyX = KeyCode.X; // Phím phụ bay lựa chọn 2

    [Header("Hình Ảnh (Sprites)")]
    [Tooltip("Ảnh mặt chính (đứng im)")]
    public Sprite frontSprite;
    [Tooltip("Ảnh khi đi/bay sang TRÁI")]
    public Sprite leftSprite;
    [Tooltip("Ảnh khi đi/bay sang PHẢI")]
    public Sprite rightSprite;

    [Header("Laser Eyes (Skill Chính)")]
    public GameObject laserPrefab;
    public Transform eyePoint;
    public GameObject explosionPrefab;

    [Header("Thông số laser")]
    public float laserCooldown = 3f;
    public float explosionRadius = 2f;
    public float knockbackForce = 10f;
    public float laserSpeed = 20f;
    public float laserLifetime = 4f;

    // --- Biến nội bộ ---
    private float laserCooldownTimer = 0f;
    private bool isLaserReady = true;

    private Vector3 originalScale;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private FlyCharacter flyCharacter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        flyCharacter = GetComponent<FlyCharacter>();
        originalScale = transform.localScale;

        if (frontSprite != null) spriteRenderer.sprite = frontSprite;
    }

    void Update()
    {
        // 1. Cập nhật đổi mặt (Trái/Phải/Chính) theo vận tốc thực tế
        UpdateSpriteAppearance();

        // 2. Quản lý Cooldown Laser
        if (!isLaserReady)
        {
            laserCooldownTimer -= Time.deltaTime;
            if (laserCooldownTimer <= 0f)
            {
                isLaserReady = true;
                Debug.Log("[Superman] Laser sẵn sàng!");
            }
        }

        // 3. Skill CHÍNH: Bắn laser (Phím Space)
        if (Input.GetKeyDown(laserKey) && isLaserReady)
        {
            FireLaser();
        }

        // 4. Skill PHỤ: Bật/Tắt chế độ bay bằng phím Z hoặc X
        if (Input.GetKeyDown(flyKeyZ) || Input.GetKeyDown(flyKeyX))
        {
            HandleFlyInput();
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
        }
        else if (speedX > 0.1f)
        {
            if (rightSprite != null) spriteRenderer.sprite = rightSprite;
        }
        else
        {
            if (frontSprite != null) spriteRenderer.sprite = frontSprite;
        }
    }

    // -------------------------------------------------------
    // KÍCH HOẠT BAY QUA FLYCHARACTER
    // -------------------------------------------------------
    void HandleFlyInput()
    {
        if (flyCharacter == null) return;

        // Nếu đang bay → bấm phím Z/X lần nữa để hạ cánh (chuyển sang rơi từ từ)
        if (flyCharacter.IsFlying)
        {
            // Gọi phương thức BeginGentleFall (hoặc StopFlying) bên FlyCharacter bằng cách tắt trạng thái bay
            // Cách đơn giản nhất là gọi lại hàm kiểm tra phím phụ bên FlyCharacter hoặc viết thêm public hàm hạ cánh.
            return;
        }

        // Nếu chưa bay, không rơi và đã hồi chiêu xong
        if (!flyCharacter.IsFlying && !flyCharacter.IsFalling && flyCharacter.CooldownRemaining <= 0f)
        {
            // Gọi trực tiếp coroutine hoặc hàm StartFlying bên FlyCharacter
            // Vì FlyCharacter đang dùng cơ chế ẩn, ta chỉ cần kích hoạt bằng cách gọi một hàm công khai.
            // (Xem Bước 2 để mở hàm StartFlying bên FlyCharacter)
            flyCharacter.TriggerFlightExternal();
        }
    }

    // -------------------------------------------------------
    // KỸ NĂNG BẮN LASER
    // -------------------------------------------------------

    void FireLaser()
    {
        // 1. Xác định hướng bắn chính xác dựa vào hướng mặt thực tế của nhân vật (Trái hoặc Phải)
        float currentFacing = (spriteRenderer != null && spriteRenderer.sprite == leftSprite) ? -1f : 1f;

        // Nếu đang di chuyển sang trái/phải rõ ràng thì ưu tiên lấy theo vận tốc thực tế
        float speedX = rb != null ? rb.linearVelocity.x : 0f;
        if (speedX < -0.1f) currentFacing = -1f;
        else if (speedX > 0.1f) currentFacing = 1f;

        Vector2 fireDir = new Vector2(currentFacing, 0f);

        // 2. Vị trí xuất phát từ mắt
        Vector3 spawnPos = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 0.3f;

        GameObject laserObj = laserPrefab != null
            ? Instantiate(laserPrefab, spawnPos, Quaternion.identity)
            : new GameObject("Laser");

        laserObj.transform.position = spawnPos;

        // 3. Cấu hình viên đạn Laser
        LaserProjectile laser = laserObj.GetComponent<LaserProjectile>();
        if (laser == null) laser = laserObj.AddComponent<LaserProjectile>();

        laser.explosionPrefab = explosionPrefab;
        laser.explosionRadius = explosionRadius;
        laser.knockbackForce = knockbackForce;
        laser.speed = laserSpeed;
        laser.lifetime = laserLifetime;

        // Ép Rigidbody2D của viên đạn không bị trọng lực kéo rơi xuống đất
        Rigidbody2D laserRb = laserObj.GetComponent<Rigidbody2D>();
        if (laserRb != null)
        {
            laserRb.gravityScale = 0f; // Không bị rơi
        }

        laser.Init(gameObject, fireDir);

        isLaserReady = false;
        laserCooldownTimer = laserCooldown;
        Debug.Log($"[Superman] Bắn laser về hướng {currentFacing}!");
    }
}
