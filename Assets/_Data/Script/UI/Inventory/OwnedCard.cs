using UnityEngine;

[System.Serializable]
public class OwnedCard
{
    public CardDataSO data;
    public int level = 1;

    // --- CÁC BIẾN MỚI CHO HỆ THỐNG NÂNG SAO ---
    public int starLevel = 0;
    public int currentShards = 0;

    // Mảng lưu trữ tối đa 12 viên ngọc đang khảm trên thẻ này
    public InventoryItem[] equippedGems = new InventoryItem[12];

    public OwnedCard(CardDataSO data)
    {
        this.data = data;
        starLevel = 0;
        currentShards = 0;
    }

    // ==========================================
    // CÔNG THỨC NÂNG SAO (RULE CONFIG)
    // ==========================================

    // Sếp muốn đổi luật lên sao thì chỉ việc sửa cái hàm này!
    // Trả về số lượng mảnh CẦN THIẾT để lên cấp sao tiếp theo.
    public int GetRequiredShardsForNextStar()
    {
        // Công thức của sếp: Bắt đầu từ 50, mỗi sao nhân đôi
        // 0->1: 50 | 1->2: 100 | 2->3: 200 | 3->4: 400 ...
        if (starLevel == 0) return 50;

        // Dùng phép dịch bit (<<) để nhân đôi cho nhẹ máy, hoặc sếp dùng Math.Pow cũng được
        return 50 * (int)Mathf.Pow(2, starLevel);
    }

    // Kiểm tra xem thẻ đã Max Sao chưa (Ví dụ: Max là 8 sao)
    public bool IsMaxStar()
    {
        return starLevel >= 8;
    }

    // ==========================================
    // HỆ THỐNG TÍNH TOÁN CHỈ SỐ TỔNG (BASE + GEMS + SAO)
    // ==========================================

    public int GetTotalTop() => CalculateStatWithStar(data.top) + GetGemBonus(GemDirection.Top);
    public int GetTotalRight() => CalculateStatWithStar(data.right) + GetGemBonus(GemDirection.Right);
    public int GetTotalBottom() => CalculateStatWithStar(data.bottom) + GetGemBonus(GemDirection.Bottom);
    public int GetTotalLeft() => CalculateStatWithStar(data.left) + GetGemBonus(GemDirection.Left);

    // Buff chỉ số gốc theo số Sao (Ví dụ: Mỗi sao tăng 10% chỉ số gốc)
    private int CalculateStatWithStar(int baseStat)
    {
        float multiplier = 1f + (starLevel * 0.1f); // 0 sao: x1 | 1 sao: x1.1 | 2 sao: x1.2
        return Mathf.RoundToInt(baseStat * multiplier);
    }

    private int GetGemBonus(GemDirection direction)
    {
        int totalBonus = 0;
        foreach (var gem in equippedGems)
        {
            if (gem != null && gem.data != null && gem.data.directionTag == direction)
            {
                totalBonus += gem.data.bonusStat;
            }
        }
        return totalBonus;
    }
}