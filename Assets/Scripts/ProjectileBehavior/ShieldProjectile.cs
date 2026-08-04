using System.Collections;
using UnityEngine;

/// <summary>
/// Khiên của Captain America (Phiên bản hoàn chỉnh):
/// - Bay thẳng theo phương ngang đến maxDistance.
/// - Tự động lật mặt sprite theo hướng ném và hướng bay về.
/// - Chỉ hiển thị hiệu ứng lúc bay ra, có thể tùy chỉnh tốc độ biến mất trên Inspector.
/// - Không bị quay tròn lúc bay ra và bay về.
/// - Tự quay về khi chạm Ground (không đi xuyên).
/// - Trúng Player (tag "Player") → freeze nhân vật đó 1 giây.
/// - Khi về đến tay chủ → tự hủy.
/// </summary>
public class ShieldProjectile : MonoBehaviour
{
    // -------------------------------------------------------
    // Set bởi CaptainAmericaController.ThrowShield()
    // -------------------------------------------------------
    [HideInInspector] public GameObject owner;
    [HideInInspector] public float speed = 10f;
    [HideInInspector] public float returnSpeed = 12f;
    [HideInInspector] public float maxDistance = 6f;
    [HideInInspector] public float freezeDuration = 1f;
    [HideInInspector] public float shieldBoostForce = 18f;

    [Header("Cài đặt kích thước & Hiệu ứng")]
    [Tooltip("Tỷ lệ phóng to kích cỡ khiên")]
    public float shieldScale = 1.2f;

    [Tooltip("Prefab hiệu ứng đi kèm khi khiên bay (chỉ phát lúc bay ra)")]
    public GameObject shieldEffectPrefab;

    [Tooltip("Thời gian trễ trước khi xóa hẳn hiệu ứng (số càng nhỏ biến mất càng nhanh)")]
    public float effectFadeTime = 0.2f;

    [Header("Cài đặt va chạm Ground")]
    [Tooltip("Layer mask chỉ định cái gì là Ground")]
    public LayerMask groundLayer;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private enum State { Flying, Returning }
    private State state = State.Flying;
    private Vector2 launchPos;
    private Vector2 direction;

    private bool hasHit = false;
    private float flyTimer = 0f;
    private GameObject activeEffect;
    private float currentFacingDir = 1f; // Hướng mặt của khiên

    // -------------------------------------------------------
    void Awake()
    {
        BuildVisual();
    }

    public void Init(GameObject ownerObj, Vector2 fireDir, float facingDir)
    {
        owner = ownerObj;
        currentFacingDir = facingDir;

        // Ép hướng bay hoàn toàn theo phương ngang (trục Y = 0)
        float horizontalDir = fireDir.x >= 0f ? 1f : -1f;
        direction = new Vector2(horizontalDir, 0f);

        launchPos = transform.position;
        flyTimer = 0f;

        // Lật mặt Sprite của khiên theo hướng ném
        Vector3 shieldScaleFactor = transform.localScale;
        shieldScaleFactor.x = Mathf.Abs(shieldScaleFactor.x) * (currentFacingDir < 0f ? -1f : 1f);
        transform.localScale = shieldScaleFactor;

        // Sinh hiệu ứng LÚC BAY RA
        if (shieldEffectPrefab != null)
        {
            activeEffect = Instantiate(shieldEffectPrefab, transform.position, Quaternion.identity);

            // Lật hiệu ứng nếu bay sang trái
            if (horizontalDir < 0f)
            {
                Vector3 fxScale = activeEffect.transform.localScale;
                fxScale.x = -Mathf.Abs(fxScale.x);
                activeEffect.transform.localScale = fxScale;
            }
        }

        // Bỏ qua va chạm với Owner
        Collider2D[] myCols = GetComponents<Collider2D>();
        Collider2D[] ownerCols = owner != null ? owner.GetComponents<Collider2D>() : new Collider2D[0];

        foreach (var myCol in myCols)
        {
            foreach (var ownerCol in ownerCols)
            {
                if (myCol != null && ownerCol != null)
                    Physics2D.IgnoreCollision(myCol, ownerCol, true);
            }
        }
    }

