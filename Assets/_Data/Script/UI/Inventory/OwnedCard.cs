using UnityEngine;

[System.Serializable]
public class OwnedCard
{
    public CardDataSO data;
    public int level = 1;

    // Mảng lưu trữ tối đa 12 viên ngọc đang khảm trên thẻ này
    public InventoryItem[] equippedGems = new InventoryItem[12];

    public OwnedCard(CardDataSO data)
    {
        this.data = data;
    }

    // ==========================================
    // HỆ THỐNG TÍNH TOÁN CHỈ SỐ TỔNG (BASE + GEMS)
    // ==========================================

    public int GetTotalTop() => data.top + GetGemBonus(GemDirection.Top);
    public int GetTotalRight() => data.right + GetGemBonus(GemDirection.Right);
    public int GetTotalBottom() => data.bottom + GetGemBonus(GemDirection.Bottom);
    public int GetTotalLeft() => data.left + GetGemBonus(GemDirection.Left);

    // Hàm quét 12 lỗ ngọc để gom điểm buff
    private int GetGemBonus(GemDirection direction)
    {
        int totalBonus = 0;
        foreach (var gem in equippedGems)
        {
            // Kiểm tra lỗ có ngọc không, ngọc có data không, và hướng ngọc có khớp không
            if (gem != null && gem.data != null && gem.data.directionTag == direction)
            {
                totalBonus += gem.data.bonusStat;
            }
        }
        return totalBonus;
    }
}