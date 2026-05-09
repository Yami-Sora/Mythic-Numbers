using UnityEngine;

[CreateAssetMenu(menuName = "Mythic/Skills/Absolute Cinema")]
public class AbsoluteCinemaSkillSO : BaseSkillSO
{
    public override void Execute(GameManager gm, CardObj card, int slotIndex)
    {
        // Kích hoạt trạng thái Vô Địch
        // Duration = 2 (1 bán lượt của mình kết thúc + 1 bán lượt của đối thủ)
        card.SetInvincible(2);

        Debug.Log($"[Absolute Cinema] Card {card.CardID} is now INVINCIBLE!");

        // Hiệu ứng Visual (nếu có)
        InGameUIManager.Instance?.ShowFloatingText("ABSOLUTE CINEMA!", card.transform.position);
    }
}