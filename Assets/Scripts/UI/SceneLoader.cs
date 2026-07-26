using UnityEngine;
using UnityEngine.SceneManagement;

// Lớp static tồn tại toàn cục, KHÔNG gắn vào GameObject
public static class SceneLoader
{
    // Tên Scene đích đến (lưu lại map hoặc scene thực tế cần load sau khi qua màn Loading)
    public static string targetSceneName = "";

    // Lưu frame hoạt hình dở dang để nối tiếp nhịp màn Loading
    public static int lastLoadingFrame = 0;

    public static void LoadNextScene(string sceneName)
    {
        // Kiểm tra nếu tên scene truyền vào hợp lệ thì mới lưu
        if (!string.IsNullOrEmpty(sceneName))
        {
            targetSceneName = sceneName;
        }
        else
        {
            Debug.LogWarning("SceneLoader nhận được tên scene trống, tự động gán mặc định sang Factory Map!");
            targetSceneName = "Factory Map"; // Đổi thành tên map mặc định của bạn
        }

        // Luôn chuyển qua LoadingScene làm bước đệm trước khi vào map chính
        SceneManager.LoadScene("LoadingScene");
    }
}