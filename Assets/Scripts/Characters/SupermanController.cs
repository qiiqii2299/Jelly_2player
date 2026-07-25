using UnityEngine;

/// <summary>
/// Gắn lên Superman — kế thừa PlayerBase (di chuyển đã có sẵn).
/// Space → bắn laser đỏ từ mắt theo hướng mặt đang nhìn.
/// </summary>
public class SupermanController : PlayerBase
{
    // Ẩn các field di chuyển trùng lặp từ PlayerBase khỏi Inspector
    [HideInInspector] new public float moveSpeed;
    [HideInInspector] new public float jumpForce;
    [HideInInspector] new public bool  isAutoRun;
    [HideInInspector] new public Transform groundCheck;
    [HideInInspector] new public float groundCheckRadius;
    [HideInInspector] new public LayerMask groundLayer;
    [HideInInspector] new public float wallCheckDistance;
    [HideInInspector] new public float wallSlideSpeed;
    [HideInInspector] new public float wallGrabDuration;
    [HideInInspector] new public float wallJumpForceX;
    [HideInInspector] new public float wallJumpForceY;
    [Header("Laser Eyes")]
    [Tooltip("Prefab laser — để trống sẽ tự tạo hình chữ nhật đỏ")]
    public GameObject laserPrefab;

    [Tooltip("Transform điểm xuất phát laser (vị trí mắt Superman)")]
    public Transform  eyePoint;

    [Tooltip("Prefab vụ nổ — để trống sẽ tự tạo hình tròn vàng")]
    public GameObject explosionPrefab;

    [Header("Thông số laser")]
    public float laserCooldown    = 3f;
    public float explosionRadius  = 2f;
    public float knockbackForce   = 10f;
    public float laserSpeed       = 20f;
    public float laserLifetime    = 4f;

    private float cooldownTimer = 0f;
    private bool  isReady       = true;

    protected override void Start()
    {
        base.Start();
    }

    protected override void HandleSkillInput()
    {
        // Đếm cooldown
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady = true;
                Debug.Log("[Superman] Laser sẵn sàng!");
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && isReady)
            FireLaser();
    }

    void FireLaser()
    {
        // Hướng bắn theo mặt nhân vật
        Vector2 fireDir  = transform.right;
        Vector3 spawnPos = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 0.3f;

        // Spawn laser — dùng prefab hoặc empty object
        GameObject laserObj;
        if (laserPrefab != null)
            laserObj = Instantiate(laserPrefab, spawnPos, Quaternion.identity);
        else
            laserObj = new GameObject("Laser");

        laserObj.transform.position = spawnPos;

        // Lấy hoặc thêm LaserProjectile
        LaserProjectile laser = laserObj.GetComponent<LaserProjectile>();
        if (laser == null) laser = laserObj.AddComponent<LaserProjectile>();

        // Truyền cấu hình
        laser.explosionPrefab = explosionPrefab;
        laser.explosionRadius = explosionRadius;
        laser.knockbackForce  = knockbackForce;
        laser.speed           = laserSpeed;
        laser.lifetime        = laserLifetime;

        laser.Init(gameObject, fireDir);

        isReady       = false;
        cooldownTimer = laserCooldown;
        Debug.Log($"[Superman] Bắn laser! Cooldown {laserCooldown}s.");
    }
}
