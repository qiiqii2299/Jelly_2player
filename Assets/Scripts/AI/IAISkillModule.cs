using UnityEngine;

/// <summary>
/// Interface mỗi nhân vật bot implement để xử lý kỹ năng riêng.
/// AIBrain gọi Tick() mỗi frame, truyền context môi trường vào.
/// Bot Spider-Man → SpiderManBotSkill
/// Bot Batman     → BatmanBotSkill
/// Nhân vật mới   → tạo thêm class implement interface này
/// </summary>
public interface IAISkillModule
{
    void Tick(AIContext ctx);
}

// -------------------------------------------------------
// Dữ liệu môi trường AIBrain thu thập và truyền cho skill
// -------------------------------------------------------
public class AIContext
{
    // Vị trí và tốc độ hiện tại
    public Vector2 Position;
    public Vector2 Velocity;

    // Trạng thái
    public bool IsGrounded;

    // Phát hiện khoảng hở phía trước
    public bool GapAhead;
    public float DistanceToGap;

    // Phát hiện tường / chướng ngại phía trước
    public bool WallAhead;
    public float DistanceToWall;

    // Điểm anchor tốt nhất tìm thấy phía trên-trước
    public bool HasAnchorPoint;
    public Vector2 BestAnchorPoint;
}
