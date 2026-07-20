using UnityEngine;

public class MenuController : MonoBehaviour
{
    // Hàm này bắt buộc phải có chữ "public" ở đầu thì nút bấm mới nhìn thấy được
    public void GoToSelectionScene()
    {
        // Gọi "Trạm trung chuyển" và báo cho nó biết điểm đến tiếp theo là SelectionScene
        SceneLoader.LoadNextScene("SelectionScene");
    }
}