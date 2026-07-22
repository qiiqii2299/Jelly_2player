using UnityEngine;

/// <summary>
/// Kỹ năng AI cho bot Spider-Man.
/// Logic đơn giản và thực tế:
///   - Khi đang trên không (không chạm đất) → tìm HookPoint phía trên-trước và bắn tơ
///   - Khi đang swing → đung đưa về phía trước, thả tơ sau maxSwingDuration
///   - Không phụ thuộc vào GapAhead — bắn tơ bất cứ khi nào có thể khi trên không
/// </summary>
[RequireComponent(typeof(BotMovement))]
public class SpiderManBotSkill : MonoBehaviour, IAISkillModule
{
    [Header("Web Settings")]
    [Tooltip("Layer có thể bắn tơ vào — thường là HookPoint")]
    public LayerMask attachableLayers;
    public float maxWebDistance   = 5f;
    public float minWebDistance   = 1f;
    public float swingForce       = 10f;
    public float releaseJumpForce = 5f;
    public LineRenderer webLine;
    public Transform    webOrigin;

    [Header("AI Timing")]
    [Tooltip("Thời gian tối đa giữ tơ trước khi tự thả")]
    public float maxSwingDuration = 1.2f;

    [Tooltip("Cooldown sau khi thả tơ trước khi bắn lại (tránh bắn liên tục)")]
    public float shootCooldown = 0.3f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private BotMovement     movement;
    private Rigidbody2D     rb;
    private DistanceJoint2D webJoint;

    private bool  isSwinging   = false;
    private float swingTimer   = 0f;
    private float cooldownTimer = 0f;
    private Vector2 attachPoint;

    void Awake()
    {
        movement = GetComponent<BotMovement>();
        rb       = GetComponent<Rigidbody2D>();
    }

    // -------------------------------------------------------
    // IAISkillModule — gọi mỗi frame từ AIBrain
    // -------------------------------------------------------
    public void Tick(AIContext ctx)
    {
        cooldownTimer -= Time.deltaTime;

        if (isSwinging)
        {
            HandleSwinging();
            return;
        }

        // Bắn tơ khi: đang trên không + cooldown xong + tìm thấy anchor
        bool canShoot = !ctx.IsGrounded
                     && cooldownTimer <= 0f
                     && ctx.HasAnchorPoint;

        if (canShoot)
            TryShootWeb(ctx.BestAnchorPoint);
    }

    // -------------------------------------------------------
    // Bắn tơ về phía anchor
    // -------------------------------------------------------
    void TryShootWeb(Vector2 target)
    {
        if (webOrigin == null) return;

        Vector2 dir = (target - (Vector2)webOrigin.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            webOrigin.position, dir, maxWebDistance, attachableLayers);

        if (hit.collider == null) return;

        float dist = Vector2.Distance(webOrigin.position, hit.point);
        if (dist < minWebDistance) return;

        attachPoint = hit.point;

        // Tạo joint vật lý
        webJoint = gameObject.AddComponent<DistanceJoint2D>();
        webJoint.connectedAnchor       = attachPoint;
        webJoint.autoConfigureDistance = false;
        webJoint.distance              = dist;
        webJoint.enableCollision       = true;

        rb.linearDamping    = 0.5f;
        isSwinging          = true;
        swingTimer          = 0f;
        movement.isSwinging = true;

        DrawWebLine(attachPoint);
    }

    // -------------------------------------------------------
    // Xử lý mỗi frame khi đang swing
    // -------------------------------------------------------
    void HandleSwinging()
    {
        swingTimer += Time.deltaTime;

        // Đẩy về phía trước để tăng tốc swing
        rb.AddForce(Vector2.right * swingForce);

        DrawWebLine(attachPoint);

        if (swingTimer >= maxSwingDuration)
            ReleaseWeb();
    }

    void ReleaseWeb()
    {
        if (webJoint != null)
            Destroy(webJoint);

        ClearWebLine();
        isSwinging          = false;
        movement.isSwinging = false;
        rb.linearDamping    = 0f;
        cooldownTimer       = shootCooldown;

        // Bật lên sau khi thả tơ
        movement.ReleaseJump(releaseJumpForce);
    }

    // Nếu chạm đất khi đang swing → thả tơ ngay
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSwinging)
            ReleaseWeb();
    }

    // -------------------------------------------------------
    // Visual
    // -------------------------------------------------------
    void DrawWebLine(Vector2 target)
    {
        if (webLine == null || webOrigin == null) return;
        webLine.positionCount = 2;
        webLine.SetPosition(0, webOrigin.position);
        webLine.SetPosition(1, target);
    }

    void ClearWebLine()
    {
        if (webLine != null)
            webLine.positionCount = 0;
    }

    void LateUpdate()
    {
        if (isSwinging)
            DrawWebLine(attachPoint);
    }
}
