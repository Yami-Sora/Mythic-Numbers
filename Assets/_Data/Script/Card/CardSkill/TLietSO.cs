using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mythic/Skills/TLiet")]
public class TLietSO : RandomStatAbstract
{
    [Header("Config")]
    public int buffAmount = 6;

    protected override int LuckySidesCount => 4;

    protected override void ApplyBuffLogic(CardObj caster, List<int> targetSides)
    {
        int pointsLeft = buffAmount;
        while (pointsLeft > 0)
        {
            int randomIndex = Random.Range(0, targetSides.Count);
            int side = randomIndex;

            ModifyStat(caster, side, 1);

            pointsLeft--;
        }

        Debug.Log($"[TLiet] Added {buffAmount} points randomly. New Stats: Top:{caster.Top} Right:{caster.Right} Bottom:{caster.Bottom} Left:{caster.Left}");
    }

    protected override void ApplyDebuffLogic(CardObj caster, List<int> remainingSides)
    {
        // No debuff logic for TLiet
    }
}
