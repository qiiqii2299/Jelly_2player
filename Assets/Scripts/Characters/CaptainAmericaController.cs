using UnityEngine;

/// <summary>
/// Gắn lên Captain America cùng với PlayerBase.
/// Di chuyển do PlayerBase đảm nhiệm.
/// Skill: ném khiên theo hướng đang nhìn.
///   - Người chơi nhấn skillKey (Space mặc định)
///   - Bot gọi ThrowShield() trực tiếp
///
/// Khiên bay ra đến maxDistance → quay về.
/// Trúng Player → freeze 1 giây.
/// </summary>
public class CaptainAmericaController : MonoBehaviour
{
    [Header("Khiên")]
    [Tooltip("Prefab khiên — để trống sẽ tự tạo hình tròn xanh")]
    public GameObject shieldPrefab;

    [Tooltip("Transform điểm xuất phát khiên (tay Captain)")]
    public Transform throwPoint;

    [Header("Thông số khiên")]
    public float throwCooldown   = 3f;
    public float shieldSpeed     = 10f;
    public float shieldReturnSpeed = 14f;
    public float maxShieldDistance = 6f;
    public float freezeDuration  = 1f;

    [Header("Bot Mode")]
    [Tooltip("Tick nếu đây là bot — tắt input bàn phím")]
    public bool isBot = false;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private float   cooldownTimer = 0f;
    private bool    isReady       = true;
    private bool    shieldInFlight = false; // chỉ 1 khiên tồn tại cùng lúc

    private PlayerInputController inputController;

    // Bot đọc để biết cooldown
    public bool IsReady => isReady;

    void Start()
    {
        inputController = GetComponent<PlayerInputController>();
    }

    void Update()
    {
        // Đếm cooldown
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isReady = true;
                Debug.Log("[CaptainAmerica] Khiên sẵn sàng!");
            }
        }

        // Người chơi nhấn skill key
        if (!isBot && isReady && !shieldInFlight)
        {
            bool skillPressed = inputController != null
                ? inputController.IsSkillPressed
                : Input.GetKeyDown(KeyCode.Space);

            if (skillPressed)
                ThrowShield();
        }
    }

    /// <summary>
    /// Ném khiên. Gọi trực tiếp bởi bot AI.
    /// </summary>
    public void ThrowShield()
    {
        if (!isReady || shieldInFlight) return;

        Vector2 fireDir  = transform.right; // hướng nhìn thực tế
        Vector3 spawnPos = throwPoint != null
            ? throwPoint.position
            : transform.position + Vector3.right * 0.5f;

        // Tạo khiên — dùng prefab nếu có, fallback tạo GameObject trống
        GameObject shieldObj = shieldPrefab != null
            ? Instantiate(shieldPrefab, spawnPos, Quaternion.identity)
            : new GameObject("Shield");

        shieldObj.transform.position = spawnPos;

        // Gắn ShieldProjectile
        ShieldProjectile shield = shieldObj.GetComponent<ShieldProjectile>();
        if (shield == null) shield = shieldObj.AddComponent<ShieldProjectile>();

        shield.speed          = shieldSpeed;
        shield.returnSpeed    = shieldReturnSpeed;
        shield.maxDistance    = maxShieldDistance;
        shield.freezeDuration = freezeDuration;
        shield.Init(gameObject, fireDir);

        // Theo dõi khiên — khi nó bị Destroy thì cho ném lại
        shieldInFlight = true;
        StartCoroutine(WatchShield(shieldObj));

        // Cooldown
        isReady       = false;
        cooldownTimer = throwCooldown;

        Debug.Log("[CaptainAmerica] Ném khiên!");
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
