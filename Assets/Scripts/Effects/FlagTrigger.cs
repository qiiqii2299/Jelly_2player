using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FlagTrigger : MonoBehaviour
{
    private bool hasFinished = false;

    [Header("UI Chiến Thắng")]
    public GameObject winPanel;
    public TextMeshProUGUI winText;

    [Header("Cấu hình tên Scene")]
    public string startSceneName = "StartScene";         // Tên scene màn hình chính (Nút Exit)
    public string selectionSceneName = "SelectionScene"; // Tên scene chọn nhân vật (Nút Selection)
    public string gameplaySceneName = "GameplayScene";   // Tên scene chơi hiện tại (Nút Restart)

    private void Start()
    {
        if (winPanel == null)
        {
            winPanel = GameObject.Find("WinPanel");
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false); // Ẩn panel khi mới vào game
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasFinished) return;

        PlayerBase player = collision.GetComponent<PlayerBase>();

        if (player != null && collision.CompareTag("Player"))
        {
            float playerX = collision.transform.position.x;
            float flagX = transform.position.x;

            if (playerX < flagX)
            {
                hasFinished = true;
                string winnerName = "NGƯỜI CHƠI";

                if (collision.GetComponent<BatmanBotAI>() != null)
                {
                    winnerName = "PLAYER 2 (BOT)";
                }
                else if (collision.gameObject.name.Contains("P2") || (GameManager.Instance != null && collision.transform.position.x > GameManager.Instance.spawnPointP2.position.x + 5f))
                {
                    winnerName = "PLAYER 2";
                }
                else
                {
                    winnerName = "PLAYER 1";
                }

                WinGame(winnerName);
            }
        }
    }

    private void WinGame(string winnerString)
    {
        Debug.Log("CHÚC MỪNG! " + winnerString + " ĐÃ VỀ ĐÍCH THÀNH CÔNG!");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (winText != null)
        {
            winText.text = winnerString + " YOU WIN!";
        }

        Time.timeScale = 0f; // Dừng thời gian game
    }

    // ==========================================
    // CÁC HÀM GẮN CHO 3 NÚT TRÊN WINPANEL
    // ==========================================

    // 1. Nút Restart: Chơi lại màn hiện tại
    public void RestartGame()
    {
        Time.timeScale = 1f; // Phục hồi thời gian game
        SceneManager.LoadScene(gameplaySceneName);
    }

    // 2. Nút Selection: Quay lại màn chọn nhân vật/map
    public void GoToSelectionScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(selectionSceneName);
    }

    // 3. Nút Exit: Quay trở lại màn hình chính StartScene
    public void GoToStartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }
}