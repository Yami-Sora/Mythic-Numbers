using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Mythic/Skills/SungSigma")]
public class SungSigma : RandomStatAbstract
{
    [Header("Config")]
    public int buffAmount = 5;
    public int totalDebuffAmount = 5;

    protected override int LuckySidesCount => 1;

    protected override void ApplyBuffLogic(CardObj caster, List<int> targetSides)
    {
        // Vì LuckySidesCount = 1, list này chắc chắn chỉ có 1 phần tử
        foreach (int side in targetSides)
        {
            ModifyStat(caster, side, buffAmount);
        }
    }

    protected override void ApplyDebuffLogic(CardObj caster, List<int> remainingSides)
    {
        int pointsToRemove = totalDebuffAmount;

        // Vòng lặp chạy khi còn điểm cần trừ VÀ còn cạnh có thể trừ
        while (pointsToRemove > 0 && remainingSides.Count > 0)
        {
            // 1. Chọn ngẫu nhiên một chỉ số TRONG DANH SÁCH
            int randomListIndex = Random.Range(0, remainingSides.Count);
            int targetSide = remainingSides[randomListIndex];

            // 2. Lấy giá trị hiện tại
            int currentVal = GetStat(caster, targetSide);

            if (currentVal > 0)
            {
                // 3. Trừ 1 điểm
                ModifyStat(caster, targetSide, -1);
                pointsToRemove--;

                // 4. KIỂM TRA NGAY: Nếu sau khi trừ mà về 0 -> Loại khỏi danh sách luôn
                if (GetStat(caster, targetSide) <= 0)
                {
                    remainingSides.RemoveAt(randomListIndex);
                }
            }
            else
            {
                // Trường hợp hy hữu: Cạnh đã là 0 từ đầu (do data gốc)
                // Loại ngay lập tức để không random trúng nữa
                remainingSides.RemoveAt(randomListIndex);
            }
        }
    }
}