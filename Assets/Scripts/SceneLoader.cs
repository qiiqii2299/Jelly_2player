using UnityEngine.SceneManagement;

// Lưu ý: Lớp static không thể gắn vào Game Object
public static class SceneLoader
{
    // Biến này sẽ lưu tên của Scene mà người chơi muốn đi tới
    public static string targetSceneName;

    // Hàm này được gọi từ các nút bấm (Button)
    public static void LoadNextScene(string sceneName)
    {
        // 1. Ghi nhớ điểm đến vào "đám mây"
        targetSceneName = sceneName;

        // 2. Tải màn hình Loading (Luôn luôn qua LoadingScene trước)
        SceneManager.LoadScene("LoadingScene");
    }
}