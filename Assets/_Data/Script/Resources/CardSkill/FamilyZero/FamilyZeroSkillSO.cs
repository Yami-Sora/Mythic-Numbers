using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Mythic/Skills/FamilyZero")]
public class FamilyZeroSkillSO : BaseSkillSO
{
    public override void Execute(GameManagerNet gm, CardNet card, int slotIndex)
    {
        // 1. Lấy danh sách các chỉ số hiện tại để tìm Min/Max
        List<int> currentStats = new List<int> { card.Top, card.Right, card.Bottom, card.Left };

        int minVal = currentStats.Min();
        int maxVal = currentStats.Max();

        // Lưu ý: Random.Range(int min, int max) trong Unity không lấy giá trị max, nên ta cần cộng thêm 1

        card.Top = Random.Range(minVal, maxVal + 1);
        card.Right = Random.Range(minVal, maxVal + 1);
        card.Bottom = Random.Range(minVal, maxVal + 1);
        card.Left = Random.Range(minVal, maxVal + 1);
    }
}