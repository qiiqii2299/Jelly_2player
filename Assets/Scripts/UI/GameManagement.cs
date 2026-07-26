using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dữ liệu lựa chọn từ Selection Scene")]
    [HideInInspector] public CharacterData p1SelectedCharacter;
    [HideInInspector] public CharacterData p2SelectedCharacter;
    [HideInInspector] public MapData selectedMap;

    [HideInInspector] public string nextSceneToLoad;

    [Header("Cấu hình chế độ chơi")]
    [Tooltip("Tích chọn (true) nếu Player 2 là Bot, bỏ tích (false) nếu muốn chơi 2 người (PvsP)")]
    public bool isPlayer2Bot = false;

    [HideInInspector] public Transform spawnPointP1;
    [HideInInspector] public Transform spawnPointP2;

    [Header("Cấu hình Camera (Cinemachine)")]
    [Tooltip("Được truyền tự động từ SceneSpawner khi vào map")]
    public CinemachineTargetGroup cameraTargetGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpawnPlayers()
    {
        GameObject p1Obj = null;
        GameObject p2Obj = null;

        // 1. Spawn Player 1 dựa trên CharacterData đã chọn (Luôn dùng playerPrefab)
        if (spawnPointP1 != null && p1SelectedCharacter != null)
        {
            p1Obj = Instantiate(p1SelectedCharacter.playerPrefab, spawnPointP1.position, Quaternion.identity);

            PlayerInputController p1Input = p1Obj.GetComponent<PlayerInputController>();
            if (p1Input != null)
            {
                p1Input.playerType = PlayerInputController.PlayerType.Player1;
                p1Input.ApplyDefaultKeys(); // Gán phím Mũi tên, Space, Z, X
            }
        }
        else
        {
            Debug.LogWarning("Thiếu điểm Spawn P1 hoặc P1 chưa chọn nhân vật!");
        }

        // 2. Spawn Player 2 dựa trên CharacterData đã chọn và chế độ PvsP hay PvsBot
        if (spawnPointP2 != null && p2SelectedCharacter != null)
        {
            GameObject p2PrefabToSpawn;

            // NẾU LÀ BOT VÀ ĐÃ CÓ CONFIG BOT PREFAB (ĐÃ LÀM Ở CÁCH 1), HỆ THỐNG SỰ TỰ ĐỘNG LẤY DÙNG
            if (isPlayer2Bot && p2SelectedCharacter.botPrefab != null)
            {
                p2PrefabToSpawn = p2SelectedCharacter.botPrefab;
            }
            else
            {
                p2PrefabToSpawn = p2SelectedCharacter.playerPrefab;
            }

            p2Obj = Instantiate(p2PrefabToSpawn, spawnPointP2.position, Quaternion.identity);

            // NẾU KHÔNG PHẢI BOT (TỨC LÀ P2 LÀ NGƯỜI CHƠI THẬT), GÁN PHÍM ĐIỀU KHIỂN BÌNH THƯỜNG
            if (!isPlayer2Bot)
            {
                PlayerInputController p2Input = p2Obj.GetComponent<PlayerInputController>();
                if (p2Input != null)
                {
                    p2Input.playerType = PlayerInputController.PlayerType.Player2;
                    p2Input.ApplyDefaultKeys(); // Tự động gán phím A, D, W, LeftShift, J, K
                }
            }
            else
            {
                Debug.Log("Player 2 hoạt động ở chế độ BOT (Sử dụng Prefab chuyên dụng độc lập).");
            }
        }
        else
        {
            Debug.LogWarning("Thiếu điểm Spawn P2 hoặc P2 chưa chọn nhân vật!");
        }

        // 3. Đưa cả 2 nhân vật vào Camera Target Group của Cinemachine
        if (cameraTargetGroup != null && p1Obj != null && p2Obj != null)
        {
            if (cameraTargetGroup.Targets == null)
            {
                cameraTargetGroup.Targets = new System.Collections.Generic.List<CinemachineTargetGroup.Target>();
            }

            cameraTargetGroup.Targets.Clear();
            cameraTargetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = p1Obj.transform, Weight = 1f, Radius = 1f });
            cameraTargetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = p2Obj.transform, Weight = 1f, Radius = 1f });
        }
    }
}