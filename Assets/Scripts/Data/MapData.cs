using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    public string mapName;
    public Sprite mapPreviewSprite;
    public string sceneName;

    [Header("Map Status")]
    public bool isComingSoon;
}