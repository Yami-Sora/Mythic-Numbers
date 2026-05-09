using UnityEngine;

public abstract class BaseSkillSO : ScriptableObject
{
    [TextArea] public string description;
    // Kích hoạt khi vừa sinh ra
    public virtual void OnCardSpawned(CardObj card) { }

    // Kích hoạt khi đánh xuống NHƯNG TRƯỚC KHI tính điểm
    public abstract void Execute(GameManager gm, CardObj caster, int slotIndex);

    // Kích hoạt SAU KHI tính điểm Battle xong (VD: Nếu lật được bài thì Buff...)
    public virtual void OnAfterBattle(GameManager gm, CardObj card, int flippedCount) { }
}