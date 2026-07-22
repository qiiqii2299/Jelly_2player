using UnityEngine;

/// <summary>
/// Gắn lên nhân vật Ironman (player hoặc AI).
/// Chuột trái → bắn missile với cooldown 15s.
/// Yêu cầu: gán missilePrefab (prefab có MissileProjectile) và firePoint.
/// </summary>
public class MissileSkill : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab tên lửa — cần có script MissileProjectile")]
    public GameObject missilePrefab;
    [Tooltip("Transform điểm xuất phát tên lửa (ví dụ: tay / ngực Ironman)")]
    public Transform  firePoint;

    [Header("Cooldown")]
    public float cooldown = 15f;

    private float      cooldownTimer = 0f;
    private bool       isReady       = true;
    private Collider2D myCollider;  // giữ lại để tương thích, không còn dùng trực tiếp

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // Đếm cooldown
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady       = true;
                cooldownTimer = 0f;
                Debug.Log("[MissileSkill] Tên lửa đã sẵn sàng! Cooldown hồi xong.");
            }
        }

        // Input: chuột trái
        if (Input.GetMouseButtonDown(0))
            Fire();
    }

    /// <summary>Gọi hàm này để bắn (dùng cho AI hoặc input thủ công).</summary>
    public void Fire()
    {
        if (!isReady)
        {
            Debug.Log($"[MissileSkill] Còn {cooldownTimer:F1}s mới bắn lại được.");
            return;
        }

        if (missilePrefab == null)
        {
            Debug.LogWarning("[MissileSkill] Chưa gán missilePrefab!");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("[MissileSkill] Chưa gán firePoint!");
            return;
        }

        // Spawn và truyền gameObject của shooter để tránh self-hit
        GameObject obj = Instantiate(missilePrefab, firePoint.position, firePoint.rotation);
        Debug.Log($"[MissileSkill] Spawned missile tại {firePoint.position}");
        MissileProjectile missile = obj.GetComponent<MissileProjectile>();
        if (missile != null)
            missile.Init(gameObject);
        else
            Debug.LogError("[MissileSkill] Prefab thiếu script MissileProjectile!");

        isReady       = false;
        cooldownTimer = cooldown;
        Debug.Log("[MissileSkill] Đã bắn tên lửa! Cooldown 15s bắt đầu.");
    }

    public float GetRemainingCooldown() => isReady ? 0f : cooldownTimer;
}
