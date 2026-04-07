using UnityEngine;
using Fusion;

public abstract class BaseSkillSO : ScriptableObject
{
    [TextArea] public string description;
    // Kích hoạt khi vừa sinh ra
    public virtual void OnCardSpawned(CardNet card) { }

    // Kích hoạt khi đánh xuống NHƯNG TRƯỚC KHI tính điểm
    public abstract void Execute(GameManagerNet gm, CardNet caster, int slotIndex);

    // Kích hoạt SAU KHI tính điểm Battle xong (VD: Nếu lật được bài thì Buff...)
    public virtual void OnAfterBattle(GameManagerNet gm, CardNet card, int flippedCount) { }
}