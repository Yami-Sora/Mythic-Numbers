using UnityEngine;

[CreateAssetMenu(menuName = "Mythic/Skills/Buff Self")]
public class BuffSkillSO : BaseSkillSO
{
    public int boostAmount = 1;

    public override void Execute(GameManagerNet gm, CardNet caster, int slotIndex)
    {
        caster.Top += boostAmount;
        caster.Right += boostAmount;
        caster.Bottom += boostAmount;
        caster.Left += boostAmount;

        Debug.Log($"[Skill] {caster.name} tự buff {boostAmount} điểm toàn chỉ số!");
    }
}