using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public enum PlayerType { Player1, Player2, Custom }

    [Header("Loại Người Chơi")]
    public PlayerType playerType = PlayerType.Player1;

    [Header("Cấu hình phím di chuyển")]
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;
    public KeyCode jumpKey = KeyCode.UpArrow;

    [Header("Cấu hình phím skill (Input)")]
    public KeyCode skillKey = KeyCode.Space;
    public KeyCode ropeInKey = KeyCode.Z;
    public KeyCode ropeOutKey = KeyCode.X;

    void Awake()
    {
        ApplyDefaultKeys();
    }

    public void ApplyDefaultKeys()
    {
        if (playerType == PlayerType.Player1)
        {
            // Player 1: Di chuyển bằng Mũi tên + Skill dùng Space, Z, X
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            jumpKey = KeyCode.UpArrow;
            skillKey = KeyCode.Space;
            ropeInKey = KeyCode.Z;
            ropeOutKey = KeyCode.X;
        }
        else if (playerType == PlayerType.Player2)
        {
            // Player 2: Di chuyển bằng WASD + Skill dùng LeftShift, J, K (Hoàn toàn độc lập với P1)
            leftKey = KeyCode.A;
            rightKey = KeyCode.D;
            jumpKey = KeyCode.W;
            skillKey = KeyCode.LeftShift;
            ropeInKey = KeyCode.J;
            ropeOutKey = KeyCode.K;
        }
    }

    // Hàm cho phép GameManager cấu hình tùy chỉnh từ xa nếu cần
    public void InitializeInput(KeyBinding newKeys)
    {
        playerType = PlayerType.Custom;
        skillKey = newKeys.skillKey;
        ropeInKey = newKeys.ropeInKey;
        ropeOutKey = newKeys.ropeOutKey;
    }

    // Thuộc tính di chuyển để PlayerBase đọc vào
    public bool IsLeftHeld => Input.GetKey(leftKey);
    public bool IsRightHeld => Input.GetKey(rightKey);
    public bool IsJumpPressed => Input.GetKeyDown(jumpKey);

    // Thuộc tính skill
    public bool IsSkillPressed => Input.GetKeyDown(skillKey);
    public bool IsRopeInHeld => Input.GetKey(ropeInKey);
    public bool IsRopeOutHeld => Input.GetKey(ropeOutKey);
}

// Cấu trúc gói gọn bộ phím nếu cần truyền dữ liệu mở rộng
[System.Serializable]
public struct KeyBinding
{
    public KeyCode skillKey;
    public KeyCode ropeInKey;
    public KeyCode ropeOutKey;

    public KeyBinding(KeyCode skill, KeyCode ropeIn, KeyCode ropeOut)
    {
        skillKey = skill;
        ropeInKey = ropeIn;
        ropeOutKey = ropeOut;
    }
}