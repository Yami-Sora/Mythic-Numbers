using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class RandomStatAbstract : BaseSkillSO
{
    // Quy định số lượng cạnh may mắn sẽ được Buff
    protected abstract int LuckySidesCount { get; }

    public override void Execute(GameManagerNet gm, CardNet caster, int slotIndex)
    {
        // 1. Tạo danh sách 4 hướng (0, 1, 2, 3)
        List<int> allSides = new List<int> { 0, 1, 2, 3 };

        // 2. Xáo trộn danh sách để lấy ngẫu nhiên
        Shuffle(allSides);

        // 3. Tách làm 2 phe: May mắn (Buff) và Xui xẻo (Debuff)
        List<int> luckySides = allSides.Take(LuckySidesCount).ToList();
        List<int> unluckySides = allSides.Skip(LuckySidesCount).ToList();

        // 4. Log thông tin
        string luckyNames = string.Join(", ", luckySides.Select(SideName));
        Debug.Log($"[Skill] {caster.name} kích hoạt! Cạnh Buff: [{luckyNames}]");

        // 5. Gọi hàm abstract để lớp con tự xử lý logic cộng trừ
        ApplyBuffLogic(caster, luckySides);
        ApplyDebuffLogic(caster, unluckySides);
    }

    // --- CÁC HÀM TRỪU TƯỢNG ĐỂ CON CÁI TỰ ĐỊNH NGHĨA ---

    // Logic xử lý cho các cạnh được chọn (VD: Cộng 5 điểm)
    protected abstract void ApplyBuffLogic(CardNet caster, List<int> targetSides);

    // Logic xử lý cho các cạnh còn lại (VD: Trừ điểm, hoặc Set về 0)
    protected abstract void ApplyDebuffLogic(CardNet caster, List<int> remainingSides);


    // --- CÁC HÀM TIỆN ÍCH (HELPER) DÙNG CHUNG ---

    protected void ModifyStat(CardNet card, int sideIndex, int amount)
    {
        switch (sideIndex)
        {
            case 0: card.Top += amount; break;
            case 1: card.Right += amount; break;
            case 2: card.Bottom += amount; break;
            case 3: card.Left += amount; break;
        }
    }

    protected void SetStat(CardNet card, int sideIndex, int value)
    {
        switch (sideIndex)
        {
            case 0: card.Top = value; break;
            case 1: card.Right = value; break;
            case 2: card.Bottom = value; break;
            case 3: card.Left = value; break;
        }
    }

    protected int GetStat(CardNet card, int sideIndex)
    {
        switch (sideIndex)
        {
            case 0: return card.Top;
            case 1: return card.Right;
            case 2: return card.Bottom;
            case 3: return card.Left;
            default: return 0;
        }
    }

    protected string SideName(int index)
    {
        switch (index) { case 0: return "Top"; case 1: return "Right"; case 2: return "Bottom"; default: return "Left"; }
    }

    // Hàm xáo trộn danh sách (Fisher-Yates shuffle)
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}