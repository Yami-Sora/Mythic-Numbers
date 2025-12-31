using UnityEngine;
[CreateAssetMenu(menuName = "Mythic/Skills/Hee")]
public class HeheSkillSO : BaseSkillSO
{
    [Header("Config")]
    public int buffAmount = 3;

    public override void Execute(GameManagerNet gm, CardNet card, int slotIndex)
    {
        int minVal = Mathf.Min(card.Top, Mathf.Min(card.Right, Mathf.Min(card.Bottom, card.Left)));

        if (card.Top == minVal) card.Top += buffAmount;
        if (card.Right == minVal) card.Right += buffAmount;
        if (card.Bottom == minVal) card.Bottom += buffAmount;
        if (card.Left == minVal) card.Left += buffAmount;
    }
}
