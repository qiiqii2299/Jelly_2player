using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Nếu dùng TextMeshPro
using System.Collections; // Thư viện bắt buộc để dùng Coroutine (hiệu ứng nảy)

public class SelectionManager : MonoBehaviour
{
    [Header("=== DATA LISTS ===")]
    [SerializeField] private CharacterData[] availableCharacters;
    [SerializeField] private MapData[] availableMaps;

    [Header("=== MAP UI REFERENCES ===")]
    [SerializeField] private Image mapPreviewImage;
    [SerializeField] private TMP_Text mapNameText; // Nếu dùng Text thường thì đổi thành Text

    [SerializeField] private Button readyButton;
    [SerializeField] private Image backgroundImage;      // Hình nền của Canvas
    [SerializeField] private GameObject comingSoonText; // Text Coming Soon

    [Header("=== PLAYER 1 UI REFERENCES ===")]
    [SerializeField] private Image p1AvatarImage;
    [SerializeField] private TMP_Text p1NameText;

    [Header("=== PLAYER 2 UI REFERENCES ===")]
    [SerializeField] private Image p2AvatarImage;
    [SerializeField] private TMP_Text p2NameText;

    [Header("=== PLAYER 2 MODE ===")]
    [SerializeField] private TMP_Text p2ModeText;
    private bool isP2Bot = true;

    // Các biến chỉ số hiện tại
    private int currentMapIndex = 0;
    private int currentP1Index = 0;
    private int currentP2Index = 1; // Cho P2 mặc định chọn nhân vật thứ 2

    private Coroutine mapBounce;
    private Coroutine p1Bounce;
    private Coroutine p2Bounce;

    [HideInInspector] public string nextSceneToLoad;

    private void Start()
    {
        UpdateMapUI();
        UpdateP1UI();
        UpdateP2UI();
        UpdateModeText();
    }

    #region MAP SELECTION
    public void NextMap()
    {
        currentMapIndex = (currentMapIndex + 1) % availableMaps.Length;
        UpdateMapUI();
    }

    public void PrevMap()
    {
        currentMapIndex--;
        if (currentMapIndex < 0) currentMapIndex = availableMaps.Length - 1;
        UpdateMapUI();
    }

    private void UpdateMapUI()
    {
        if (availableMaps.Length == 0) return;
        MapData currentMap = availableMaps[currentMapIndex];

        // 1. Cập nhật ảnh Preview và Tên Map
        mapPreviewImage.sprite = currentMap.mapPreviewSprite;
        mapNameText.text = currentMap.mapName;

        // 2. CẬP NHẬT ẢNH BACKGROUND
        if (backgroundImage != null)
        {
            backgroundImage.sprite = currentMap.mapPreviewSprite;
        }

        // 3. XỬ LÝ LOGIC COMING SOON
        if (currentMap.isComingSoon)
        {
            // Tắt nút Ready, Bật chữ Coming Soon
            if (readyButton != null) readyButton.interactable = false;
            if (comingSoonText != null) comingSoonText.SetActive(true);
        }
        else
        {
            // Bật lại nút Ready, Tắt chữ Coming Soon
            if (readyButton != null) readyButton.interactable = true;
            if (comingSoonText != null) comingSoonText.SetActive(false);
        }

        if (mapBounce != null) StopCoroutine(mapBounce);
        mapBounce = StartCoroutine(BounceRoutine(mapPreviewImage.transform));
    }
    #endregion

    #region PLAYER 1 SELECTION
    public void NextP1()
    {
        currentP1Index = (currentP1Index + 1) % availableCharacters.Length;
        UpdateP1UI();
    }

    public void PrevP1()
    {
        currentP1Index--;
        if (currentP1Index < 0) currentP1Index = availableCharacters.Length - 1;
        UpdateP1UI();
    }

    private void UpdateP1UI()
    {
        if (availableCharacters.Length == 0) return;
        p1AvatarImage.sprite = availableCharacters[currentP1Index].avatarSprite;
        p1NameText.text = availableCharacters[currentP1Index].characterName;

        if (p1Bounce != null) StopCoroutine(p1Bounce);
        p1Bounce = StartCoroutine(BounceRoutine(p1AvatarImage.transform));
    }
    #endregion

    #region PLAYER 2 SELECTION
    public void NextP2()
    {
        currentP2Index = (currentP2Index + 1) % availableCharacters.Length;
        UpdateP2UI();
    }

