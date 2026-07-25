using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite avatarSprite;
    public GameObject playerPrefab; // Prefab cho người chơi điều khiển
    public GameObject botPrefab;    // (TÙY CHỌN) Prefab dành riêng cho Bot nếu bạn tách riêng, hoặc dùng chung Prefab và bật/tắt script AI.
}