using UnityEngine;

[CreateAssetMenu(menuName = "Mythic/Skills/Skill_IssueSO")]
public class Skill_IssueSO : BaseSkillSO
{
    [Header("Config")]
    [Tooltip("Số lượng bài tối thiểu cần lật để kích hoạt Vô Địch")]
    public int minFlipCount = 1;

    [Tooltip("Thời gian hiệu lực (2 = 1 lượt đầy đủ của đối thủ)")]
    public int duration = 4;

    public override void Execute(GameManagerNet gm, CardNet caster, int slotIndex)
    {
        // Không làm gì ở giai đoạn này
    }

    public override void OnAfterBattle(GameManagerNet gm, CardNet card, int flippedCount)
    {
        // Kiểm tra điều kiện lật bài
        if (flippedCount >= minFlipCount)
        {
            card.SetInvincible(duration);
            Debug.Log($"[Skill] {card.Object.Id} lật được {flippedCount} lá -> Kích hoạt Vô Địch!");

            GameUIManager.Instance?.ShowFloatingText("INVINCIBLE!", card.transform.position);
        }
        else
        {
            Debug.Log($"[Skill] {card.Object.Id} chỉ lật được {flippedCount} lá -> Không đủ điều kiện.");
        }
    }
}