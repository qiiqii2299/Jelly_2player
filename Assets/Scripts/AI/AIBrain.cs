using UnityEngine;

/// <summary>
/// Brain dùng chung cho mọi bot.
/// Mỗi frame:
///   1. Sense  — quét môi trường bằng Raycast, điền vào AIContext
///   2. Decide — ra lệnh di chuyển / nhảy qua BotMovement
///   3. Skill  — delegate cho IAISkillModule của nhân vật (bắn tơ / bắn móc)
///
/// Script người chơi (Double_Jump, WebSwing, GrapplingHook) KHÔNG bị đụng tới.
/// </summary>
[RequireComponent(typeof(BotMovement))]
public class AIBrain : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector
    // -------------------------------------------------------
    [Header("Sensor — Ground / Gap")]
    [Tooltip("Layer mặt đất / platform — set ở BotMovement, AIBrain đọc từ đó")]
 
    public float gapProbeOffset = 0.7f;
    public float gapProbeDepth  = 2.5f;

    [Header("Sensor — Wall")]
    public float wallRayLength   = 1.0f;
    public float wallRayHeight   = 0.4f;

    [Header("Sensor — Anchor")]
    [Tooltip("Layer có thể bắn tơ / móc vào")]
    public LayerMask anchorLayer;
    public float anchorSearchRadius = 6f;
    public float anchorForwardBias  = 2f; // tìm thiên về phía trước

    [Header("Behavior")]
    public float jumpTriggerDistance = 1.0f;

    [Tooltip("Thời gian không di chuyển trước khi kích hoạt anti-stuck")]
    public float stuckTimeout = 1.8f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private BotMovement     movement;
    private IAISkillModule  skillModule;
    private AIContext       ctx = new AIContext();

    // Anti-stuck
    private Vector2 lastPos;
    private float   stuckTimer    = 0f;
    private bool    isUnstucking  = false;
    private float   unstuckTimer  = 0f;
    private const float UNSTUCK_DURATION = 0.6f;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------
    void Awake()
    {
        movement    = GetComponent<BotMovement>();
        skillModule = GetComponent<IAISkillModule>(); // null = basic bot không có kỹ năng
    }

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        Sense();
        Decide();
        skillModule?.Tick(ctx);
    }

    // -------------------------------------------------------
    // 1. SENSE
    // -------------------------------------------------------
    void Sense()
    {
        ctx.Position   = transform.position;
        ctx.Velocity   = movement.GetRb().linearVelocity;
        ctx.IsGrounded = movement.isGrounded; // đọc từ BotMovement (Raycast mỗi frame)

        DetectGap();
        DetectWall();
        FindAnchor();
        TrackStuck();
    }

    void DetectGap()
    {
        ctx.GapAhead      = false;
        ctx.DistanceToGap = float.MaxValue;

        // Dùng groundLayer của BotMovement — chỉ cần set 1 chỗ
        LayerMask ground = movement.groundLayer;

        Vector2 probeOrigin = (Vector2)transform.position + Vector2.right * gapProbeOffset;
        RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, gapProbeDepth, ground);

        if (hit.collider == null)
        {
            ctx.GapAhead = true;
            RaycastHit2D edgeHit = Physics2D.Raycast(transform.position, Vector2.right, gapProbeOffset + 0.5f, ground);
            ctx.DistanceToGap = edgeHit.collider != null ? edgeHit.distance : gapProbeOffset;
        }
    }

    void DetectWall()
    {
        ctx.WallAhead      = false;
        ctx.DistanceToWall = float.MaxValue;

        LayerMask ground = movement.groundLayer;

        Vector2 origin = (Vector2)transform.position + Vector2.up * wallRayHeight;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right, wallRayLength, ground);

        if (hit.collider != null)
        {
            ctx.WallAhead      = true;
            ctx.DistanceToWall = hit.distance;
        }
    }

    void FindAnchor()
    {
        ctx.HasAnchorPoint  = false;
        ctx.BestAnchorPoint = Vector2.zero;

        Vector2 searchCenter = (Vector2)transform.position + Vector2.right * anchorForwardBias;
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchCenter, anchorSearchRadius, anchorLayer);

        float bestScore = float.MinValue;

        foreach (var col in hits)
        {
            Vector2 point   = col.ClosestPoint(transform.position);
            Vector2 toPoint = point - (Vector2)transform.position;

            // Chỉ lấy điểm phía trước
            if (toPoint.x <= 0f) continue;

            // Ưu tiên điểm xa về trước + cao lên trên
            float score = toPoint.x * 1.5f + toPoint.y;

            if (score > bestScore)
            {
                bestScore          = score;
                ctx.BestAnchorPoint = point;
                ctx.HasAnchorPoint  = true;
            }
        }
    }

    void TrackStuck()
    {
        float moved = Vector2.Distance(transform.position, lastPos);

        if (moved < 0.04f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer   = 0f;
            isUnstucking = false;
            lastPos      = transform.position;
        }

        if (stuckTimer >= stuckTimeout && !isUnstucking)
        {
            isUnstucking = true;
            unstuckTimer = 0f;
            stuckTimer   = 0f;
            Debug.Log($"[AIBrain] {gameObject.name} bị kẹt → anti-stuck kích hoạt");
        }

        if (isUnstucking)
        {
            unstuckTimer += Time.deltaTime;
            if (unstuckTimer >= UNSTUCK_DURATION)
                isUnstucking = false;
        }
    }

    // -------------------------------------------------------
    // 2. DECIDE
    // -------------------------------------------------------
    void Decide()
    {
        // Anti-stuck: nhảy liên tục để thoát
        if (isUnstucking)
        {
            movement.MoveForward();
            movement.Jump();
            return;
        }

        // Luôn tiến về phía trước
        movement.MoveForward();

        // Nhảy khi sắp rơi xuống gap
        if (ctx.IsGrounded && ctx.GapAhead && ctx.DistanceToGap <= jumpTriggerDistance)
            movement.Jump();

        // Nhảy khi có tường chặn trước mặt
        if (ctx.IsGrounded && ctx.WallAhead && ctx.DistanceToWall <= jumpTriggerDistance)
            movement.Jump();
    }

    // -------------------------------------------------------
    // Gizmos debug (chỉ hiện khi chọn object trong Scene)
    // -------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        // Ray gap
        Gizmos.color = Color.red;
        Vector2 gapOrigin = (Vector2)transform.position + Vector2.right * gapProbeOffset;
        Gizmos.DrawRay(gapOrigin, Vector2.down * gapProbeDepth);

        // Ray wall
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay((Vector2)transform.position + Vector2.up * wallRayHeight, Vector2.right * wallRayLength);

        // Vùng tìm anchor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere((Vector2)transform.position + Vector2.right * anchorForwardBias, anchorSearchRadius);

        // Điểm anchor tốt nhất (khi chạy)
        if (Application.isPlaying && ctx != null && ctx.HasAnchorPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(ctx.BestAnchorPoint, 0.25f);
            Gizmos.DrawLine(transform.position, ctx.BestAnchorPoint);
        }
    }
}
