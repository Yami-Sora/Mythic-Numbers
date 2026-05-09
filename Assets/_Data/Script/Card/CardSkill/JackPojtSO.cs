using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Mythic/Skills/JackPoltSO")]
public class JackPoltSO : RandomStatAbstract
{
    [Header("Config")]
    public int buffAmount = 5;

    protected override int LuckySidesCount => 2;

    protected override void ApplyBuffLogic(CardObj caster, List<int> targetSides)
    {
        // Cộng điểm cho 2 cạnh may mắn
        foreach (int side in targetSides)
        {
            ModifyStat(caster, side, buffAmount);
        }
    }

    protected override void ApplyDebuffLogic(CardObj caster, List<int> remainingSides)
    {
        // Set 2 cạnh còn lại về 0
        foreach (int side in remainingSides)
        {
            SetStat(caster, side, 0);
            Debug.Log($"[Skill] Cạnh {SideName(side)} bị set về 0!");
        }
    }
}