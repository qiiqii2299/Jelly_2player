using UnityEngine;
using Unity.Cinemachine;

public class SceneSpawner : MonoBehaviour
{
    [Header("Các điểm Spawn trong Scene này")]
    public Transform p1SpawnPoint;
    public Transform p2SpawnPoint;

    [Header("Camera Target Group của Scene này")]
    [Tooltip("Kéo object CameraTargetGroup từ Hierarchy vào đây")]
    public CinemachineTargetGroup sceneCameraTargetGroup;

    void Start()
    {
        // Kiểm tra xem GameManager đã tồn tại chưa
        if (GameManager.Instance != null)
        {
            // Truyền thông tin điểm spawn và Target Group vào GameManager
            GameManager.Instance.spawnPointP1 = p1SpawnPoint;
            GameManager.Instance.spawnPointP2 = p2SpawnPoint;
            GameManager.Instance.cameraTargetGroup = sceneCameraTargetGroup;

            Debug.Log("SceneSpawner đã truyền Target Group thành công: " + (sceneCameraTargetGroup != null));
            // Ra lệnh cho GameManager tiến hành sinh nhân vật, gán phím và cập nhật camera
            GameManager.Instance.SpawnPlayers();
        }
        else
        {
            // Debug.LogWarning("Không tìm thấy GameManager trong Scene! Hãy đảm bảo bạn bắt đầu game từ Selection Scene.");
        }
    }
}