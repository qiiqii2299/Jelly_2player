using UnityEngine;

public class SelectionSceneController : MonoBehaviour
{
    // Hàm này sẽ được gọi khi người chơi bấm nút Ready
    public void OnReadyButtonClicked()
    {
        // Gọi "Trạm trung chuyển" và báo cho nó biết điểm đến cuối cùng là GameplayScene
        SceneLoader.LoadNextScene("GameplayScene");
    }

}
