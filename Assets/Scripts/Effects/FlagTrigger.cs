using UnityEngine;

public class FlagTrigger : MonoBehaviour
{
    private bool hasFinished = false; // Đảm bảo chỉ tính thắng 1 lần duy nhất

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng va chạm có phải là Player không
        if (collision.CompareTag("Player") && !hasFinished)
        {
            BatmanController batman = collision.GetComponent<BatmanController>();
            if (batman != null)
            {
                // ĐIỀU KIỆN CHỐNG ĂN GIAN:
                // 1. Nhân vật phải đang quay mặt/di chuyển hướng sang phải (currentDirection > 0)
                // 2. Hoặc kiểm tra vị trí X của Player phải nhỏ hơn vị trí X của cờ (lao từ trái sang phải)

                float playerX = batman.transform.position.x;
                float flagX = transform.position.x;

                if (playerX < flagX)
                {
                    hasFinished = true;
                    WinGame();
                }
                else
                {
                    Debug.Log("Đang tiếp cận cờ từ phía sau (chạy ngược chiều)! Không tính thắng.");
                }
            }
        }
    }

    private void WinGame()
    {
        Debug.Log("CHÚC MỪNG! ĐÃ VỀ ĐÍCH THÀNH CÔNG!");

        // Dừng thời gian game hoặc mở Panel Chiến Thắng ở đây
        // Time.timeScale = 0f;
    }
}