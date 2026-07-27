using System.Collections;
using UnityEngine;

/// <summary>
/// Khiên của Captain America:
///   - Bay thẳng về phía trước đến maxDistance
///   - Sau khi đến khoảng cách tối đa → tự quay về tay chủ
///   - Trúng Player (tag "Player") → freeze nhân vật đó 1 giây
///   - Trúng Ground trong lúc bay ra → dừng lại, rồi quay về
///   - Khi về đến tay chủ → tự hủy
///
/// Không cần prefab — tự tạo sprite hình tròn xanh dương.
/// </summary>
public class ShieldProjectile : MonoBehaviour
{
    // -------------------------------------------------------
    // Set bởi CaptainAmericaController.ThrowShield()
    // -------------------------------------------------------
    [HideInInspector] public GameObject owner;       // nhân vật ném khiên
    [HideInInspector] public float      speed        = 10f;
    [HideInInspector] public float      returnSpeed  = 12f;
    [HideInInspector] public float      maxDistance  = 6f;
    [HideInInspector] public float      freezeDuration = 1f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private enum State { Flying, Returning }
    private State   state       = State.Flying;
    private Vector2 launchPos;   // vị trí lúc ném — dùng để tính khoảng cách
    private Vector2 direction;   // hướng bay ra

    private bool hasHit = false; // đã trúng rồi thì không xử lý va chạm nữa

    // -------------------------------------------------------
    void Awake()
    {
        BuildVisual();
    }

    /// <summary>
    /// Gọi ngay sau khi Instantiate để khởi tạo hướng bay.
    /// </summary>
    public void Init(GameObject ownerObj, Vector2 fireDir)
    {
        owner     = ownerObj;
        direction = fireDir.normalized;
        launchPos = transform.position;

        // Ignore collision với owner
        Collider2D myCol = GetComponent<Collider2D>();
        Collider2D ownerCol = owner != null ? owner.GetComponent<Collider2D>() : null;
        if (myCol != null && ownerCol != null)
            Physics2D.IgnoreCollision(myCol, ownerCol, true);
    }

    // -------------------------------------------------------
    void Update()
    {
        if (state == State.Flying)
            UpdateFlying();
        else
            UpdateReturning();
    }

    // -------------------------------------------------------
    // Phase 1: Bay ra
    // -------------------------------------------------------
    void UpdateFlying()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Quay tròn cho đẹp
        transform.Rotate(0f, 0f, 720f * Time.deltaTime);

        // Đã đến khoảng cách tối đa → quay về
        float dist = Vector2.Distance(transform.position, launchPos);
        if (dist >= maxDistance)
            state = State.Returning;
    }

    // -------------------------------------------------------
    // Phase 2: Quay về tay chủ
    // -------------------------------------------------------
    void UpdateReturning()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 toOwner = (Vector2)owner.transform.position - (Vector2)transform.position;

        // Về đến nơi → tự hủy
        if (toOwner.magnitude < 0.3f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(toOwner.normalized * returnSpeed * Time.deltaTime);
        transform.Rotate(0f, 0f, 720f * Time.deltaTime);
    }

    // -------------------------------------------------------
    // Va chạm — chỉ xử lý khi đang bay ra (Flying)
    // -------------------------------------------------------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (owner != null && other.gameObject == owner) return;
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        if (other.CompareTag("Player"))
        {
            hasHit = true;
            FreezePlayer(other.gameObject);
            state  = State.Returning; // trúng rồi → quay về ngay
            return;
        }

        if (other.CompareTag("Ground") && state == State.Flying)
        {
            // Chạm đất → dừng lại rồi quay về
            state = State.Returning;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        if (owner != null && collision.gameObject == owner) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            hasHit = true;
            FreezePlayer(collision.gameObject);
            state  = State.Returning;
            return;
        }

        if (collision.gameObject.CompareTag("Ground") && state == State.Flying)
            state = State.Returning;
    }

    // -------------------------------------------------------
    // Freeze Player trong freezeDuration giây
    // -------------------------------------------------------
    void FreezePlayer(GameObject target)
    {
        // Dùng Coroutine trên chính target để không bị mất khi khiên bị Destroy
        FreezeEffect freeze = target.GetComponent<FreezeEffect>();
        if (freeze == null) freeze = target.AddComponent<FreezeEffect>();
        freeze.Apply(freezeDuration);
    }

    // -------------------------------------------------------
    // Visual: hình tròn xanh dương (fallback khi chưa có prefab)
    // -------------------------------------------------------
    void BuildVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            sr.sprite = MakeCircleSprite(64);
            sr.color  = new Color(0.2f, 0.4f, 1f); // xanh dương
        }
        sr.sortingOrder = 10;

        // Collider tròn
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.3f;

        transform.localScale = Vector3.one * 0.6f;
    }

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
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
