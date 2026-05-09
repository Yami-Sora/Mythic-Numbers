using UnityEngine;

[CreateAssetMenu(menuName = "Mythic/Skills/Skill_IssueSO")]
public class Skill_IssueSO : BaseSkillSO
{
    [Header("Config")]
    [Tooltip("Số lượng bài tối thiểu cần lật để kích hoạt Vô Địch")]
    public int minFlipCount = 1;

    [Tooltip("Thời gian hiệu lực (2 = 1 lượt đầy đủ của đối thủ)")]
    public int duration = 4;

    public override void Execute(GameManager gm, CardObj caster, int slotIndex)
    {
        // Không làm gì ở giai đoạn này
    }

    public override void OnAfterBattle(GameManager gm, CardObj card, int flippedCount)
    {
        // Kiểm tra điều kiện lật bài
        if (flippedCount >= minFlipCount)
        {
            card.SetInvincible(duration);
            Debug.Log($"[Skill] {card.name} lật được {flippedCount} lá -> Kích hoạt Vô Địch!");

            InGameUIManager.Instance?.ShowFloatingText("INVINCIBLE!", card.transform.position);
        }
        else
        {
            Debug.Log($"[Skill] {card.name} chỉ lật được {flippedCount} lá -> Không đủ điều kiện.");
        }
    }
}