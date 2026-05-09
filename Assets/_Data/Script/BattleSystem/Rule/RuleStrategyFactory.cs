/// <summary>
/// Factory tạo IRuleSet theo CurrentRuleIndex – tuân thủ OCP, thêm luật mới mà không sửa GameManager.
/// </summary>
public static class RuleStrategyFactory
{
    public static IRuleSet Create(int ruleIndex)
    {
        IRuleSet baseRule = (ruleIndex == 0 || ruleIndex == 2) ? new NormalRule() : new ReverseRule();
        if (ruleIndex == 2 || ruleIndex == 3)
            return new OrderRuleDecorator(baseRule);
        return baseRule;
    }
}
