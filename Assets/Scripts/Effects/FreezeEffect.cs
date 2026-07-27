using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn tạm lên Player khi bị trúng chiêu gây choáng (Hulk đấm, Captain America khiên,...)
/// Bất động nhân vật trong duration giây rồi tự gỡ.
/// Nếu bị Apply nhiều lần thì reset lại timer.
/// </summary>
public class FreezeEffect : MonoBehaviour
{
    private Coroutine freezeRoutine;

    /// <summary>
    /// Gọi từ bất kỳ skill nào muốn gây choáng.
    /// Nếu đang bị freeze thì reset lại thời gian.
    /// </summary>
    public void Apply(float duration)
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        SetFrozenState(true);

        yield return new WaitForSeconds(duration);

        SetFrozenState(false);

        Destroy(this);
    }

    void SetFrozenState(bool frozen)
    {
        // --- PlayerBase xử lý Rigidbody + flag chặn input ---
        PlayerBase pb = GetComponent<PlayerBase>();
        if (pb != null)
        {
            pb.SetFrozen(frozen);
        }

        // --- Chặn các Controller nhân vật (HulkController, SpidermanController,...) ---
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s == null || s == this) continue;
            if (s is PlayerBase || s is PlayerInputController) continue; // PlayerBase đã xử lý riêng

            string typeName = s.GetType().Name;
            if (typeName.Contains("Controller") || typeName.Contains("Skill"))
                s.enabled = !frozen;
        }
    }
}
