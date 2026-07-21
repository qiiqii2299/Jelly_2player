using System.Collections;
using UnityEngine;

/// <summary>
/// Kỹ năng AI cho bot Batman — bắn móc cố định đến điểm phía trước-trên,
/// bị kéo đến đó rồi tiếp tục chạy. Không linh hoạt như Spider-Man.
/// Không dùng GrapplingHook của người chơi, implement riêng.
///
/// Gắn cùng prefab bot với AIBrain và BotMovement.
/// </summary>
[RequireComponent(typeof(BotMovement))]
public class BatmanBotSkill : MonoBehaviour, IAISkillModule
{
    [Header("Hook Settings")]
    public LayerMask hookableLayers;
    public float maxHookDistance  = 10f;
    public float hookMoveSpeed    = 5f;      // tốc độ bị kéo đến target
    public float hookShootSpeed   = 10f;     // tốc độ animation dây bay ra
    public LineRenderer hookLine;

    [Header("AI Timing")]
    [Tooltip("Khoảng cách đến gap bắt đầu bắn móc")]
    public float hookTriggerDistance = 3f;

    [Tooltip("Cooldown giữa các lần bắn (giây)")]
    public float hookCooldown = 2.5f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------
    private BotMovement movement;
    private float       cooldownTimer = 0f;
    private bool        isHooking     = false;

    void Awake()
    {
        movement = GetComponent<BotMovement>();
    }

    // -------------------------------------------------------
    // IAISkillModule
    // -------------------------------------------------------
    public void Tick(AIContext ctx)
    {
        cooldownTimer -= Time.deltaTime;

        // Đang retracting thì không làm gì thêm — coroutine đang xử lý
        if (isHooking) return;

        bool shouldHook = ctx.GapAhead
                       && ctx.DistanceToGap <= hookTriggerDistance
                       && ctx.HasAnchorPoint
                       && cooldownTimer <= 0f;

        if (shouldHook)
            StartCoroutine(FireHook(ctx.BestAnchorPoint));
    }

    // -------------------------------------------------------
    // Bắn móc: dây bay ra → kéo bot đến target → tiếp tục chạy
    // -------------------------------------------------------
    IEnumerator FireHook(Vector2 target)
    {
        isHooking = true;
        cooldownTimer = hookCooldown;

        // --- Phase 1: dây bay ra đến target ---
        if (hookLine != null)
        {
            hookLine.enabled      = true;
            hookLine.positionCount = 2;
            hookLine.SetPosition(0, transform.position);
            hookLine.SetPosition(1, transform.position);
        }

        float t    = 0f;
        float time = 10f;

        while (t < time)
        {
            t += hookShootSpeed * Time.deltaTime;
            float frac = Mathf.Clamp01(t / time);

            Vector2 tipPos = Vector2.Lerp(transform.position, target, frac);

            if (hookLine != null)
            {
                hookLine.SetPosition(0, transform.position);
                hookLine.SetPosition(1, tipPos);
            }

            // Móc chạm đến target → chuyển sang Phase 2
            if (Vector2.Distance(tipPos, target) < 0.3f) break;

            yield return null;
        }

        // --- Phase 2: kéo bot đến target ---
        while (Vector2.Distance(transform.position, target) > 0.5f)
        {
            transform.position = Vector2.Lerp(
                transform.position, target, hookMoveSpeed * Time.deltaTime);

            if (hookLine != null)
                hookLine.SetPosition(0, transform.position);

            yield return null;
        }

        // --- Xong: ẩn dây, tiếp tục chạy ---
        if (hookLine != null)
            hookLine.enabled = false;

        isHooking = false;
    }
}
