using UnityEngine;

public class FlagTrigger : MonoBehaviour
{
    private bool hasFinished = false; // Đảm bảo chỉ tính thắng 1 lần duy nhất

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng va chạm có phải là Player không
        if (collision.CompareTag("Player") && !hasFinished)
        {
            float playerX = collision.transform.position.x;
            float flagX = transform.position.x;

            // Kiểm tra xem người chạm cờ là Batman hay Spider-Man
            BatmanController batman = collision.GetComponent<BatmanController>();
            SpiderManController spiderMan = collision.GetComponent<SpiderManController>();

            // Nếu một trong hai nhân vật tồn tại và lao tới từ bên trái sang phải
            if ((batman != null || spiderMan != null) && playerX < flagX)
            {
                hasFinished = true;
                WinGame();
            }
            else if (batman != null || spiderMan != null)
            {
                Debug.Log("Đang tiếp cận cờ từ phía sau (chạy ngược chiều)! Không tính thắng.");
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