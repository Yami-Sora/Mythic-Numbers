using UnityEngine;

public static class RealmCalculator
{
    private static Color HexToColor(string hex, Color fallback)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;
        return fallback;
    }

    public static readonly Color[] RealmColors = new Color[]
    {
        Color.white,                                 // 0: Phàm Nhân (1-99)
        HexToColor("#50C878", Color.green),          // 1: Trúc Cơ (100-199) -> 100 = 1
        HexToColor("#007FFF", Color.blue),           // 2: Kim Đan (200-299) -> 200 = 1
        HexToColor("#9966CC", Color.magenta),        // 3: Nguyên Anh (300-399) -> 300 = 1, 301 = 2
        HexToColor("#FF8C00", Color.yellow),         // 4: Hóa Thần
        HexToColor("#FF0000", Color.red),            // 5: Đại Đế
        HexToColor("#FFD700", Color.yellow)          // 6+: Thần Thoại
    };

    public static (int displayValue, Color displayColor) GetRealmStat(int rawStat)
    {
        // Vẫn giữ logic cũ: Nếu chỉ số tụt xuống âm hoặc bằng 0 thì cho màu xám
        if (rawStat <= 0) return (0, Color.gray);

        // ----------------------------------------------------
        // CÔNG THỨC MỚI (Hệ cơ số 100 cực dễ tính)
        int tier = rawStat / 100;

        // Phép chia lấy dư % 100 sẽ luôn trả về kết quả từ 0 đến 99
        // VD: 100 % 100 = 0
        // VD: 105 % 100 = 5
        int displayVal = rawStat % 100;

        // Lấy màu dựa trên Tier (Giới hạn lại để không bị out of bounds nếu chỉ số quá to)
        int colorIndex = Mathf.Clamp(tier, 0, RealmColors.Length - 1);
        Color color = RealmColors[colorIndex];

        return (displayVal, color);
    }
}