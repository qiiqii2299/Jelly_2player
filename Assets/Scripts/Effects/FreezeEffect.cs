using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn tạm lên Player khi bị trúng khiên Captain America.
/// Freeze nhân vật (đứng yên) trong duration giây rồi tự gỡ.
/// Tự xử lý nếu bị Apply nhiều lần — reset lại timer.
/// </summary>
public class FreezeEffect : MonoBehaviour
{
    private Rigidbody2D  rb;
    private Coroutine    freezeRoutine;
    private float        savedGravity;
    private bool         isFrozen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Gọi từ ShieldProjectile khi trúng Player.
    /// Nếu đang bị freeze rồi thì reset lại thời gian.
    /// </summary>
    public void Apply(float duration)
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        // --- Bắt đầu freeze ---
        isFrozen = true;

        if (rb != null)
        {
            savedGravity        = rb.gravityScale;
            rb.gravityScale     = 0f;
            rb.linearVelocity   = Vector2.zero;
            rb.constraints      = RigidbodyConstraints2D.FreezeAll;
        }

        // Disable PlayerBase và các controller để nhân vật không tự di chuyển
        SetControllersEnabled(false);

        yield return new WaitForSeconds(duration);

        // --- Kết thúc freeze ---
        if (rb != null)
        {
            rb.gravityScale = savedGravity;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation; // chỉ giữ không xoay
        }

        SetControllersEnabled(true);
        isFrozen = false;

        // Tự xóa component khỏi target khi xong
        Destroy(this);
    }

    void SetControllersEnabled(bool enabled)
    {
        // Disable/Enable PlayerBase để ngăn di chuyển
        PlayerBase pb = GetComponent<PlayerBase>();
        if (pb != null) pb.enabled = enabled;

        // Disable/Enable các controller nhân vật khác nếu có
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s == this) continue;
            if (s is PlayerBase) continue; // đã xử lý ở trên
            // Chỉ disable các script có tên chứa "Controller" hoặc "Skill"
            string typeName = s.GetType().Name;
            if (typeName.Contains("Controller") || typeName.Contains("Skill"))
                s.enabled = enabled;
        }
    }
}
