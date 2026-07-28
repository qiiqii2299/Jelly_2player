using UnityEngine;

/// <summary>
/// Gắn lên prefab tên lửa.
/// Nhận target cụ thể từ MissileSkill, bay thẳng về hướng đó (không theo sát).
/// Tự tạo visual chấm tròn trắng — prefab chỉ cần mỗi script này.
/// </summary>
public class MissileProjectile : MonoBehaviour
{
    [Header("Thông số bay")]
    public float speed    = 14f;
    public float lifetime = 5f;

    private Vector2    direction;
    private GameObject shooter;

    // Gọi từ MissileSkill, truyền shooter và target cụ thể
    public void Init(GameObject shooterObj, GameObject target)
    {
        shooter = shooterObj;

        if (target != null)
        {
            direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            Debug.Log($"[Missile] Nhắm vào: {target.name}");
        }
        else
        {
            direction = transform.right;
        }
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
            Collider2D shooterCol = shooter.GetComponent<Collider2D>();
            if (shooterCol != null)
                Physics2D.IgnoreCollision(myCol, shooterCol, true);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (shooter != null && other.gameObject == shooter) return;
        Destroy(gameObject);
    }

    void BuildVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCircleSprite(32);
        sr.color        = new Color(1f, 0.45f, 0f); // màu cam Ironman
        sr.sortingOrder = 10;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null) Destroy(box);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

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
}
