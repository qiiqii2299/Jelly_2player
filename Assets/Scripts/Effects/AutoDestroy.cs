using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Header("Thời gian tồn tại (giây)")]
    public float destroyDelay = 0.2f; 

    void Start()
    {
        // Lệnh này sẽ tự động xóa (tiêu hủy) GameObject này khỏi màn hình sau khoảng thời gian destroyDelay[cite: 1, 2]
        Destroy(gameObject, destroyDelay);
    }
}