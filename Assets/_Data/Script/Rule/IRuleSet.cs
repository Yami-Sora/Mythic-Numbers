using UnityEngine;
using Fusion;

public interface IRuleSet
{
    string RuleName { get; }
    void ResolveBattle(GameManagerNet gm, CardNet playedCard, int slotIndex);
}
