using UnityEngine;

public enum GemDirection { None, Top, Bottom, Left, Right }

// ==========================================
// ENUM PHẨM CHẤT MÀU (Dùng cho Nâng cấp & Khung viền)
// ==========================================
public enum ColorLv
{
    White = 1,   // Trắng (Bình thường)
    Green = 2,   // Lục (Tốt)
    Blue = 3,    // Lam (Hiếm)
    Purple = 4,  // Tím (Sử thi)
    Gold = 5,    // Vàng (Huyền thoại)
    Orange = 6,  // Cam (Truyền thuyết)
    Red = 7      // Đỏ (Thần thoại)
}

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

    [Header("--- HỆ THỐNG NÂNG CẤP ---")]
    public ColorLv colorLevel = ColorLv.White;
    public ItemDataSO nextLevelGem; // Bỏ ngọc cấp tiếp theo vào đây (VD: Kéo SO Ngọc Lục vào SO Ngọc Trắng)

    [Header("--- THÔNG SỐ TINH THẠCH ---")]
    public GemDirection directionTag = GemDirection.None;
    public int bonusStat;

    public enum ItemType { Gem, Prop, Special }

    // HÀM TIỆN ÍCH: TỰ ĐỘNG XUẤT MÀU THEO PHẨM CHẤT
    public Color GetFrameColor()
    {
        switch (colorLevel)
        {
            case ColorLv.White: return Color.white;
            case ColorLv.Green: return Color.green;
            case ColorLv.Blue: return new Color(0.2f, 0.6f, 1f); // Xanh Lam
            case ColorLv.Purple: return new Color(0.7f, 0.3f, 1f); // Tím
            case ColorLv.Gold: return new Color(1f, 0.84f, 0f); // Vàng kim
            case ColorLv.Orange: return new Color(1f, 0.5f, 0f); // Cam
            case ColorLv.Red: return new Color(1f, 0.2f, 0.2f); // Đỏ
            default: return Color.white;
        }
    }
}