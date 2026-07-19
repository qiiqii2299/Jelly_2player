using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [Header("--- THÀNH PHẦN UI ---")]
    public Slider loadingBar;
    public RectTransform characterRect;
    public Image characterImage;
    public TMP_Text loadingText;

    [Header("--- HIỆU ỨNG NHÂN VẬT ---")]
    public float bounceHeight = 30f;
    public float bounceSpeed = 6f;
    public float animFrameRate = 0.1f;
    public Sprite[] characterSprites;

    [Header("--- HIỆU ỨNG CHỮ LOADING ---")]
    public float waveMultiplier = 5f;
    public float speedMultiplier = 4f;

    [Header("--- CÀI ĐẶT THỜI GIAN ---")]
    [Tooltip("Thời gian TỐI THIỂU để thanh loading chạy (giây)")]
    public float minimumLoadTime = 2.5f;

    private float charOriginalY;
    private float timer;
    private int currentFrame = 0;

    // Tránh gọi load scene nhiều lần
    private bool isLoadingStarted = false;

    void Start()
    {
        if (characterRect != null) charOriginalY = characterRect.anchoredPosition.y;
        if (loadingBar != null) loadingBar.value = 0f;

        // Phục hồi frame ảnh cũ từ SceneLoader (để mượt mà khi đổi scene)
        currentFrame = SceneLoader.lastLoadingFrame;

        if (characterSprites != null && characterSprites.Length > 0)
        {
            if (currentFrame >= characterSprites.Length) currentFrame = 0;
            if (characterImage != null) characterImage.sprite = characterSprites[currentFrame];
        }
    }

    void Update()
    {
        // 1. Hoạt hình nhân vật & Lưu frame lên Đám mây
        if (characterSprites.Length > 0 && characterImage != null)
        {
            timer += Time.deltaTime;
            if (timer >= animFrameRate)
            {
                timer = 0f;
                currentFrame = (currentFrame + 1) % characterSprites.Length;
                characterImage.sprite = characterSprites[currentFrame];

                SceneLoader.lastLoadingFrame = currentFrame;
            }
        }

        // 2. Nhân vật nảy
        if (characterRect != null)
        {
            float newCharY = charOriginalY + Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            characterRect.anchoredPosition = new Vector2(characterRect.anchoredPosition.x, newCharY);
        }

        // 3. LƯỢN SÓNG TỪNG CHỮ CÁI (TEXTMESHPRO)
        if (loadingText != null)
        {
            loadingText.ForceMeshUpdate();
            TMP_TextInfo textInfo = loadingText.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int vertexIndex = charInfo.vertexIndex;
                int materialIndex = charInfo.materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                Vector3 offset = new Vector3(0, Mathf.Sin(Time.time * speedMultiplier + i) * waveMultiplier, 0);

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].mesh != null)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    loadingText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }
            }
        }

        // 4. BẮT ĐẦU TẢI SCENE THẬT 
        if (!isLoadingStarted)
        {
            isLoadingStarted = true;

            if (!string.IsNullOrEmpty(SceneLoader.targetSceneName))
            {
                StartCoroutine(LoadSceneAsyncCoroutine(SceneLoader.targetSceneName));
            }
            else
            {
                Debug.LogWarning("Không có tên Scene đích đến!");
            }
        }
    }

    // Coroutine xử lý tải ngầm và đồng bộ thanh tiến trình
    private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string targetScene)
    {
        UnityEngine.AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false; // Chặn chuyển cảnh đột ngột

        float timeElapsed = 0f;

        while (!operation.isDone)
        {
            timeElapsed += Time.deltaTime;

            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float fakeProgress = Mathf.Clamp01(timeElapsed / minimumLoadTime);

            // Ép thanh Loading chạy từ từ theo fakeProgress
            float displayProgress = Mathf.Min(realProgress, fakeProgress);

            if (loadingBar != null)
            {
                loadingBar.value = displayProgress;
            }

            // Chuyển cảnh khi load xong data VÀ đủ thời gian làm màu
            if (operation.progress >= 0.9f && timeElapsed >= minimumLoadTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}