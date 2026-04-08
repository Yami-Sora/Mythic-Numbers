using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Mythic/Item")]
public class ItemDataSO : ScriptableObject
{
    public string itemID;
    public string itemName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite icon;    // Hình vật phẩm
    public Sprite frame;   // Hình cái viền 
    public Color itemColor = Color.white; // Màu đặc trưng cho độ hiếm

    [Header("Settings")]
    public ItemType type;
    public int maxStack = 99;

    public enum ItemType { Gem, Prop, Special }
}