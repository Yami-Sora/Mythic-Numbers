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
        if (rawStat <= 0) return (0, Color.gray);

        // Dùng công thức (n-1) để mốc 99, 198, 297... luôn là số cuối cùng của 1 bậc màu
        int tier = (rawStat - 1) / 99;

        // Phép toán Modulo 99 để số luôn xoay vòng từ 1 đến 99
        int displayVal = ((rawStat - 1) % 99) + 1;

        // Lấy màu dựa trên Tier
        int colorIndex = Mathf.Clamp(tier, 0, RealmColors.Length - 1);
        Color color = RealmColors[colorIndex];

        return (displayVal, color);
    }
}