using UnityEngine;

/// <summary>
/// Gắn lên Superman cùng với PlayerBase và FlyCharacter.
/// - Skill CHÍNH (Space): Bắn laser đỏ từ mắt.
/// - Skill PHỤ (J/K/LeftShift): Bay lên (xử lý bởi FlyCharacter).
/// Di chuyển do PlayerBase + FlyCharacter đảm nhiệm.
/// </summary>
[RequireComponent(typeof(PlayerBase))]
[RequireComponent(typeof(FlyCharacter))]
public class SupermanController : MonoBehaviour
{
    [Header("Laser Eyes (Skill Chính)")]
    [Tooltip("Prefab laser — để trống sẽ tự tạo hình chữ nhật đỏ")]
    public GameObject laserPrefab;

    [Tooltip("Transform điểm xuất phát laser (vị trí mắt Superman)")]
    public Transform eyePoint;

    [Tooltip("Prefab vụ nổ — để trống sẽ tự tạo hình tròn vàng")]
    public GameObject explosionPrefab;

    [Header("Thông số laser")]
    public float laserCooldown   = 3f;
    public float explosionRadius = 2f;
    public float knockbackForce  = 10f;
    public float laserSpeed      = 20f;
    public float laserLifetime   = 4f;

    private float cooldownTimer = 0f;
    private bool  isReady       = true;

    private PlayerInputController inputController;

    // -------------------------------------------------------
    void Start()
    {
        inputController = GetComponent<PlayerInputController>();
    }

    void Update()
    {
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady = true;
                Debug.Log("[Superman] Laser sẵn sàng!");
            }
        }

        // Skill CHÍNH: Bắn laser
        bool skillPressed = inputController != null
            ? inputController.IsSkillPressed
            : Input.GetKeyDown(KeyCode.Space);

        if (skillPressed && isReady)
            FireLaser();
    }

    // -------------------------------------------------------
    void FireLaser()
    {
        Vector2 fireDir  = transform.right;
        Vector3 spawnPos = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 0.3f;

        GameObject laserObj = laserPrefab != null
            ? Instantiate(laserPrefab, spawnPos, Quaternion.identity)
            : new GameObject("Laser");

        laserObj.transform.position = spawnPos;

        LaserProjectile laser = laserObj.GetComponent<LaserProjectile>();
        if (laser == null) laser = laserObj.AddComponent<LaserProjectile>();

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
