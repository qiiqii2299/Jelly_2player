using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn lên Hulk cùng với PlayerBase.
/// Di chuyển do PlayerBase đảm nhiệm.
///
/// Skill: đấm — quét OverlapCircle phía trước mặt.
///   Trúng Player (tag "Player") → bất động 3 giây + hiệu ứng vòng tròn nhỏ.
///   Phím: Space (hoặc skillKey của PlayerInputController).
///   Cooldown: 5 giây.
/// </summary>
public class HulkController : MonoBehaviour
{
    [Header("Thông số đấm")]
    [Tooltip("Tầm đấm — bán kính OverlapCircle phía trước mặt")]
    public float punchRange    = 1.5f;
    [Tooltip("Thời gian bất động khi trúng đấm (giây)")]
    public float freezeDuration = 3f;
    [Tooltip("Cooldown giữa 2 lần đấm (giây)")]
    public float punchCooldown  = 5f;

    [Header("Hiệu ứng")]
    [Tooltip("Prefab hiệu ứng đấm — để trống tự tạo hình tròn nhỏ")]
    public GameObject hitEffectPrefab;

    [Header("Bot Mode")]
    [Tooltip("Tick nếu là bot — tắt input bàn phím")]
    public bool isBot = false;

    // Bot đọc để biết cooldown
    public bool IsReady => isReady;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private float  cooldownTimer = 0f;
    private bool   isReady       = true;

    private PlayerInputController inputController;

    // -------------------------------------------------------
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
                Debug.Log("[Hulk] Đấm sẵn sàng!");
            }
        }

        // Input người chơi
        if (!isBot && isReady)
        {
            bool skillPressed = inputController != null
                ? inputController.IsSkillPressed
                : Input.GetKeyDown(KeyCode.Space);

            if (skillPressed)
                Punch();
        }
    }

    // -------------------------------------------------------
    /// <summary>
    /// Thực hiện đấm. Bot AI gọi trực tiếp hàm này.
    /// </summary>
    public void Punch()
    {
        if (!isReady) return;

        // Tâm vùng đấm: phía trước mặt Hulk
        Vector2 punchCenter = (Vector2)transform.position
                            + (Vector2)transform.right * (punchRange * 0.6f);

        // Quét tất cả collider trong tầm đấm
        Collider2D[] hits = Physics2D.OverlapCircleAll(punchCenter, punchRange);

        bool hitAnyone = false;

        foreach (var col in hits)
        {
            if (!col.CompareTag("Player"))    continue;
            if (col.gameObject == gameObject) continue; // không tự đấm mình

            // Freeze target
            FreezeEffect freeze = col.GetComponent<FreezeEffect>();
            if (freeze == null) freeze = col.gameObject.AddComponent<FreezeEffect>();
            freeze.Apply(freezeDuration);

            // Hiệu ứng tại vị trí mục tiêu
            SpawnHitEffect(col.transform.position);

            hitAnyone = true;
            Debug.Log($"[Hulk] Đấm trúng {col.gameObject.name} — bất động {freezeDuration}s");
        }

        // Hiệu ứng tại tâm vùng đấm dù trúng hay không (visual feedback)
        if (!hitAnyone)
            SpawnHitEffect(punchCenter);

        // Bắt đầu cooldown
        isReady       = false;
        cooldownTimer = punchCooldown;
        Debug.Log($"[Hulk] Đấm! Cooldown {punchCooldown}s.");
    }

    // -------------------------------------------------------
    // Spawn hiệu ứng hình tròn nhỏ tại vị trí target
    // -------------------------------------------------------
    void SpawnHitEffect(Vector2 position)
    {
        GameObject fx;

        if (hitEffectPrefab != null)
        {
            fx = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        }
        else
        {
            // Fallback: tạo hình tròn xanh lá nhỏ, tự hủy sau 0.3s
            fx = new GameObject("HulkHitFX");
            fx.transform.position = position;

            SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeCircleSprite(32);
            sr.color        = new Color(0.2f, 1f, 0.2f, 0.9f); // xanh lá
            sr.sortingOrder = 20;

            // Nhỏ — scale 0.4
            fx.transform.localScale = Vector3.one * 0.4f;
        }

        Destroy(fx, 0.3f);
    }

    // -------------------------------------------------------
    // Tạo sprite hình tròn runtime (dùng khi chưa có prefab)
    // -------------------------------------------------------
    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     radius = size / 2f;
        Vector2   center = new Vector2(radius, radius);

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y,
                    Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? Color.white : Color.clear);

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    // -------------------------------------------------------
    // Gizmos — hiển thị vùng đấm khi chọn object
    // -------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        Vector2 punchCenter = (Vector2)transform.position
                            + (Vector2)transform.right * (punchRange * 0.6f);

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(punchCenter, punchRange);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, punchCenter);
    }
}
