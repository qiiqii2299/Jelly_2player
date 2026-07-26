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

    // THÊM BIẾN NÀY ĐỂ ĐỒNG BỘ VỚI SCENELOADER VÀ TRÁNH LỖI CS1061
    [HideInInspector] public string nextSceneToLoad;

    [Header("Cấu hình chế độ chơi")]
    [Tooltip("Tích chọn (true) nếu Player 2 là Bot, bỏ tích (false) nếu muốn chơi 2 người (PvsP)")]
    public bool isPlayer2Bot = false; // Mặc định để false để test 2 bàn phím ngay

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

            if (isPlayer2Bot && p2SelectedCharacter.botPrefab != null)
            {
                // Nếu là Bot và có cấu hình botPrefab riêng thì dùng botPrefab
                p2PrefabToSpawn = p2SelectedCharacter.botPrefab;
            }
            else
            {
                // Ngược lại dùng playerPrefab chung
                p2PrefabToSpawn = p2SelectedCharacter.playerPrefab;
            }

            p2Obj = Instantiate(p2PrefabToSpawn, spawnPointP2.position, Quaternion.identity);

            PlayerInputController p2Input = p2Obj.GetComponent<PlayerInputController>();
            if (p2Input != null)
            {
                if (isPlayer2Bot)
                {
                    p2Input.enabled = false; // Tắt nhận phím để nhường quyền cho AI Controller
                    // TODO: Thêm lệnh bật AI cho P2 ở đây nếu cần
                }
                else
                {
                    p2Input.playerType = PlayerInputController.PlayerType.Player2;
                    p2Input.ApplyDefaultKeys(); // Tự động gán phím A, D, W, LeftShift, J, K
                }
            }
        }
        else
        {
            Debug.LogWarning("Thiếu điểm Spawn P2 hoặc P2 chưa chọn nhân vật!");
        }

        // 3. Đưa cả 2 nhân vật vào Camera Target Group của Cinemachine
        if (cameraTargetGroup != null && p1Obj != null && p2Obj != null)
        {
            // Đảm bảo list/array đã có đủ 2 ô
            if (cameraTargetGroup.Targets == null)
            {
                cameraTargetGroup.Targets = new System.Collections.Generic.List<CinemachineTargetGroup.Target>();
            }

            // Nếu danh sách chưa đủ 2 phần tử thì thêm mới, ngược lại gán đè trực tiếp
            cameraTargetGroup.Targets.Clear();
            cameraTargetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = p1Obj.transform, Weight = 1f, Radius = 1f });
            cameraTargetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = p2Obj.transform, Weight = 1f, Radius = 1f });
        }
    }
}