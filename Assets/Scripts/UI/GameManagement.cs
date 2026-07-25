using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [HideInInspector] public CharacterData p1SelectedCharacter;
    [HideInInspector] public CharacterData p2SelectedCharacter;
    [HideInInspector] public MapData selectedMap;

    // ĐÃ THÊM: Biến lưu trữ trạng thái chế độ của Player 2
    [HideInInspector] public bool isPlayer2Bot = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Không bị xóa khi chuyển Scene
    }
}