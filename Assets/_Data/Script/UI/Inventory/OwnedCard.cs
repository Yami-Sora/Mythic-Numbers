using UnityEngine;

[System.Serializable]
public class OwnedCard
{
    public CardDataSO data;
    public int level = 1;

    // Mảng lưu trữ tối đa 12 viên ngọc đang khảm trên thẻ này
    public InventoryItem[] equippedGems = new InventoryItem[12];

    // Constructor dùng để khởi tạo thẻ mới khi Roll Gacha hoặc nhận thưởng
    public OwnedCard(CardDataSO data)
    {
        this.data = data;
    }
}