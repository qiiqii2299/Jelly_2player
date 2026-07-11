using UnityEngine;
using UnityEngine.UI;
using TMPro; // Thư viện để thao tác với TextMeshPro

public class LoadingScreenController : MonoBehaviour
{
    [Header("--- THÀNH PHẦN UI ---")]
    public Slider loadingBar;               // Thanh Slider
    public RectTransform characterRect;     // Khung của nhân vật (để nảy)
    public Image characterImage;            // Hình của nhân vật (để đổi ảnh)
    public TMP_Text loadingText;            // Chữ Loading 

    [Header("--- HIỆU ỨNG NHÂN VẬT ---")]
    public float bounceHeight = 30f;        // Độ cao nảy
    public float bounceSpeed = 6f;          // Tốc độ nảy
    public float animFrameRate = 0.1f;      // Tốc độ lật ảnh (càng nhỏ càng nhanh)
    public Sprite[] characterSprites;       // Danh sách ảnh player_0 đến player_17

    [Header("--- HIỆU ỨNG CHỮ LOADING ---")]
    public float waveMultiplier = 5f;       // Độ cao của ngọn sóng từng chữ
    public float speedMultiplier = 4f;      // Tốc độ sóng cuộn

    private float charOriginalY;
    private float textOriginalY;
    private float timer;
    private int currentFrame = 0;

    void Start()
    {
        // Lưu lại vị trí đứng ban đầu của nhân vật và chữ
        if (characterRect != null) charOriginalY = characterRect.anchoredPosition.y;
        // Reset thanh Loading về 0
        if (loadingBar != null) loadingBar.value = 0f;
    }

    void Update()
    {
        // 1. Hoạt hình nhân vật
        if (characterSprites.Length > 0 && characterImage != null)
        {
            timer += Time.deltaTime;
            if (timer >= animFrameRate)
            {
                timer = 0f;
                currentFrame = (currentFrame + 1) % characterSprites.Length;
                characterImage.sprite = characterSprites[currentFrame];
            }
        }
        {
            timer += Time.deltaTime;
            if (timer >= animFrameRate)
            {
                timer = 0f;
                // Chuyển frame, nếu đến hình cuối thì quay lại hình 0
                currentFrame = (currentFrame + 1) % characterSprites.Length;
                characterImage.sprite = characterSprites[currentFrame];
            }
        }

        // 2. Nhân vật nảy
        if (characterRect != null)
        {
            float newCharY = charOriginalY + Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            characterRect.anchoredPosition = new Vector2(characterRect.anchoredPosition.x, newCharY);
        }
        {
            // Dùng Mathf.Abs để nhân vật chạm đất rồi nảy lên
            float newCharY = charOriginalY + Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            characterRect.anchoredPosition = new Vector2(characterRect.anchoredPosition.x, newCharY);
        }

        // 3. Thanh Loading giả lập
        if (loadingBar != null && loadingBar.value < 1f)
        {
            loadingBar.value += Time.deltaTime * 0.2f;
        }

        // 4. LƯỢN SÓNG TỪNG CHỮ CÁI (TEXTMESHPRO)
        if (loadingText != null)
        {
            // Bắt buộc TMP cập nhật lưới (mesh) trước khi ta bẻ cong nó
            loadingText.ForceMeshUpdate();

            // Lấy dữ liệu các chữ cái
            TMP_TextInfo textInfo = loadingText.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                // Lấy thông tin của từng ký tự
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // Nếu là khoảng trắng (space), bỏ qua không làm lượn sóng
                if (!charInfo.isVisible) continue;

                // Lấy vị trí 4 đỉnh (vertex) tạo nên 1 chữ cái đó
                int vertexIndex = charInfo.vertexIndex;
                int materialIndex = charInfo.materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                // Tính toán độ lượn sóng: Dựa vào thời gian (Time.time) và vị trí chữ (i) để tạo sóng đuổi nhau
                Vector3 offset = new Vector3(0, Mathf.Sin(Time.time * speedMultiplier + i) * waveMultiplier, 0);

                // Di chuyển 4 góc của chữ cái đó lên/xuống theo sóng
                vertices[vertexIndex + 0] += offset; // Góc dưới trái
                vertices[vertexIndex + 1] += offset; // Góc trên trái
                vertices[vertexIndex + 2] += offset; // Góc trên phải
                vertices[vertexIndex + 3] += offset; // Góc dưới phải
            }

            // Sau khi bẻ cong xong, cập nhật lại hình ảnh chữ lên màn hình
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].mesh != null)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    loadingText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }
            }
            {
                // Tốc độ thanh loading đầy (Thay đổi số 0.2f để nhanh/chậm hơn)
                loadingBar.value += Time.deltaTime * 0.2f;
            }
        }
    }
}