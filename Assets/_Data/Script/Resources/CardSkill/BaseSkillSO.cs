using UnityEngine;
using Fusion;

public abstract class BaseSkillSO : ScriptableObject
{
    [TextArea] public string description;
    public abstract void Execute(GameManagerNet gm, CardNet caster, int slotIndex);
}