    // -------------------------------------------------------
    void Update()
    {
        if (state == State.Flying)
        {
            if (activeEffect != null)
            {
                activeEffect.transform.position = transform.position;
                activeEffect.transform.rotation = Quaternion.identity;
            }

            UpdateFlying();
        }
        else
        {
            UpdateReturning();
        }
    }

    // -------------------------------------------------------
    // Phase 1: Bay ra 
    // -------------------------------------------------------
    void UpdateFlying()
    {
        flyTimer += Time.deltaTime;

        float moveAmount = speed * Time.deltaTime;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, moveAmount + 0.3f, groundLayer);

        if (hit.collider != null)
        {
            TriggerReturn();
            return;
        }

        transform.position += (Vector3)(direction * moveAmount);
        transform.rotation = Quaternion.identity;

        float dist = Vector2.Distance(transform.position, launchPos);
        if (dist >= maxDistance)
        {
            TriggerReturn();
        }
    }

    // -------------------------------------------------------
    // Chuyển sang trạng thái quay về
    // -------------------------------------------------------
    void TriggerReturn()
    {
        state = State.Returning;

        if (activeEffect != null)
        {
            ParticleSystem ps = activeEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            Destroy(activeEffect, effectFadeTime);
            activeEffect = null;
        }
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

        // Tự động lật mặt khiên hướng về phía chủ nhân đang đứng khi bay về
        if (toOwner.x != 0)
        {
            float returnFacing = toOwner.x > 0 ? 1f : -1f;
            Vector3 shieldScaleFactor = transform.localScale;
            shieldScaleFactor.x = Mathf.Abs(shieldScaleFactor.x) * returnFacing;
            transform.localScale = shieldScaleFactor;
        }

        // Về đến nơi → tự hủy
        if (toOwner.magnitude < 0.3f)
        {
            Rigidbody2D ownerRb = owner.GetComponent<Rigidbody2D>();
            if (ownerRb != null)
            {
                PlayerBase pb = owner.GetComponent<PlayerBase>();
                bool isAirborne = pb != null ? !pb.IsGrounded() : ownerRb.linearVelocity.y != 0f;

                if (isAirborne)
                {
                    ownerRb.linearVelocity = new Vector2(ownerRb.linearVelocity.x, shieldBoostForce);
                }
            }

            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(toOwner.normalized * returnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.identity;
    }

    // -------------------------------------------------------
    // Va chạm với Player hoặc Owner
    // -------------------------------------------------------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (flyTimer < 0.2f) return;

        if (owner != null && other.gameObject == owner)
        {
            Rigidbody2D ownerRb = owner.GetComponent<Rigidbody2D>();

            bool isJumpingOnShield = ownerRb != null
                && ownerRb.linearVelocity.y < 0f
                && owner.transform.position.y > transform.position.y + 0.1f;

            if (isJumpingOnShield && ownerRb != null)
            {
                ownerRb.linearVelocity = new Vector2(ownerRb.linearVelocity.x, shieldBoostForce);
            }

            if (activeEffect != null)
            {
                Destroy(activeEffect, effectFadeTime);
                activeEffect = null;
            }
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player") && state == State.Flying)
        {
            hasHit = true;
            FreezePlayer(other.gameObject);
            TriggerReturn();
            return;
        }

        if (((1 << other.gameObject.layer) & groundLayer) != 0 && state == State.Flying)
        {
            TriggerReturn();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        if (flyTimer < 0.2f) return;
        if (owner != null && collision.gameObject == owner) return;

        if (collision.gameObject.CompareTag("Player") && state == State.Flying)
        {
            hasHit = true;
            FreezePlayer(collision.gameObject);
            TriggerReturn();
            return;
        }

        if (((1 << collision.gameObject.layer) & groundLayer) != 0 && state == State.Flying)
        {
            TriggerReturn();
        }
    }

    void FreezePlayer(GameObject target)
    {
        FreezeEffect freeze = target.GetComponent<FreezeEffect>();
        if (freeze == null) freeze = target.AddComponent<FreezeEffect>();
        freeze.Apply(freezeDuration);
    }

    void BuildVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            sr.sprite = MakeCircleSprite(64);
            sr.color = new Color(0.2f, 0.4f, 1f);
        }
        sr.sortingOrder = 20;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        transform.localScale = Vector3.one * shieldScale;
    }

    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y,
                    Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}