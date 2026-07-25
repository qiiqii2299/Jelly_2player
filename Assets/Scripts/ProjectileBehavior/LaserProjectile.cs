using UnityEngine;

/// <summary>
/// Tia laser mắt Superman — bay thẳng, phát nổ khi chạm vật thể hoặc Player.
/// Tự tạo visual hình chữ nhật đỏ nếu chưa có prefab.
/// </summary>
public class LaserProjectile : MonoBehaviour
{
    [Header("Bay")]
    public float speed    = 20f;
    public float lifetime = 4f;

    [Header("Va chạm")]
    [Header("Vụ nổ")]
    [Tooltip("Tag của vật thể kích hoạt nổ — mặc định Ground và Player")]
    public string[] explodeOnTags = { "Ground", "Player" };
    public GameObject explosionPrefab;      // để trống → tự tạo hình tròn vàng
    public float      explosionRadius = 2f;
    public float      knockbackForce  = 10f;

    private Vector2    direction;
    private GameObject shooter;

    public void Init(GameObject shooterObj, Vector2 dir)
    {
        shooter   = shooterObj;
        direction = dir.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Awake()
    {
        BuildVisual();
    }

    void Start()
    {
        // Ignore collision với shooter
        Collider2D myCol = GetComponent<Collider2D>();
        if (shooter != null && myCol != null)
        {
            Collider2D sc = shooter.GetComponent<Collider2D>();
            if (sc != null) Physics2D.IgnoreCollision(myCol, sc, true);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        Vector2 origin   = transform.position;
        Vector2 moveStep = direction * speed * Time.deltaTime;

        // Dùng RaycastAll để lọc được chính xác — bỏ qua laser và shooter
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, moveStep.magnitude + 0.15f);
        foreach (var hit in hits)
        {
            // Bỏ qua chính mình và shooter
            if (hit.collider.gameObject == gameObject) continue;
            if (shooter != null && hit.collider.gameObject == shooter) continue;
            // Bỏ qua collider thuộc children của shooter
            if (shooter != null && hit.collider.transform.IsChildOf(shooter.transform)) continue;

            if (ShouldExplode(hit.collider.tag))
            {
                transform.position = hit.point;
                Explode();
                return;
            }
        }

        transform.position += (Vector3)moveStep;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (shooter != null && other.gameObject == shooter) return;
        if (ShouldExplode(other.tag)) Explode();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (shooter != null && collision.gameObject == shooter) return;
        if (ShouldExplode(collision.gameObject.tag)) Explode();
    }

    bool ShouldExplode(string tag)
    {
        foreach (var t in explodeOnTags)
            if (tag == t) return true;
        return false;
    }

    void Explode()
    {
        // Spawn hiệu ứng nổ
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        else
            SpawnFallbackExplosion();

        // Đẩy lùi tất cả Player trong bán kính
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in hits)
        {
            if (!col.CompareTag("Player")) continue;
            if (col.gameObject == shooter)  continue;

            Rigidbody2D targetRb = col.GetComponent<Rigidbody2D>();
            if (targetRb == null) continue;

            Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            targetRb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
        }

        Destroy(gameObject);
    }

    // -------------------------------------------------------
    // Visual laser: hình chữ nhật đỏ dài
    void BuildVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            sr.sprite = MakeRectSprite(32, 8);
            sr.color  = new Color(1f, 0.1f, 0.1f); // đỏ
        }
        sr.sortingOrder = 10;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size      = new Vector2(1f, 0.15f);

        transform.localScale = new Vector3(1f, 0.3f, 1f);
    }

    // Visual nổ fallback: hình tròn vàng tự hủy sau 0.4s
    void SpawnFallbackExplosion()
    {
        GameObject fx = new GameObject("ExplosionFX");
        fx.transform.position = transform.position;

        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCircleSprite(64);
        sr.color        = new Color(1f, 0.6f, 0f, 0.85f); // vàng cam
        sr.sortingOrder = 15;

        float d = explosionRadius * 2f;
        fx.transform.localScale = new Vector3(d, d, 1f);

        Destroy(fx, 0.4f);
    }

    Sprite MakeRectSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
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
}
