using UnityEngine;

public enum GemDirection { None, Top, Bottom, Left, Right }

[CreateAssetMenu(fileName = "NewItem", menuName = "MythicNumbers/Item")]
public class ItemDataSO : ScriptableObject
{
    public string itemID;
    public string itemName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite icon;
    public Sprite frame;
    public Color itemColor = Color.white;

    [Header("Settings")]
    public ItemType type;
    public int maxStack = 99;

    [Header("--- THÔNG SỐ TINH THẠCH (Chỉ dùng cho Gem) ---")]
    public GemDirection directionTag = GemDirection.None; // Lỗ nào?
    public int bonusStat;

    public enum ItemType { Gem, Prop, Special }
}