    public void PrevP2()
    {
        currentP2Index--;
        if (currentP2Index < 0) currentP2Index = availableCharacters.Length - 1;
        UpdateP2UI();
    }

    private void UpdateP2UI()
    {
        if (availableCharacters.Length == 0) return;
        p2AvatarImage.sprite = availableCharacters[currentP2Index].avatarSprite;
        p2NameText.text = availableCharacters[currentP2Index].characterName;

        // ĐÃ THÊM: Cập nhật hiệu ứng nảy cho P2
        if (p2Bounce != null) StopCoroutine(p2Bounce);
        p2Bounce = StartCoroutine(BounceRoutine(p2AvatarImage.transform));
    }
    #endregion

    #region PLAYER 2 MODE TOGGLE
    // --- HÀM XỬ LÝ KHI BẤM NÚT ĐỔI CHẾ ĐỘ ---
    public void ToggleP2Mode()
    {
        isP2Bot = !isP2Bot; // Đảo ngược trạng thái (Từ True sang False và ngược lại)
        UpdateModeText();
    }

    // Hàm phụ để cập nhật giao diện chữ
    private void UpdateModeText()
    {
        if (p2ModeText == null) return;

        if (isP2Bot)
        {
            p2ModeText.text = "Mode: BOT";

            // Dùng màu Đỏ sậm (Crimson/Dark Red) thay vì màu đỏ rực
            ColorUtility.TryParseHtmlString("#B22222", out Color darkRed);
            p2ModeText.color = darkRed;
        }
        else
        {
            p2ModeText.text = "Mode: PLAYER";

            // Dùng màu Xanh lá đậm (Forest Green) thay vì xanh phản quang
            ColorUtility.TryParseHtmlString("#228B22", out Color darkGreen);
            p2ModeText.color = darkGreen;
        }
    }
    #endregion


    #region READY / START GAME
    public void OnReadyButtonClicked()
    {
        // 1. Kiểm tra GameManager xem đã tồn tại trong Scene chưa
        if (GameManager.Instance == null)
        {
            Debug.LogError("LỖI: Không tìm thấy GameManager trong Scene hiện tại! Hãy chắc chắn bạn có đặt GameManager ở màn hình khởi đầu.");
            return;
        }

        // 2. Kiểm tra dữ liệu Map
        if (availableMaps == null || availableMaps.Length == 0 || availableMaps[currentMapIndex] == null)
        {
            Debug.LogError("LỖI: Danh sách Map đang bị trống hoặc dữ liệu Map tại vị trí hiện tại bị null!");
            return;
        }

        // 3. Kiểm tra dữ liệu Nhân vật P1 & P2
        if (availableCharacters == null || availableCharacters.Length == 0)
        {
            Debug.LogError("LỖI: Danh sách nhân vật (availableCharacters) đang bị trống!");
            return;
        }

        // --- BẮT ĐẦU LƯU DỮ LIỆU KHI ĐÃ AN TOÀN ---
        GameManager.Instance.selectedMap = availableMaps[currentMapIndex];
        GameManager.Instance.p1SelectedCharacter = availableCharacters[currentP1Index];
        GameManager.Instance.p2SelectedCharacter = availableCharacters[currentP2Index];

        // Lưu trạng thái Bot hay Player
        GameManager.Instance.isPlayer2Bot = isP2Bot;

        // Lấy tên Scene Map đã chọn
        string sceneToLoad = availableMaps[currentMapIndex].sceneName;
        GameManager.Instance.nextSceneToLoad = sceneToLoad;

        // Gọi qua SceneLoader để chuyển cảnh mượt mà qua Loading
        SceneLoader.LoadNextScene(sceneToLoad);
    }
    #endregion


    #region ANIMATION ROUTINES
    // Phần thân của hàm BounceRoutine để tạo hiệu ứng nảy
    private IEnumerator BounceRoutine(Transform targetTransform)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.25f; // Nảy to lên 25%

        float timeToScale = 0.1f; // Thời gian nảy
        float timer = 0f;

        // Phóng to
        while (timer < timeToScale)
        {
            timer += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(originalScale, targetScale, timer / timeToScale);
            yield return null;
        }

        timer = 0f;

        // Thu nhỏ về bình thường
        while (timer < timeToScale)
        {
            timer += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(targetScale, originalScale, timer / timeToScale);
            yield return null;
        }

        targetTransform.localScale = originalScale;
    }
    #endregion
}