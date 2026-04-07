using UnityEngine;

[CreateAssetMenu(menuName = "Mythic/Skills/Absolute Cinema")]
public class AbsoluteCinemaSkillSO : BaseSkillSO
{
    public override void Execute(GameManagerNet gm, CardNet card, int slotIndex)
    {
        // Kích hoạt trạng thái Vô Địch
        // Duration = 2 (1 bán lượt của mình kết thúc + 1 bán lượt của đối thủ)
        card.SetInvincible(2);

        Debug.Log($"[Absolute Cinema] Card {card.CardID} is now INVINCIBLE!");

        // Hiệu ứng Visual (nếu có)
        GameUIManager.Instance?.ShowFloatingText("ABSOLUTE CINEMA!", card.transform.position);
    }
}