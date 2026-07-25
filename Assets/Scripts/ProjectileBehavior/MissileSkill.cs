using UnityEngine;

/// <summary>
/// Gắn lên Ironman.
/// Space → bắn tối đa 3 tên lửa, mỗi tên lửa nhắm vào 1 Player khác nhau.
/// Cooldown 15s sau mỗi lần bắn.
/// </summary>
public class MissileSkill : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Kéo prefab tên lửa vào đây")]
    public GameObject missilePrefab;

    [Header("Điểm bắn")]
    public Transform firePoint;

    [Header("Thông số")]
    public float cooldown    = 15f;
    public int   maxMissiles = 3;     // số tên lửa tối đa mỗi lần bắn

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
                Debug.Log("[MissileSkill] Tên lửa sẵn sàng! Cooldown hồi xong.");
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
            Fire();
    }

    public void Fire()
    {
        if (!isReady)
        {
            Debug.Log($"[MissileSkill] Cooldown còn {cooldownTimer:F1}s.");
            return;
        }

        if (missilePrefab == null)
        {
            Debug.LogWarning("[MissileSkill] Chưa gán missilePrefab!");
            return;
        }

        // Lấy danh sách tất cả Player (trừ chính Ironman)
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        int count = 0;

        foreach (var target in allPlayers)
        {
            if (count >= maxMissiles) break;
            if (target == gameObject) continue;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject obj   = Instantiate(missilePrefab, spawnPos, Quaternion.identity);

            MissileProjectile missile = obj.GetComponent<MissileProjectile>();
            if (missile != null)
                missile.Init(gameObject, target);
            else
                Debug.LogError("[MissileSkill] Prefab thiếu script MissileProjectile!");

            count++;
        }

        if (count == 0)
        {
            Debug.LogWarning("[MissileSkill] Không tìm thấy Player nào để nhắm.");
            return;
        }

        Debug.Log($"[MissileSkill] Bắn {count} tên lửa! Cooldown {cooldown}s bắt đầu.");
        isReady       = false;
        cooldownTimer = cooldown;
    }

    public float GetRemainingCooldown() => isReady ? 0f : cooldownTimer;
}
