using UnityEngine;

/// <summary>
/// Gắn lên MissilePrefab.
/// - Tự tạo chấm tròn trắng (không cần component nào trên prefab ngoài script này).
/// - Khi spawn: nhắm thẳng vào Player gần nhất (trừ Ironman bắn ra), bay đường thẳng.
/// </summary>
public class MissileProjectile : MonoBehaviour
{
    [Header("Bay")]
    public float speed    = 12f;
    public float lifetime = 5f;

    private Vector2    direction;
    private GameObject shooter;

    // --------------------------------------------------
    // Gọi từ MissileSkill ngay sau Instantiate
    // --------------------------------------------------
    public void Init(GameObject shooterObj)
    {
        shooter = shooterObj;
    }

    void Awake()
    {
        BuildVisual();
    }

    void Start()
    {
        // Ignore collision với shooter để không tự hủy ngay
        Collider2D myCol = GetComponent<Collider2D>();
        if (shooter != null && myCol != null)
        {
            Collider2D shooterCol = shooter.GetComponent<Collider2D>();
            if (shooterCol != null)
                Physics2D.IgnoreCollision(myCol, shooterCol, true);
        }

        // Xác định hướng bay 1 lần duy nhất
        GameObject target = FindNearestPlayer();
        if (target != null)
        {
            direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            Debug.Log($"[Missile] Nhắm vào: {target.name} | hướng: {direction}");
        }
        else
        {
            direction = Vector2.right;
            Debug.LogWarning("[Missile] Không tìm thấy target nào có tag 'Player'.");
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Bay thẳng, không thay đổi hướng
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (shooter != null && other.gameObject == shooter) return;
        Destroy(gameObject);
    }

    // --------------------------------------------------
    // Tạo visual chấm tròn trắng hoàn toàn bằng code
    // Chỉ thêm component nếu chưa có
    // --------------------------------------------------
    void BuildVisual()
    {
        // --- SpriteRenderer ---
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite       = MakeCircleSprite(32);
        sr.color        = Color.white;
        sr.sortingOrder = 10;

        // --- Collider (chỉ 1 cái) ---
        // Xóa Box nếu có để tránh xung đột
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) Destroy(box);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        // --- Scale ---
        transform.localScale = new Vector3(0.25f, 0.25f, 1f);
    }

    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     radius = size / 2f;
        Vector2   center = new Vector2(radius, radius);

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? Color.white : Color.clear);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    GameObject FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject   nearest = null;
        float        minDist = float.MaxValue;

        foreach (var p in players)
        {
            if (p == shooter) continue;
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist) { minDist = d; nearest = p; }
        }

        return nearest;
    }
}
