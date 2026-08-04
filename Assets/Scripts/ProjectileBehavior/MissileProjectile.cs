using UnityEngine;

/// <summary>
/// Gắn lên prefab tên lửa của Ironman.
/// Hỗ trợ quỹ đạo bay lượn vòng cung, tự động quy tụ và tự động neo hiệu ứng khói ra đúng đuôi.
/// </summary>
public class MissileProjectile : MonoBehaviour
{
    [Header("Đồ họa Vụ nổ")]
    public GameObject explosionPrefab;

    [Header("Thông số bay")]
    public float speed = 14f;
    public float lifetime = 12f;

    [Header("Hiệu ứng quỹ đạo lượn")]
    public bool useCurvedFlight = true;
    public float curveAmount = 2f; // Độ cong của đường bay

    [Header("Hiệu ứng trúng Player")]
    public float slowDuration = 3f;
    public float slowMultiplier = 0.5f;
    public float knockupForce = 6f;
    public float explosionRadius = 0.6f;

    private Vector2 baseDirection;
    private GameObject shooter;
    private Rigidbody2D rb;
    private float lifeTimer = 0f;
    private float curveSign = 1f; // Quyết định lượn lên hay lượn xuống
    private int missileIndex = 0;   // Lưu vị trí thứ tự của tên lửa trong chuỗi bắn

    public void Init(GameObject shooterObj, GameObject target, int index = 0)
    {
        shooter = shooterObj;
        missileIndex = index;
        baseDirection = transform.right;

        // Phân bổ quỹ đạo lệch nhau cho từng quả (Quả 0 lượn lên, quả 1 lượn xuống, quả 3 tự quy tụ)
        if (missileIndex == 0) curveSign = 1f;
        else if (missileIndex == 1) curveSign = -1f;
        else curveSign = 0.5f; // Quả thứ 3 biên độ nhỏ hơn để chuẩn bị chụm lại

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = baseDirection * speed;
        }
    }

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            BuildVisual();
        }
        else
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D r = gameObject.AddComponent<Rigidbody2D>();
            r.gravityScale = 0f;
            r.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Start()
    {
        // --- TỰ ĐỘNG CĂN CHỈNH KHÓI RA ĐÚNG ĐUÔI TÊN LỬA (Không cần chỉnh tay) ---
        Transform trail = transform.Find("TrailEffect");
        if (trail != null)
        {
            // Số -0.6f là khoảng cách lùi về phía sau đuôi, bạn có thể tăng giảm tùy ý
            trail.localPosition = new Vector3(-0.6f, 0f, 0f);
        }

        Collider2D myCol = GetComponent<Collider2D>();
        if (shooter != null && myCol != null)
        {
            Collider2D shooterCol = shooter.GetComponent<Collider2D>();
            if (shooterCol != null)
                Physics2D.IgnoreCollision(myCol, shooterCol, true);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;

        if (useCurvedFlight)
        {
            float verticalOffset = 0f;

            // Cơ chế quy tụ: Quả thứ 3 (index == 2) sau khi bay được 0.3 giây sẽ tự động gom thẳng về trục giữa
            if (missileIndex == 2 && lifeTimer > 0.3f)
            {
                verticalOffset = Mathf.Lerp(curveAmount * 0.5f, 0f, (lifeTimer - 0.3f) * 4f);
            }
            else
            {
                // Tạo quỹ đạo uốn lượn hình sin bình thường cho các quả trước
                verticalOffset = Mathf.Sin(lifeTimer * 5f) * curveAmount * curveSign;
            }

            Vector2 currentVelocity = new Vector2(baseDirection.x * speed, verticalOffset * speed);

            if (rb != null)
            {
                rb.linearVelocity = currentVelocity;
            }
            else
            {
                transform.position += (Vector3)(currentVelocity * Time.deltaTime);
            }

            // Xoay đầu tên lửa mượt mà theo hướng di chuyển thực tế
            if (currentVelocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        else if (rb == null)
        {
            transform.position += (Vector3)(baseDirection * speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (shooter != null && (other.gameObject == shooter || other.transform.IsChildOf(shooter.transform)))
            return;

        bool hitPlayer = other.CompareTag("Player");
        bool hitGround = other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground");

        if (!hitPlayer && !hitGround) return;

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            SpawnExplosion();
        }

        if (hitPlayer)
        {
            SlowEffect slow = other.GetComponent<SlowEffect>();
            if (slow == null) slow = other.gameObject.AddComponent<SlowEffect>();
            slow.Apply(slowDuration, slowMultiplier);

            KnockupEffect.Apply(other.gameObject, knockupForce);
        }

        Destroy(gameObject);
    }

    void SpawnExplosion()
    {
        GameObject fx = new GameObject("MissileExplosion");
        fx.transform.position = transform.position;

        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite(64);
        sr.color = new Color(1f, 0.55f, 0f, 0.9f);
        sr.sortingOrder = 15;

        float d = explosionRadius * 2f;
        fx.transform.localScale = new Vector3(d, d, 1f);

        Destroy(fx, 0.3f);
    }

    void BuildVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite(32);
        sr.color = new Color(1f, 0.45f, 0f);
        sr.sortingOrder = 10;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) Destroy(box);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        transform.localScale = new Vector3(0.25f, 0.25f, 1f);
    }

    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? Color.white : Color.clear);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}