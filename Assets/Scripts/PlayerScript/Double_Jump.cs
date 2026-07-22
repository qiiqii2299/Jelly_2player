using UnityEngine;

/// <summary>
/// Chỉ còn nhiệm vụ track trạng thái chạm đất (Ground).
/// Movement và jump đã được xử lý bởi PlayerBase.
/// Không còn phụ thuộc WebSwing.
/// </summary>
public class Double_Jump : MonoBehaviour
{
    public bool Ground = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            Ground = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            Ground = false;
    }

    public void ResetJumpCount() { }
}
