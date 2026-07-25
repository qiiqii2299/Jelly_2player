using UnityEngine;

/// <summary>
/// Gắn lên Superman cùng với PlayerBase.
/// Chỉ xử lý kỹ năng laser — di chuyển do PlayerBase đảm nhiệm.
/// Space → bắn laser đỏ từ mắt theo hướng mặt đang nhìn.
/// </summary>
public class SupermanController : MonoBehaviour
{
    [Header("Laser Eyes")]
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

        if (Input.GetKeyDown(KeyCode.Space) && isReady)
            FireLaser();
    }

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
