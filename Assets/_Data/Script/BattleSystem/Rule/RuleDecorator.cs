
public abstract class RuleDecorator : IRuleSet
{
    protected IRuleSet _wrappedRule; // Luật được bọc bên trong
    public IRuleSet InnerRule => _wrappedRule;

    public RuleDecorator(IRuleSet rule)
    {
        _wrappedRule = rule;
    }

    public virtual string RuleName => "Decorator";

    // Mặc định: Ghép mô tả
    public virtual string RuleDescription => _wrappedRule.RuleDescription;

    // Mặc định: Chuyển tiếp việc xử lý chiến đấu cho luật bên trong
    public virtual void ResolveBattle(GameManager gm, CardObj playedCard, int slotIndex)
    {
        _wrappedRule.ResolveBattle(gm, playedCard, slotIndex);
    }

    // Mặc định: Chuyển tiếp việc kiểm tra điều kiện cho luật bên trong
    public virtual bool CanPlayCard(GameManager gm, CardObj cardToPlay)
    {
        return _wrappedRule.CanPlayCard(gm, cardToPlay);
    }
}
