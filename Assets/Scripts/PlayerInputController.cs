using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public enum PlayerType { Player1, Player2, Custom }

    [Header("Loại Người Chơi")]
    public PlayerType playerType = PlayerType.Player1;

    [Header("Cấu hình phím di chuyển")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.W;

    [Header("Cấu hình phím skill (Input)")]
    public KeyCode skillKey = KeyCode.Space;
    public KeyCode secondarySkillKey = KeyCode.J;
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
            // P1 (bên trái bàn phím): WASD di chuyển, Space skill chính, J skill phụ
            leftKey           = KeyCode.A;
            rightKey          = KeyCode.D;
            jumpKey           = KeyCode.W;
            skillKey          = KeyCode.Space;
            secondarySkillKey = KeyCode.J;
            ropeInKey         = KeyCode.Z;
            ropeOutKey        = KeyCode.X;
        }
        else if (playerType == PlayerType.Player2)
        {
            // P2 (bên phải bàn phím): Mũi tên di chuyển, Numpad0 skill chính, Numpad4 skill phụ
            leftKey           = KeyCode.LeftArrow;
            rightKey          = KeyCode.RightArrow;
            jumpKey           = KeyCode.UpArrow;
            skillKey          = KeyCode.Keypad0;
            secondarySkillKey = KeyCode.Keypad4;
            ropeInKey         = KeyCode.Keypad1;
            ropeOutKey        = KeyCode.Keypad2;
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
    public bool IsSecondarySkillPressed => Input.GetKeyDown(secondarySkillKey);
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