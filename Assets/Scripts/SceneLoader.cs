using UnityEngine.SceneManagement;

// Lớp static tồn tại toàn cục, KHÔNG gắn vào GameObject
public static class SceneLoader
{
    // Tên Scene đích đến
    public static string targetSceneName;

    // Lưu frame hoạt hình dở dang để nối tiếp nhịp màn Loading
    public static int lastLoadingFrame = 0;

    public static void LoadNextScene(string sceneName)
    {
        targetSceneName = sceneName;

        // Luôn chuyển qua LoadingScene làm bước đệm
        SceneManager.LoadScene("LoadingScene");
    }
}