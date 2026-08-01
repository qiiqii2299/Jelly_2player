using UnityEngine;

/// <summary>
/// Hất nhân vật thẳng lên trên — không làm ngã, không xô ngang.
/// Gọi KnockupEffect.Apply(target, force) từ bất kỳ projectile nào.
/// </summary>
public class KnockupEffect : MonoBehaviour
{
    [Tooltip("Lực hất lên (units/s)")]
    public float knockupForce = 10f;

    /// <summary>
    /// Gọi static từ bên ngoài — không cần component trên target.
    /// </summary>
    public static void Apply(GameObject target, float force)
    {
        if (target == null) return;

        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Chỉ set vận tốc dọc, giữ nguyên ngang → không xô, không nghiêng
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);

        // Đảm bảo không bị xoay
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// Gọi instance nếu muốn gắn component lên prefab và config từ Inspector.
    /// </summary>
    public void ApplyToTarget(GameObject target)
    {
        Apply(target, knockupForce);
    }
}
