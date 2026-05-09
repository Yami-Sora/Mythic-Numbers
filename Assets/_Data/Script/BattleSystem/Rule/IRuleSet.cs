using UnityEngine;

public interface IRuleSet
{
    string RuleName { get; }
    string RuleDescription { get; }
    void ResolveBattle(GameManager gm, CardObj playedCard, int slotIndex);
    bool CanPlayCard(GameManager gm, CardObj cardToPlay);
}
