using UnityEngine;

public class BatmanBotAI : MonoBehaviour
{
    private GrabblingHook hookScript;
    private Rigidbody2D rb;

    [Header("Cấu hình hành vi di chuyển của Bot")]
    public float moveSpeed = 5f; // Thêm tốc độ chạy cho bot

    [Header("Cấu hình thời gian hành vi của Bot")]
    public float timeToReleaseHook = 1.2f; // Thời gian đu dây trước khi tự động phóng ra bay đi
    private float hookTimer = 0f;

    void Start()
    {
        hookScript = GetComponent<GrabblingHook>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. Kiểm tra an toàn: Nếu không có GameManager hoặc chưa bật chế độ Bot thì Bot đứng im/không chạy logic AI
        if (GameManager.Instance == null || !GameManager.Instance.isPlayer2Bot)
        {
            return;
        }

        if (hookScript == null || rb == null) return;

        // 2. Logic tự động hóa Móc câu cho Bot
        bool hasTarget = hookScript.HasTarget();
        bool isPulling = hookScript.IsPulling();

        if (!isPulling)
        {
            // --- TỰ ĐỘNG DI CHUYỂN TIẾN VỀ PHÍA TRƯỚC ---
            float moveDir = 1f; // Giả sử đích đến nằm ở bên phải (nếu map chạy ngược lại thì đổi thành -1f)
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

            // Xoay mặt nhân vật theo hướng chạy
            if (moveDir > 0) transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (moveDir < 0) transform.rotation = Quaternion.Euler(0, 180, 0);

            // Nếu nhìn thấy điểm bám hợp lý phía trước/trên đầu, Bot tự động bắn móc (tương đương nhấn Space lần 1)
            if (hasTarget)
            {
                hookScript.BotInput_TriggerSpace();
                hookTimer = 0f; // Reset lại bộ đếm thời gian đu dây
            }
        }
        else
        {
            // Đang trong trạng thái bám dây, đếm thời gian để chuẩn bị phóng người lấy đà bay đi
            hookTimer += Time.deltaTime;
            if (hookTimer >= timeToReleaseHook)
            {
                // Tự động nhấn Space lần 2 để phóng người bay qua chướng ngại vật/về đích
                hookScript.BotInput_TriggerSpace();
            }
        }
    }
}