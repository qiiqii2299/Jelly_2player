using System.Collections;
using UnityEngine;

/// <summary>
/// Kỹ năng bắn tên lửa của Ironman.
/// Đã tích hợp đổi ảnh nhân vật (Front, Left, Right) và tự động bắt chuẩn hướng trái/phải theo Sprite đang hiển thị.
/// </summary>
[RequireComponent(typeof(PlayerBase))]
public class MissileSkill : MonoBehaviour
{
    [Header("Đồ họa Nhân vật (Sprite)")]
    [Tooltip("Ảnh nhân vật nhìn thẳng (mặc định)")]
    public Sprite frontSprite;
    [Tooltip("Ảnh nhân vật nhìn sang trái")]
    public Sprite leftSprite;
    [Tooltip("Ảnh nhân vật nhìn sang phải")]
    public Sprite rightSprite;

    [Header("Prefabs Tên lửa")]
    public GameObject missilePrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Thông số")]
    public float cooldown = 15f;
    public int maxMissiles = 3;

    [Header("Nút bấm")]
    public KeyCode fireKey = KeyCode.Space;

    private float cooldownTimer = 0f;
    private bool isReady = true;

    // --- Các Component hệ thống ---
    private PlayerBase playerBase;
    private SpriteRenderer spriteRenderer;

    // Lưu hướng đang nhìn hiện tại (1 = phải, -1 = trái)
    private float currentFacingX = 1f;

    void Start()
    {
        playerBase = GetComponent<PlayerBase>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && frontSprite != null)
        {
            spriteRenderer.sprite = frontSprite;
        }
    }

    void Update()
    {
        if (playerBase != null && playerBase.IsFrozen) return;

        // 1. Quản lý thời gian hồi chiêu
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady = true;
                Debug.Log("[MissileSkill] Tên lửa sẵn sàng!");
            }
        }

        // 2. Cập nhật hướng nhìn và đổi ảnh nhân vật liên tục
        if (playerBase != null && !playerBase.IsFlying)
        {
            UpdateFacingSprite();
        }

        // 3. Xử lý bóp cò
        if (Input.GetKeyDown(fireKey))
        {
            Fire();
        }
    }

    // --- Cập nhật hướng nhìn và ảnh theo phím bấm ---
    private void UpdateFacingSprite()
    {
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            currentFacingX = -1f;
            if (spriteRenderer != null && leftSprite != null)
                spriteRenderer.sprite = leftSprite;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            currentFacingX = 1f;
            if (spriteRenderer != null && rightSprite != null)
                spriteRenderer.sprite = rightSprite;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            if (spriteRenderer != null && frontSprite != null)
                spriteRenderer.sprite = frontSprite;
            // Giữ nguyên hướng nhìn gần nhất (currentFacingX) khi bấm xuống nhìn thẳng
        }
    }

    public void Fire()
    {
        if (!isReady) return;
        if (missilePrefab == null) return;

        // Xác định hướng bắn theo Sprite hoặc phím bấm
        float fireDirectionX = 1f;
        if (spriteRenderer != null && spriteRenderer.sprite == leftSprite)
        {
            fireDirectionX = -1f;
            currentFacingX = -1f;
        }
        else if (spriteRenderer != null && spriteRenderer.sprite == rightSprite)
        {
            fireDirectionX = 1f;
            currentFacingX = 1f;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            fireDirectionX = -1f;
            currentFacingX = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            fireDirectionX = 1f;
            currentFacingX = 1f;
        }
        else
        {
            fireDirectionX = currentFacingX;
        }

        // Bắt đầu chuỗi bắn lần lượt từng quả
        StartCoroutine(FireSequence(fireDirectionX));

        isReady = false;
        cooldownTimer = cooldown;
    }

    // --- Tiến trình bắn lần lượt từng quả tên lửa kèm khói ---
    private IEnumerator FireSequence(float fireDirectionX)
    {
        for (int i = 0; i < maxMissiles; i++)
        {
            // Tính vị trí xuất phát cho từng quả nối đuôi nhau nhẹ nhàng
            Vector3 spawnPos = transform.position + new Vector3(fireDirectionX * (0.9f + i * 0.3f), 0.2f, 0f);
            Quaternion spawnRot = fireDirectionX > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

            // Hiệu ứng chớp sáng đầu nòng riêng cho mỗi lần bắn
            if (muzzleFlashPrefab != null)
            {
                StartCoroutine(ShowMuzzleFlash(fireDirectionX, spawnPos));
            }

            // Sinh ra đúng 1 tên lửa (đã kèm sẵn hiệu ứng khói con bên trong Prefab)
            GameObject obj = Instantiate(missilePrefab, spawnPos, spawnRot);
            MissileProjectile missile = obj.GetComponent<MissileProjectile>();
            if (missile != null)
            {
                // Truyền index để quả thứ 3 biết đường quy tụ
                missile.Init(gameObject, null, i);
            }

            // Đợi một khoảng thời gian ngắn (0.15 giây) rồi mới bắn quả tiếp theo để tạo hiệu ứng lần lượt
            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator ShowMuzzleFlash(float fireDirectionX, Vector3 flashPos)
    {
        GameObject flash = Instantiate(muzzleFlashPrefab, flashPos, Quaternion.identity);

        if (fireDirectionX < 0)
        {
            flash.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        yield return new WaitForSeconds(0.1f);
        Destroy(flash);
    }
}