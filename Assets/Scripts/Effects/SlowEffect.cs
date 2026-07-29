using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn tạm lên Player khi bị trúng tên lửa Ironman (hoặc bất kỳ skill gây chậm nào).
/// Giảm moveSpeed xuống theo slowMultiplier trong duration giây rồi tự khôi phục.
/// Nếu bị Apply nhiều lần thì reset lại timer, không stack.
/// </summary>
public class SlowEffect : MonoBehaviour
{
    private Coroutine slowRoutine;
    private float     originalSpeed = -1f;  // -1 = chưa lưu

    /// <summary>
    /// Gọi từ skill muốn gây chậm.
    /// slowMultiplier: 0.5 = chậm 50%, 0.3 = chậm 70%
    /// </summary>
    public void Apply(float duration, float slowMultiplier = 0.5f)
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine(duration, slowMultiplier));
    }

    IEnumerator SlowRoutine(float duration, float slowMultiplier)
    {
        PlayerBase pb = GetComponent<PlayerBase>();

        if (pb != null)
        {
            // Lưu speed gốc lần đầu (tránh lưu speed đang bị slow)
            if (originalSpeed < 0f)
                originalSpeed = pb.moveSpeed;

            pb.moveSpeed = originalSpeed * slowMultiplier;
        }

        yield return new WaitForSeconds(duration);

        // Khôi phục
        if (pb != null && originalSpeed >= 0f)
            pb.moveSpeed = originalSpeed;

        originalSpeed = -1f;
        Destroy(this);
    }

    void OnDestroy()
    {
        // Safety: khôi phục nếu object bị destroy trước khi hết thời gian
        if (originalSpeed >= 0f)
        {
            PlayerBase pb = GetComponent<PlayerBase>();
            if (pb != null) pb.moveSpeed = originalSpeed;
        }
    }
}
